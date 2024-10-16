using Microsoft.Extensions.Caching.Memory;

namespace WebCityEvents.Services
{
    public interface ITableDataService<T> where T : class
    {
        Task<string> GetCachedTableDataHtml(string cacheKey, int cacheTime);
    }
}