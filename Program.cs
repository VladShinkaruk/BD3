using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WebCityEvents.Data;
using System.Collections.Generic;
using System.Linq;
using WebCityEvents.Models;

namespace WebCityEvents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;
            string connectionString = builder.Configuration.GetConnectionString("RemoteSQLConnection");
            services.AddDbContext<EventContext>(options => options.UseSqlServer(connectionString));

            services.AddMemoryCache();

            services.AddDistributedMemoryCache();
            services.AddSession();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseSession();

            int cacheTime = 2 * 23 + 240;

            AddTableMiddleware(app, cacheTime);

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Map("/info", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";

                    var clientInfo = $"<h1>Информация о клиенте:</h1>" +
                                     $"<p>IP: {context.Connection.RemoteIpAddress}</p>" +
                                     $"<p>Метод запроса: {context.Request.Method}</p>" +
                                     $"<p>User-Agent: {context.Request.Headers["User-Agent"]}</p>";

                    await context.Response.WriteAsync(clientInfo);
                });
            });

            app.Map("/searchform1", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var db = context.RequestServices.GetRequiredService<EventContext>();
                    var places = db.Places.ToList();

                    // Получаем данные из куки
                    var placeNameCookie = context.Request.Cookies["placename"] ?? "";
                    var placeIdCookie = context.Request.Cookies["placeid"] ?? "";

                    // Получаем данные из сессии
                    var placeNameSession = context.Session.GetString("placename") ?? "";
                    var placeIdSession = context.Session.GetString("placeid") ?? "";

                    // Заполняем значение на основе кук или сессии
                    string placenameValue = !string.IsNullOrEmpty(placeNameCookie) ? placeNameCookie : placeNameSession;
                    string placeIdValue = !string.IsNullOrEmpty(placeIdCookie) ? placeIdCookie : placeIdSession;

                    context.Response.ContentType = "text/html; charset=utf-8";
                    string form = "<form method='POST' action='/searchform1'>" +
                                  $"Введите название места: <input type='text' name='placename' value='{placenameValue}' />" +
                                  "<br>Выберите место: <select name='placeid'>";

                    foreach (var place in places)
                    {
                        // Предварительно выбираем сохраненное значение
                        var selected = place.PlaceID.ToString() == placeIdValue ? "selected" : "";
                        form += $"<option value='{place.PlaceID}' {selected}>{place.PlaceName}</option>";
                    }

                    form += "</select>" +
                            "<br><button type='submit'>Поиск</button>" +
                            "</form>";

                    // Если форма отправлена
                    if (context.Request.Method == "POST")
                    {
                        var formData = context.Request.Form;
                        var formPlacename = formData["placename"]; // Изменил имя переменной на formPlacename
                        var placeid = formData["placeid"];

                        // Сохраняем данные в куки и сессии
                        context.Response.Cookies.Append("placename", formPlacename, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });
                        context.Response.Cookies.Append("placeid", placeid, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });

                        context.Session.SetString("placename", formPlacename);
                        context.Session.SetString("placeid", placeid);

                        // Отправляем обновленную форму
                        form += "<p>Данные формы сохранены в куки и сессии.</p>";
                    }

                    await context.Response.WriteAsync(form);
                });
            });


            app.Map("/searchform2", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var db = context.RequestServices.GetRequiredService<EventContext>();
                    var customers = db.Customers.ToList();

                    context.Response.ContentType = "text/html; charset=utf-8";

                    // Чтение данных из сессии
                    string savedTicketCount = context.Session.GetString("ticketcount") ?? "";
                    string savedCustomerId = context.Session.GetString("customerid") ?? "";

                    // Формирование формы с заполненными значениями
                    string form = $"<form method='GET' action='/searchform2'>" +
                                  $"Введите количество билетов: <input type='number' name='ticketcount' value='{savedTicketCount}' />" +
                                  $"<br>Выберите клиента: <select name='customerid'>";

                    foreach (var customer in customers)
                    {
                        bool isSelected = customer.CustomerID.ToString() == savedCustomerId;
                        form += $"<option value='{customer.CustomerID}' {(isSelected ? "selected" : "")}>{customer.FullName}</option>";
                    }

                    form += "</select>" +
                            "<br><button type='submit'>Поиск</button>" +
                            "</form>";

                    // Сохранение данных формы в сессии
                    if (context.Request.Query.ContainsKey("ticketcount") && context.Request.Query.ContainsKey("customerid"))
                    {
                        context.Session.SetString("ticketcount", context.Request.Query["ticketcount"]);
                        context.Session.SetString("customerid", context.Request.Query["customerid"]);
                    }

                    await context.Response.WriteAsync(form);
                });
            });




            app.Run((context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";

                string HtmlString = "<HTML><HEAD><TITLE>Главная</TITLE></HEAD>" +
                "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                "<BODY><H1>Добро пожаловать на сайт городских мероприятий</H1>" +
                "<BR><A href='/places'>Места</A>" +
                "<BR><A href='/events'>Мероприятия</A>" +
                "<BR><A href='/customers'>Клиенты</A>" +
                "<BR><A href='/organizers'>Организаторы</A>" +
                "<BR><A href='/ticketorders'>Заказы билетов</A>" +
                "<BR><A href='/info'>Информация о клиенте</A>" +
                "<BR><A href='/searchform1'>Форма поиска 1</A>" +
                "<BR><A href='/searchform2'>Форма поиска 2</A>" +
                "</BODY></HTML>";

                return context.Response.WriteAsync(HtmlString);
            });

            app.UseRouting();
            app.Run();
        }

        public static void AddTableMiddleware(WebApplication app, int cacheTime)
        {
            app.Map("/places", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await CacheTableData<Place>(context, "places", cacheTime);
                });
            });

            app.Map("/events", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await CacheTableData<Event>(context, "events", cacheTime);
                });
            });

            app.Map("/customers", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await CacheTableData<Customer>(context, "customers", cacheTime);
                });
            });

            app.Map("/organizers", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await CacheTableData<Organizer>(context, "organizers", cacheTime);
                });
            });

            app.Map("/ticketorders", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await CacheTableData<TicketOrder>(context, "ticketorders", cacheTime);
                });
            });
        }

        public static async Task CacheTableData<T>(HttpContext context, string cacheKey, int cacheTime) where T : class
        {
            var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
            var db = context.RequestServices.GetRequiredService<EventContext>();

            List<T> data;

            if (!cache.TryGetValue(cacheKey, out data))
            {
                data = db.Set<T>().Take(20).ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheTime));

                cache.Set(cacheKey, data, cacheOptions);

                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("<h1>Данные загружены из базы и добавлены в кэш</h1>");
            }
            else
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("<h1>Данные получены из кэша</h1>");
            }

            string htmlResponse = "<ul>";

            foreach (var item in data)
            {
                if (item is Place place)
                {
                    htmlResponse += $"<li>Место: ID = {place.PlaceID}, Название = {place.PlaceName}, Геолокация = {place.Geolocation}</li>";
                }
                else if (item is Event evnt)
                {
                    htmlResponse += $"<li>Мероприятие: ID = {evnt.EventID}, Название = {evnt.EventName}, Дата = {evnt.EventDate}, Цена билета = {evnt.TicketPrice}</li>";
                }
                else if (item is Customer customer)
                {
                    htmlResponse += $"<li>Клиент: ID = {customer.CustomerID}, Имя = {customer.FullName}, Паспортные данные = {customer.PassportData}</li>";
                }
                else if (item is Organizer organizer)
                {
                    htmlResponse += $"<li>Организатор: ID = {organizer.OrganizerID}, Имя = {organizer.FullName}, Должность = {organizer.Post}</li>";
                }
                else if (item is TicketOrder order)
                {
                    htmlResponse += $"<li>Заказ: ID = {order.OrderID}, Клиент ID = {order.CustomerID}, Мероприятие ID = {order.EventID}, Количество билетов = {order.TicketCount}</li>";
                }
                else
                {
                    htmlResponse += $"<li>Неизвестный объект: {item}</li>";
                }
            }

            htmlResponse += "</ul>";

            await context.Response.WriteAsync(htmlResponse);
        }
    }
}