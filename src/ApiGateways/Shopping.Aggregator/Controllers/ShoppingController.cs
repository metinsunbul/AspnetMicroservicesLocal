using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ShoppingController : ControllerBase
    {
        private  readonly ICatalogService _catalogService;
        private  readonly IBasketService _basketService;
        private  readonly IOrderService _orderService;

        public ShoppingController(ICatalogService catalogService, IBasketService basketService, IOrderService orderService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        [HttpGet("{userName}", Name = "GetShopping")]
        [ProducesResponseType(typeof(ShoppingModel), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingModel>> GetShopping(string userName)
        {
            //Get basket with username
            var basket = await _basketService.GetBasket(userName);
            //iterate over basket item and consume category service
            foreach (var item in basket.Items)
            {
                var product = await _catalogService.GetCatalog(item.ProductId);
                //reflect additional product fields onto basket dto item
                //synch product coming from category service into basketitem dto with extended columns
                item.ProductName = product.Name;
                item.Category  =   product.Category;
                item.Summary = product.Summary;
                item.Description = product.Description;
                item.ImageFile = product.ImageFile;
            }            
            //Consume ordering service
            var orders = await _orderService.GetOrdersByUserName(userName);
            //return ShoppingModel
            var shoppingModel = new ShoppingModel
            {
                UserName = userName,
                BasketWithProducts = basket,
                Orders = orders
            }; 
            
            return Ok(shoppingModel);
        }


    }
}
