using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Microsoft.eShopWeb.Web.Pages.Basket
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly IBasketService _basketService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOrderService _orderService;
        private string _username = null;
        private readonly IBasketViewModelService _basketViewModelService;
        private readonly IAppLogger<CheckoutModel> _logger;

        public CheckoutModel(IBasketService basketService,
            IBasketViewModelService basketViewModelService,
            SignInManager<ApplicationUser> signInManager,
            IOrderService orderService,
            IAppLogger<CheckoutModel> logger)
        {
            _basketService = basketService;
            _signInManager = signInManager;
            _orderService = orderService;
            _basketViewModelService = basketViewModelService;
            _logger = logger;
        }

        public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

        public async Task OnGet()
        {
            await SetBasketModelAsync();
        }

        public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
        {
            try
            {
                await SetBasketModelAsync();

                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
                await _basketService.SetQuantities(BasketModel.Id, updateModel);
                await _orderService.CreateOrderAsync(BasketModel.Id, new Address("123 Main St.", "Kent", "OH", "United States", "44240"));
                await _basketService.DeleteBasketAsync(BasketModel.Id);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://zvvazuretraining-fa.azurewebsites.net/api/Function1?code=872qksVeYTHy61dv0GPcIUzaH3DhAhTrh2xHQkILXlBTv3FYnjHhGA==");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json =(new OrderReport(
                        (new Address("123 Main St.", "Kent", "OH", "United States", "44240")).ToString(),
                        BasketModel.Items.Select(t => new OrderItem(new CatalogItemOrdered(t.CatalogItemId,t.ProductName,t.PictureUrl),t.UnitPrice, t.Quantity)).ToArray()
                        )).ToString();

                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }

            }
            catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
            {
                //Redirect to Empty Basket page
                _logger.LogWarning(emptyBasketOnCheckoutException.Message);
                return RedirectToPage("/Basket/Index");
            }

            return RedirectToPage("Success");
        }

        private async Task SetBasketModelAsync()
        {
            if (_signInManager.IsSignedIn(HttpContext.User))
            {
                BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
            }
            else
            {
                GetOrSetBasketCookieAndUserName();
                BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username);
            }
        }

        private void GetOrSetBasketCookieAndUserName()
        {
            if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
            {
                _username = Request.Cookies[Constants.BASKET_COOKIENAME];
            }
            if (_username != null) return;

            _username = Guid.NewGuid().ToString();
            var cookieOptions = new CookieOptions();
            cookieOptions.Expires = DateTime.Today.AddYears(10);
            Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
        }
    }
}
