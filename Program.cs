using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebCityEvents.Services;
using Microsoft.EntityFrameworkCore;
using WebCityEvents.Data;
using System.Linq;
using System.Threading.Tasks;
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
            services.AddScoped(typeof(ICachedEntityService<>), typeof(CachedEntityService<>));
            services.AddScoped(typeof(ITableDataService<>), typeof(TableDataService<>));

            services.AddMemoryCache();
            services.AddSession();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();
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

                    var placeNameCookie = context.Request.Cookies["placename"] ?? "";
                    var placeIdCookie = context.Request.Cookies["placeid"] ?? "";

                    string placenameValue = placeNameCookie;
                    string placeIdValue = placeIdCookie;

                    context.Response.ContentType = "text/html; charset=utf-8";

                    string form = "<form method='POST' action='/searchform1'>" +
                                  $"Введите название места: <input type='text' name='placename' value='{placenameValue}' />" +
                                  "<br>Выберите место: <select name='placeid'>";

                    foreach (var place in places)
                    {
                        var selected = place.PlaceID.ToString() == placeIdValue ? "selected" : "";
                        form += $"<option value='{place.PlaceID}' {selected}>{place.PlaceName}</option>";
                    }

                    form += "</select>" +
                            "<br><button type='submit'>Поиск</button>" +
                            "</form>";

                    if (context.Request.Method == "POST")
                    {
                        var formData = context.Request.Form;
                        var formPlacename = formData["placename"];
                        var placeid = formData["placeid"];

                        context.Response.Cookies.Append("placename", formPlacename, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });
                        context.Response.Cookies.Append("placeid", placeid, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });

                        form += "<p>Данные формы сохранены в куки</p>";
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

                    string savedTicketCount = context.Session.GetString("ticketcount") ?? "";
                    string savedCustomerId = context.Session.GetString("customerid") ?? "";

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

                    if (context.Request.Query.ContainsKey("ticketcount") && context.Request.Query.ContainsKey("customerid"))
                    {
                        context.Session.SetString("ticketcount", context.Request.Query["ticketcount"]);
                        context.Session.SetString("customerid", context.Request.Query["customerid"]);

                        form += "<p>Данные формы сохранены в сессии</p>";
                    }

                    await context.Response.WriteAsync(form);
                });
            });

            app.Run((context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";

                string HtmlString = "<HTML><HEAD><TITLE>Главная</TITLE></HEAD>" +
                "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                "<BODY><H1>Сайт городских мероприятий</H1>" +
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
                    var tableDataService = context.RequestServices.GetRequiredService<ITableDataService<Place>>();
                    var cachedDataHtml = await tableDataService.GetCachedTableDataHtml("places", cacheTime);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(cachedDataHtml);
                });
            });

            app.Map("/events", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var tableDataService = context.RequestServices.GetRequiredService<ITableDataService<Event>>();
                    var cachedDataHtml = await tableDataService.GetCachedTableDataHtml("events", cacheTime);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(cachedDataHtml);
                });
            });

            app.Map("/customers", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var tableDataService = context.RequestServices.GetRequiredService<ITableDataService<Customer>>();
                    var cachedDataHtml = await tableDataService.GetCachedTableDataHtml("customers", cacheTime);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(cachedDataHtml);
                });
            });

            app.Map("/organizers", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var tableDataService = context.RequestServices.GetRequiredService<ITableDataService<Organizer>>();
                    var cachedDataHtml = await tableDataService.GetCachedTableDataHtml("organizers", cacheTime);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(cachedDataHtml);
                });
            });

            app.Map("/ticketorders", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    var tableDataService = context.RequestServices.GetRequiredService<ITableDataService<TicketOrder>>();
                    var cachedDataHtml = await tableDataService.GetCachedTableDataHtml("ticketorders", cacheTime);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(cachedDataHtml);
                });
            });
        }
    }
}
