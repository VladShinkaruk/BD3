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

                    var clientInfo = $"<h1>���������� � �������:</h1>" +
                                     $"<p>IP: {context.Connection.RemoteIpAddress}</p>" +
                                     $"<p>����� �������: {context.Request.Method}</p>" +
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

                    // �������� ������ �� ����
                    var placeNameCookie = context.Request.Cookies["placename"] ?? "";
                    var placeIdCookie = context.Request.Cookies["placeid"] ?? "";

                    // �������� ������ �� ������
                    var placeNameSession = context.Session.GetString("placename") ?? "";
                    var placeIdSession = context.Session.GetString("placeid") ?? "";

                    // ��������� �������� �� ������ ��� ��� ������
                    string placenameValue = !string.IsNullOrEmpty(placeNameCookie) ? placeNameCookie : placeNameSession;
                    string placeIdValue = !string.IsNullOrEmpty(placeIdCookie) ? placeIdCookie : placeIdSession;

                    context.Response.ContentType = "text/html; charset=utf-8";
                    string form = "<form method='POST' action='/searchform1'>" +
                                  $"������� �������� �����: <input type='text' name='placename' value='{placenameValue}' />" +
                                  "<br>�������� �����: <select name='placeid'>";

                    foreach (var place in places)
                    {
                        // �������������� �������� ����������� ��������
                        var selected = place.PlaceID.ToString() == placeIdValue ? "selected" : "";
                        form += $"<option value='{place.PlaceID}' {selected}>{place.PlaceName}</option>";
                    }

                    form += "</select>" +
                            "<br><button type='submit'>�����</button>" +
                            "</form>";

                    // ���� ����� ����������
                    if (context.Request.Method == "POST")
                    {
                        var formData = context.Request.Form;
                        var formPlacename = formData["placename"]; // ������� ��� ���������� �� formPlacename
                        var placeid = formData["placeid"];

                        // ��������� ������ � ���� � ������
                        context.Response.Cookies.Append("placename", formPlacename, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });
                        context.Response.Cookies.Append("placeid", placeid, new CookieOptions { Expires = DateTimeOffset.Now.AddMinutes(10) });

                        context.Session.SetString("placename", formPlacename);
                        context.Session.SetString("placeid", placeid);

                        // ���������� ����������� �����
                        form += "<p>������ ����� ��������� � ���� � ������.</p>";
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

                    // ������ ������ �� ������
                    string savedTicketCount = context.Session.GetString("ticketcount") ?? "";
                    string savedCustomerId = context.Session.GetString("customerid") ?? "";

                    // ������������ ����� � ������������ ����������
                    string form = $"<form method='GET' action='/searchform2'>" +
                                  $"������� ���������� �������: <input type='number' name='ticketcount' value='{savedTicketCount}' />" +
                                  $"<br>�������� �������: <select name='customerid'>";

                    foreach (var customer in customers)
                    {
                        bool isSelected = customer.CustomerID.ToString() == savedCustomerId;
                        form += $"<option value='{customer.CustomerID}' {(isSelected ? "selected" : "")}>{customer.FullName}</option>";
                    }

                    form += "</select>" +
                            "<br><button type='submit'>�����</button>" +
                            "</form>";

                    // ���������� ������ ����� � ������
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

                string HtmlString = "<HTML><HEAD><TITLE>�������</TITLE></HEAD>" +
                "<META http-equiv='Content-Type' content='text/html; charset=utf-8'/>" +
                "<BODY><H1>����� ���������� �� ���� ��������� �����������</H1>" +
                "<BR><A href='/places'>�����</A>" +
                "<BR><A href='/events'>�����������</A>" +
                "<BR><A href='/customers'>�������</A>" +
                "<BR><A href='/organizers'>������������</A>" +
                "<BR><A href='/ticketorders'>������ �������</A>" +
                "<BR><A href='/info'>���������� � �������</A>" +
                "<BR><A href='/searchform1'>����� ������ 1</A>" +
                "<BR><A href='/searchform2'>����� ������ 2</A>" +
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
                await context.Response.WriteAsync("<h1>������ ��������� �� ���� � ��������� � ���</h1>");
            }
            else
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("<h1>������ �������� �� ����</h1>");
            }

            string htmlResponse = "<ul>";

            foreach (var item in data)
            {
                if (item is Place place)
                {
                    htmlResponse += $"<li>�����: ID = {place.PlaceID}, �������� = {place.PlaceName}, ���������� = {place.Geolocation}</li>";
                }
                else if (item is Event evnt)
                {
                    htmlResponse += $"<li>�����������: ID = {evnt.EventID}, �������� = {evnt.EventName}, ���� = {evnt.EventDate}, ���� ������ = {evnt.TicketPrice}</li>";
                }
                else if (item is Customer customer)
                {
                    htmlResponse += $"<li>������: ID = {customer.CustomerID}, ��� = {customer.FullName}, ���������� ������ = {customer.PassportData}</li>";
                }
                else if (item is Organizer organizer)
                {
                    htmlResponse += $"<li>�����������: ID = {organizer.OrganizerID}, ��� = {organizer.FullName}, ��������� = {organizer.Post}</li>";
                }
                else if (item is TicketOrder order)
                {
                    htmlResponse += $"<li>�����: ID = {order.OrderID}, ������ ID = {order.CustomerID}, ����������� ID = {order.EventID}, ���������� ������� = {order.TicketCount}</li>";
                }
                else
                {
                    htmlResponse += $"<li>����������� ������: {item}</li>";
                }
            }

            htmlResponse += "</ul>";

            await context.Response.WriteAsync(htmlResponse);
        }
    }
}