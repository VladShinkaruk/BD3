using Microsoft.Extensions.Caching.Memory;

namespace WebCityEvents.Services
{
    public interface ICachedEntityService<T> where T : class
    {
        IEnumerable<T> GetEntities(int rowsNumber = 20);
        IEnumerable<T> GetEntitiesFromCache(string cacheKey, int rowsNumber = 20, int cacheTimeSeconds = 300);
    }
}