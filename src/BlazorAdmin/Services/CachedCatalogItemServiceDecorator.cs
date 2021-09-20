using Blazored.LocalStorage;
using BlazorShared.Interfaces;
using BlazorShared.Models;
//using Microsoft.ApplicationInsights;
//using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorAdmin.Services
{
    public class CachedCatalogItemServiceDecorator : ICatalogItemService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly CatalogItemService _catalogItemService;
        private ILogger<CachedCatalogItemServiceDecorator> _logger;
        //readonly TelemetryClient telemetryClient;

        public CachedCatalogItemServiceDecorator(ILocalStorageService localStorageService,
            CatalogItemService catalogItemService,
            ILogger<CachedCatalogItemServiceDecorator> logger)
        {
            _localStorageService = localStorageService;
            _catalogItemService = catalogItemService;
            _logger = logger;
            //telemetryClient = new TelemetryClient(new TelemetryConfiguration("f5a68c6a-006b-46f2-a519-584904698e33"));
        }

        public async Task<List<CatalogItem>> ListPaged(int pageSize)
        {
            string key = "items";
            var cacheEntry = await _localStorageService.GetItemAsync<CacheEntry<List<CatalogItem>>>(key);
            if (cacheEntry != null)
            {
                _logger.LogInformation("Loading items from local storage.");
                if (cacheEntry.DateCreated.AddMinutes(1) > DateTime.UtcNow)
                {
                    //_logger.LogWarning($"{cacheEntry.Value.Count} were taken from the cache");
                    //telemetryClient.TrackEvent("ListPaged_count", new Dictionary<string, string>() { { "count_items", cacheEntry.Value.Count.ToString() } });
                    return cacheEntry.Value;
                }
                else
                {
                    _logger.LogInformation($"Loading {key} from local storage.");
                    await _localStorageService.RemoveItemAsync(key);
                }
            }

            var items = await _catalogItemService.ListPaged(pageSize);
            var entry = new CacheEntry<List<CatalogItem>>(items);
            await _localStorageService.SetItemAsync(key, entry);
            //_logger.LogWarning($"{items.Count} were returned from the database");
            //telemetryClient.TrackEvent("ListPaged_count", new Dictionary<string, string>() { { "count_items", items.Count.ToString() } });
            return items;
        }

        public async Task<List<CatalogItem>> List()
        {
            string key = "items";
            var cacheEntry = await _localStorageService.GetItemAsync<CacheEntry<List<CatalogItem>>>(key);
            if (cacheEntry != null)
            {
                _logger.LogInformation("Loading items from local storage.");
                if (cacheEntry.DateCreated.AddMinutes(1) > DateTime.UtcNow)
                {
                    return cacheEntry.Value;
                }
                else
                {
                    _logger.LogInformation($"Loading {key} from local storage.");
                    await _localStorageService.RemoveItemAsync(key);
                }
            }

            var items = await _catalogItemService.List();
            var entry = new CacheEntry<List<CatalogItem>>(items);
            await _localStorageService.SetItemAsync(key, entry);
            return items;
        }

        public async Task<CatalogItem> GetById(int id)
        {
            return (await List()).FirstOrDefault(x => x.Id == id);
        }

        public async Task<CatalogItem> Create(CreateCatalogItemRequest catalogItem)
        {
            var result = await _catalogItemService.Create(catalogItem);
            await RefreshLocalStorageList();

            return result;
        }

        public async Task<CatalogItem> Edit(CatalogItem catalogItem)
        {
            var result = await _catalogItemService.Edit(catalogItem);
            await RefreshLocalStorageList();

            return result;
        }

        public async Task<string> Delete(int id)
        {
            var result = await _catalogItemService.Delete(id);
            await RefreshLocalStorageList();

            return result;
        }

        private async Task RefreshLocalStorageList()
        {
            string key = "items";

            await _localStorageService.RemoveItemAsync(key);
            var items = await _catalogItemService.List();
            var entry = new CacheEntry<List<CatalogItem>>(items);
            await _localStorageService.SetItemAsync(key, entry);
        }
    }
}
