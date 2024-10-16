using System.Text;
using System.Threading.Tasks;
using WebCityEvents.Models;

namespace WebCityEvents.Services
{
    public class TableDataService<T> : ITableDataService<T> where T : class
    {
        private readonly ICachedEntityService<T> _cachedEntityService;

        public TableDataService(ICachedEntityService<T> cachedEntityService)
        {
            _cachedEntityService = cachedEntityService;
        }

        public async Task<string> GetCachedTableDataHtml(string cacheKey, int cacheTime)
        {
            var data = _cachedEntityService.GetEntitiesFromCache(cacheKey, 20, cacheTime);
            var htmlBuilder = new StringBuilder();

            htmlBuilder.Append("<ul>");

            foreach (var item in data)
            {
                if (item is Place place)
                {
                    htmlBuilder.Append($"<li>Место: ID = {place.PlaceID}, Название = {place.PlaceName}, Геолокация = {place.Geolocation}</li>");
                }
                else if (item is Event evnt)
                {
                    htmlBuilder.Append($"<li>Мероприятие: ID = {evnt.EventID}, Название = {evnt.EventName}, Дата = {evnt.EventDate}, Цена билета = {evnt.TicketPrice}</li>");
                }
                else if (item is Customer customer)
                {
                    htmlBuilder.Append($"<li>Клиент: ID = {customer.CustomerID}, Имя = {customer.FullName}, Паспортные данные = {customer.PassportData}</li>");
                }
                else if (item is Organizer organizer)
                {
                    htmlBuilder.Append($"<li>Организатор: ID = {organizer.OrganizerID}, Имя = {organizer.FullName}, Должность = {organizer.Post}</li>");
                }
                else if (item is TicketOrder order)
                {
                    htmlBuilder.Append($"<li>Заказ: ID = {order.OrderID}, Клиент ID = {order.CustomerID}, Мероприятие ID = {order.EventID}, Количество билетов = {order.TicketCount}</li>");
                }
            }

            htmlBuilder.Append("</ul>");

            return await Task.FromResult(htmlBuilder.ToString());
        }
    }
}