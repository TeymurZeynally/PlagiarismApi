using Microsoft.Extensions.Caching.Memory;

namespace CodePlagiarismApi.Cache
{
    public class CodeFilesCache
    {
        public CodeFilesCache(IMemoryCache memoryCache, TimeSpan expiration)
        {
            _memoryCache = memoryCache;
            _expiration = expiration;
        }

        public void Put(string cacheKey, long repositoryId, string hash, List<(byte[] Contents, string Name)> files)
        {
            _memoryCache.Set(ComputeKey(cacheKey, repositoryId), (hash, files), DateTimeOffset.UtcNow.Add(_expiration));
        }

        public bool Contains(string cacheKey, long repositoryId, string hash)
        {
            (string Hash, List<(byte[] Contents, string Name)> Files) cashedData;
            return _memoryCache.TryGetValue(ComputeKey(cacheKey, repositoryId), out cashedData) && cashedData.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }


        public List<(byte[] Contents, string Name)> GetFiles(string cacheKey, long repositoryId)
        {
            return _memoryCache.Get<(string Hash, List<(byte[] Contents, string Name)> Files)>(ComputeKey(cacheKey, repositoryId)).Files;
        }

        private string ComputeKey(string cacheKey, long repositoryId)
        {
            return $"{cacheKey}-{repositoryId}";
        }

        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _expiration;
    }

}
