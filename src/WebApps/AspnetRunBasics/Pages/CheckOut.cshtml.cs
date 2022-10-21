using System;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    public class CheckOutModel : PageModel
    {
        //private readonly ICartRepository _cartRepository;
        //private readonly IOrderRepository _orderRepository;

        //public CheckOutModel(ICartRepository cartRepository, IOrderRepository orderRepository)
        //{
        //    _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        //    _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        //}

        //In order to convert from monolotic structure to microservice
        //replace the above repos with previously developed microservices over gateway api service

        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;

        public CheckOutModel(IBasketService basketService, IOrderService orderService)
        {
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }


        [BindProperty]
        //public Entities.Order Order { get; set; }
        public BasketCheckoutModel Order { get; set; }

        //public Entities.Cart Cart { get; set; } = new Entities.Cart();
        public BasketModel Cart { get; set; } = new BasketModel();

        public async Task<IActionResult> OnGetAsync()
        {
            //Cart = await _cartRepository.GetCartByUserName("test");
            var userName = "swn";
            Cart = await _basketService.GetBasket(userName);



            return Page();
        }

        public async Task<IActionResult> OnPostCheckOutAsync()
        {
            //Cart = await _cartRepository.GetCartByUserName("test");

            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            //Order.UserName = "test";
            //Order.TotalPrice = Cart.TotalPrice;

            //await _orderRepository.CheckOut(Order);
            //await _cartRepository.ClearCart("test");

            var userName = "swn";
            Cart = await _basketService.GetBasket(userName);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Order.UserName = "swn";
            Order.TotalPrice = Cart.TotalPrice;

            await _basketService.CheckoutBasket(Order);
           

            return RedirectToPage("Confirmation", "OrderSubmitted");
        }       
    }
}