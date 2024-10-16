using Microsoft.Extensions.Caching.Memory;
using WebCityEvents.Data;

namespace WebCityEvents.Services
{

    public class CachedEntityService<T> : ICachedEntityService<T> where T : class
    {
        private readonly EventContext _dbContext;
        private readonly IMemoryCache _memoryCache;

        public CachedEntityService(EventContext dbContext, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _memoryCache = memoryCache;
        }

        public IEnumerable<T> GetEntities(int rowsNumber = 20)
        {
            return _dbContext.Set<T>().Take(rowsNumber).ToList();
        }

        public IEnumerable<T> GetEntitiesFromCache(string cacheKey, int rowsNumber = 20, int cacheTimeSeconds = 300)
        {
            if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<T> entities))
            {
                entities = GetEntities(rowsNumber);

                _memoryCache.Set(cacheKey, entities, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheTimeSeconds)
                });
            }

            return entities;
        }
    }
}