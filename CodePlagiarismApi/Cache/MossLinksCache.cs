using Microsoft.Extensions.Caching.Memory;

namespace CodePlagiarismApi.Cache
{
    public class MossLinksCache
    {
        public MossLinksCache(IMemoryCache memoryCache, TimeSpan expiration)
        {
            _memoryCache = memoryCache;
            _expiration = expiration;
        }

        public void Put(string key, Uri uri)
        {
            _memoryCache.Set(key, uri, DateTimeOffset.UtcNow.Add(_expiration));
        }

        public bool TryGetUri(string key, out Uri uri)
        {
            return _memoryCache.TryGetValue(key, out uri);
        }


        private IMemoryCache _memoryCache;
        private TimeSpan _expiration;
    }

}
