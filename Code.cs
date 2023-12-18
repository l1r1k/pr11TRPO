using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnlineShop.Models;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace OnlineShop.Controllers
{
    public class BuyerController : Controller
    {
        public async Task<IActionResult> HomePageBuyer(string sort = "asc")
        {
            switch (sort)
            {
                case "asc":
                    List<AvailableFirnituresListForNonAuthUserAsc> furnitureAsc = new List<AvailableFirnituresListForNonAuthUserAsc>();
                    using (var httpClient = new HttpClient())
                    {
                        using (var response = await httpClient.GetAsync("https://localhost:7002/api/AvailableFirnituresListForNonAuthUserAsc"))
                        {
                            string apiResponse = await response.Content.ReadAsStringAsync();
                            furnitureAsc = JsonConvert.DeserializeObject<List<AvailableFirnituresListForNonAuthUserAsc>>(apiResponse);
                        }
                    }
                    AvailableModel availableModel = new AvailableModel();
                    availableModel.FirnituresListForNonAuthUserAsc = furnitureAsc;
                    availableModel.TypeSort = true;
                    return View(availableModel);
                case "desc":
                    List<AvailableFirnituresListForNonAuthUserDesc> furnitureDesc = new List<AvailableFirnituresListForNonAuthUserDesc>();
                    using (var httpClient = new HttpClient())
                    {
                        using (var response = await httpClient.GetAsync("https://localhost:7002/api/AvailableFirnituresListForNonAuthUserAsc"))
                        {
                            string apiResponse = await response.Content.ReadAsStringAsync();
                            furnitureDesc = JsonConvert.DeserializeObject<List<AvailableFirnituresListForNonAuthUserDesc>>(apiResponse);
                        }
                    }
                    AvailableModel model = new AvailableModel();
                    model.FirnituresListForNonAuthUserDesc = furnitureDesc;
                    model.TypeSort = false;
                    return View(model);
                default:
                    return View(new AvailableModel());
            }
        }

        public async Task<IActionResult> FurnitureInfo(string article)
        {
            return View();
        }

        public IActionResult Cart()
        {
            Cart cart = new Cart();

            if (HttpContext.Session.Keys.Contains("Cart"))
                cart = System.Text.Json.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));
            return View(cart);
        }

        public IActionResult AddToCart(CartLine cartLine)
        {
            Cart cart = new Cart();

            if (HttpContext.Session.Keys.Contains("Cart"))
                cart = System.Text.Json.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));

            cart.CartLines.Add(cartLine);

            HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize<Cart>(cart));         
            
            return RedirectToAction("HomePageBuyer", "Buyer");
        }

        public IActionResult RemoveFromCart(int number)
        {
            Cart cart = new Cart();
            if (HttpContext.Session.Keys.Contains("Cart"))
                cart = System.Text.Json.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));

            cart.CartLines.RemoveAt(number);

            HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize<Cart>(cart));

            return RedirectToAction("Cart", "Buyer");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            var unsecretLogin = Helpers.EncryptDecrypt.Decrypt(HttpContext.Session.GetString("AuthUser"));
            var buyer = new Buyer();
            var buyers = new List<Buyer>();
            var orders = new List<Order>();
            HttpStatusCode httpStatusCode = new HttpStatusCode();

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/Buyers"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    buyers = JsonConvert.DeserializeObject<List<Buyer>>(apiResponse);
                }
            }

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/Orders"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    orders = JsonConvert.DeserializeObject<List<Order>>(apiResponse);
                }
            }

            int countOrders = orders.Count();

            if (countOrders == 0) countOrders = 1;

            foreach (var item in buyers)
            {
                var unsecretLoginItem = Helpers.EncryptDecrypt.Decrypt(item.LoginBuyer);
                if (unsecretLogin == unsecretLoginItem)
                {
                    buyer = item;
                    break;
                }
            }

            Cart cart = new Cart();
            if (HttpContext.Session.Keys.Contains("Cart"))
                cart = System.Text.Json.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));

            foreach(var cartLines in cart.CartLines)
            {
                using (var httpClient = new HttpClient())
                {
                    OrderProcedure orderProcedure = new OrderProcedure();

                    orderProcedure.ArticleFurniture = cartLines.Furniture.ArticleFurniture;
                    orderProcedure.LoginBuyer = buyer.LoginBuyer;
                    orderProcedure.CountFurniture = cartLines.CountFurniture;
                    orderProcedure.NumberOrder = countOrders.ToString("D8");

                    StringContent content = new StringContent(JsonConvert.SerializeObject(orderProcedure), Encoding.UTF8, "application/json");

                    using (var response = await httpClient.PostAsync("https://localhost:7002/api/Orders/createOrder", content))
                    {
                        httpStatusCode = response.StatusCode;
                    }
                }
            }

            if(httpStatusCode == HttpStatusCode.OK)
            {
                HttpContext.Session.Remove("Cart");
                return RedirectToAction("MyOrders", "Buyer");
            }

            return View();
        }

        public async Task<IActionResult> MyOrders()
        {
            OrderModel orderModel = new OrderModel();
            var loginBuyer = HttpContext.Session.GetString("AuthUser");
            var unsecretLogin = Helpers.EncryptDecrypt.Decrypt(loginBuyer);
            var buyer = new Buyer();
            List<StatusOrder> statusOrders = new List<StatusOrder>();
            List<Order> orders = new List<Order>();
            List<Order> filterOrders = new List<Order>();
            List<Furniture> furnitures = new List<Furniture>();
            List<OrderFurniture> orderFurnitures = new List<OrderFurniture>();
            List<Buyer> buyers = new List<Buyer>();
            List<FurnitureStoreHouse> furnitureStoreHouses = new List<FurnitureStoreHouse>();
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/Orders"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    orders = JsonConvert.DeserializeObject<List<Order>>(apiResponse);
                }
            }
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/StatusOrders"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    statusOrders = JsonConvert.DeserializeObject<List<StatusOrder>>(apiResponse);
                }
            }
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/Furnitures"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    furnitures = JsonConvert.DeserializeObject<List<Furniture>>(apiResponse);
                }
            }
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/OrderFurnitures"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    orderFurnitures = JsonConvert.DeserializeObject<List<OrderFurniture>>(apiResponse);
                }
            }
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/OrderFurnitures"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    buyers = JsonConvert.DeserializeObject<List<Buyer>>(apiResponse);
                }
            }
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://localhost:7002/api/OrderFurnitures"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    furnitureStoreHouses = JsonConvert.DeserializeObject<List<FurnitureStoreHouse>>(apiResponse);
                }
            }
            foreach (var item in buyers)
            {
                string unsecterLoginItem = Helpers.EncryptDecrypt.Decrypt(item.LoginBuyer);
                if (unsecterLoginItem == unsecretLogin) buyer = item;
                else buyer = null;
            }
            foreach(var order in orders)
            {
                string unsecretOwnerOrder = Helpers.EncryptDecrypt.Decrypt(order.LoginBuyerFk);
                if (unsecretOwnerOrder == unsecretLogin) filterOrders.Add(order);
            }
            orderModel.furnitureStoreHouses = furnitureStoreHouses;
            orderModel.statusOrders = statusOrders;
            orderModel.orders = filterOrders;
            orderModel.furnitures = furnitures;
            orderModel.orderFurnitures = orderFurnitures;
            return View(orderModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
