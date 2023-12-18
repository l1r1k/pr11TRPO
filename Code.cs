using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using SysJson =  System.Text.Json ;

using Newtonsoft.Json;

using OnlineShop.Helpers;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    /// <summary>
    /// Класс отвечающий за функционал покупателя
    /// </summary>
    public class BuyerController : Controller
    {
        /// <summary>
        /// Функция выполняет GET-запрос на API для получения списков по переданному пути
        /// </summary>
        /// <param name="pathEndpointApi">Путь эндпоинта на API</param>
        /// <param name="responseListFromAPI">Список, получающий запрос с API</param>
        /// <returns>Список заполненный данными с API</returns>
        public async Task<T> GetRequestToAPI(string pathEndpointApi, List<T> responseListFromAPI)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync("https://localhost:7002/api/" + pathEndpointApi))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        responseListFromAPI = JsonConvert.DeserializeObject<List<T>>(apiResponse);
                    }
                }
                return responseListFromAPI;
            }
            ///Если ошибка при запросе, то возвращается список строк, с элементом ошибки.
            catch (Exception ex)
            {
                return new List<string>(){"Ошибка при попытке отправить запрос. Сообщение ошибки: " + ex.Message}
            }
        }

        /// <summary>
        /// Функция получающая с API отсортированный список мебели, тип сортировки определяется исходя из полученного параметра, и отображающая представление страницы "Каталог товаров"
        /// с моделью "Достпуной мебели"
        /// </summary>
        /// <param name="sort">Параметр типа сортировки списка мебели</param>
        /// <returns>Представление с моделью "Достпуной мебели", заполненной списком отсортированной мебели</returns>
        public async Task<IActionResult> HomePageBuyer(string sort = "asc")
        {
            try
            {
                ///Инструкция получения списка мебели с API
                switch (sort)
                {
                    ///Если статус был равен типу сортировке ASC
                    case "asc":
                        ///Список доступной мебели, отсортированной в порядке возрастания стоимости мебели
                        List<AvailableFirnituresListForNonAuthUserAsc> furnitureSortedByASC = new List<AvailableFirnituresListForNonAuthUserAsc>();
                        ///Запрос на API
                        furnitureSortedByASC = GetRequestToAPI("AvailableFirnituresListForNonAuthUserAsc", furnitureSortedByASC)
                        ///Модель представления
                        AvailableModel availableModel = new AvailableModel();
                        availableModel.firnituresListForNonAuthUserAsc = furnitureSortedByASC;
                        availableModel.typeSort = true;
                        return View(availableModel);
                    ///Если статус был равен типу сортировке DESC    
                    case "desc":
                        ///Список доступной мебели, отсортированной в порядке убывания стоимости мебели
                        List<AvailableFirnituresListForNonAuthUserDesc> furnitureSortedByDESC = new List<AvailableFirnituresListForNonAuthUserDesc>();
                        ///Запрос на апи
                        furnitureSortedByDESC = GetRequestToAPI("AvailableFirnituresListForNonAuthUserAsc", furnitureSortedByDESC)
                        ///Модель представления
                        AvailableModel model = new AvailableModel();
                        model.firnituresListForNonAuthUserDesc = furnitureSortedByDESC;
                        model.typeSort = false;
                        return View(model);
                    default:
                        return View(new AvailableModel());
                }   
            }
            ///Если при выполнении запроса обнаружилась критическая ошибка, то отображается пустое представление
            catch (Exception ex)
            {
                return View(new AvailableModel());
            }
        }
        /// <summary>
        /// Функция, отображающая страницу подробной информации о мебели
        /// </summary>
        /// <param name="article">Артикул мебели</param>
        /// <returns>Представление страницы "Подробная информация мебели"</returns>
        public async Task<IActionResult> FurnitureInfo(string article)
        {
            return View();
        }

        /// <summary>
        /// Функция, отобрадающая страницу "Корзина", получая ее из сессии куки
        /// </summary>
        /// <returns>Представление страницы "Корзина" с моделью корзины</returns>
        public IActionResult Cart()
        {
            try
            {
                Cart cart = new Cart();

                if (HttpContext.Session.Keys.Contains("Cart"))
                {
                    cart = SysJson.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));
                }

                return View(cart);
            }
            ///Если при попытке получения корзины из сессии куки обнаружена ошибка, то происходит возврат на страницу "Каталог товаров"
            catch (Exception ex)
            {
                return RedirectToAction("HomePageBuyer", "Buyer")
            }
        }

        /// <summary>
        /// Функция добавления мебели в корзину
        /// </summary>
        /// <param name="cartLine">Позиция в корзине, включающая в себя артикул мебели</param>
        /// <returns>Переадресацию на страницу "Каталог товаров"</returns>
        public IActionResult AddToCart(CartLine cartLine)
        {
            try
            {
                Cart cart = new Cart();

                if (HttpContext.Session.Keys.Contains("Cart"))
                {   
                    cart = SysJson.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));
                }

                cart.CartLines.Add(cartLine);

                HttpContext.Session.SetString("Cart", SysJson.JsonSerializer.Serialize<Cart>(cart));         
            
                return RedirectToAction("HomePageBuyer", "Buyer");
            }
            ///Если при попытке добавления мебели в корзину происходит критическая ошибка, то происходит возврат на страницу "Каталог товаров"
            catch (Exception ex)
            {
                return RedirectToAction("HomePageBuyer", "Buyer");
            }
        }

        /// <summary>
        /// Функция удаления позиции из корзины
        /// </summary>
        /// <param name="number">Индекс позиции в списке позиций</param>
        /// <returns>Переадресацию на страницу "Корзина"</returns>
        public IActionResult RemoveFromCart(int number)
        {
            try
            {
                Cart cart = new Cart();
                if (HttpContext.Session.Keys.Contains("Cart"))
                {
                    cart = SysJson.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));
                }

                cart.CartLines.RemoveAt(number);

                HttpContext.Session.SetString("Cart", SysJson.JsonSerializer.Serialize<Cart>(cart));

                return RedirectToAction("Cart", "Buyer");
            }
            ///Если при попытке удаления позиции из корзины происходит критическая ошибка, то происходит обновление страницы "Корзина"
            catch (Exception ex)
            {
                return RedirectToAction("Cart", "Buyer");
            }
        }

        /// <summary>
        /// Функция оформления заказа, исходя из позиций в корзине
        /// </summary>
        /// <returns>
        /// Если оформление заказа прошло успешно, то происходит переадресация на страницу "Заказы",
        /// иначе, отображается представление, на котором выполнялся запрос - страница "Корзина"
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            try
            {
                var decryptedLoginAuthUser = EncryptDecrypt.Decrypt(HttpContext.Session.GetString("AuthUser"));
                var buyer = new Buyer();
                var buyers = new List<Buyer>();
                var orders = new List<Order>();
                HttpStatusCode httpStatusCode = new HttpStatusCode();

                buyers = GetRequestToAPI("Buyers", buyers);

                orders = GetRequestToAPI("Orders", orders);

                int countOrders = orders.Count();

                if (countOrders == 0) countOrders = 1;

                foreach (var buyerInList in buyers)
                {
                    var decryptedLoginBuyerInList = EncryptDecrypt.Decrypt(buyerInList.LoginBuyer);
                    if (decryptedLoginAuthUser == decryptedLoginBuyerInList)
                    {
                        buyer = buyerInList;
                        break;
                    }
                }

                Cart cart = new Cart();
                if (HttpContext.Session.Keys.Contains("Cart"))
                {
                    cart = SysJson.JsonSerializer.Deserialize<Cart>(HttpContext.Session.GetString("Cart"));
                }

                foreach(var cartLine in cart.CartLines)
                {
                    using (var httpClient = new HttpClient())
                    {
                        OrderProcedure orderProcedure = new OrderProcedure();

                        orderProcedure.articleFurniture = cartLines.Furniture.articleFurniture;
                        orderProcedure.loginBuyer = buyer.loginBuyer;
                        orderProcedure.countFurniture = cartLines.countFurniture;
                        orderProcedure.numberOrder = countOrders.ToString("D8");

                        StringContent serializedOrderProcudure = new StringContent(JsonConvert.SerializeObject(serializedOrderProcudure), Encoding.UTF8, "application/json");

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
            ///Если при попытке оформления заказа произошла критическая ошибка, то обновляется страница "Корзина"
            catch (Exception ex)
            {
                return RedirectToAction("Cart", "Buyer");
            }
        }

        /// <summary>
        /// Функция, отображающая заказы авторизованного пользователя
        /// </summary>
        /// <returns>
        /// Представление страницы "Заказы"
        /// </returns>
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                OrderModel orderModel = new OrderModel();
                var loginAuthUser = HttpContext.Session.GetString("AuthUser");
                var decryptedLoginAuthUser = EncryptDecrypt.Decrypt(loginAuthUser);
                var buyer = new Buyer();
                List<StatusOrder> statusOrders = new List<StatusOrder>();
                List<Order> orders = new List<Order>();
                List<Order> orderCurrentUser = new List<Order>();
                List<Furniture> furnitures = new List<Furniture>();
                List<OrderFurniture> furnituresInOrders = new List<OrderFurniture>();
                List<Buyer> buyers = new List<Buyer>();
                List<FurnitureStoreHouse> furnituresInStoreHouses = new List<FurnitureStoreHouse>();

                orders = GetRequestToAPI("Orders", orders);

                statusOrders = GetRequestToAPI("StatusOrders", statusOrders);

                furnitures = GetRequestToAPI("Furnitures", furnitures);

                furnituresInOrders = GetRequestToAPI("OrderFurnitures", furnituresInOrders)
            
                buyers = GetRequestToAPI("Buyers", buyers);
            
                furnituresInStoreHouses = GetRequestToAPI("FunritureStoreHouses", furnituresInStoreHouses);

                foreach (var buyerInList in buyers)
                {
                    string decryptedLoginBuyerInList = EncryptDecrypt.Decrypt(buyerInList.LoginBuyer);
                    if (decryptedLoginBuyerInList == decryptedLoginAuthUser) 
                    {
                        buyer = buyerInList;
                    }
                    else
                    {
                        buyer = null;
                    } 
                }
                foreach(var order in orders)
                {
                    string decryptedLoginOwnerOrder = EncryptDecrypt.Decrypt(order.LoginBuyerFk);
                    if (decryptedLoginOwnerOrder == decryptedLoginAuthUser) 
                    {
                        orderCurrentUser.Add(order);
                    }
                }
                orderModel.furnituresInStoreHouses = furnituresInStoreHouses;
                orderModel.statusOrders = statusOrders;
                orderModel.orders = orderCurrentUser;
                orderModel.furnitures = furnitures;
                orderModel.orderFurnitures = furnituresInOrders;
                return View(orderModel);
            }
            ///Если при выполнении функции произошла критическая ошибка, то происходит переадресация на страницу "Каталог товаров"
            catch (Exception ex)
            {
                return RedirectToAction("HomePageBuyer", "Buyer")
            }
        }

        /// <summary>
        /// Функция обработки критических ошибок
        /// </summary>
        /// <returns>Отображение представления ошибок, с текстом ошибки</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
