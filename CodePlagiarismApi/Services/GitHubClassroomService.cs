using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CodePlagiarismApi.Cache;
using CodePlagiarismApi.Contract;
using CodePlagiarismApi.Extractors;
using Moss.Client;
using Moss.Report.Client;
using Octokit;

namespace CodePlagiarismApi.Services
{

	public class GitHubClassroomService 
	{
		private readonly GitHubClient _gitHubClient;
		private readonly MossClient _mossClient;
		private readonly MossReportClient _mossReportClient;
		private readonly ZipArchiveExtractor _zipArchiveExtractor;
		private readonly CodeFilesCache _cache;
		private readonly MossLinksCache _mossLinksCache;
		private readonly ILogger<GitHubClassroomService> _logger;

		public GitHubClassroomService(
			GitHubClient gitHubClient,
			MossClient mossClient,
			MossReportClient mossReportClient,
			ZipArchiveExtractor zipArchiveExtractor,
            CodeFilesCache codeCashe,
            MossLinksCache mossLinksCache,
            ILogger<GitHubClassroomService> logger)
		{
			_gitHubClient = gitHubClient;
			_mossClient = mossClient;
			_mossReportClient = mossReportClient;
			_zipArchiveExtractor = zipArchiveExtractor;
			_cache = codeCashe;
			_mossLinksCache = mossLinksCache;
            _logger = logger;
		}

		public async Task<GitHubClassroomCheckResult> CreateReport(GitHubClassroomCheckRequest request)
		{
			var filterRegex = new Regex(request.ReportFilterRegex ?? ".*");
            var cacheKey = GetCacheKey(request);
            var files = await GetAllFiles(request, cacheKey).ConfigureAwait(false);
			var baseFiles = await GetBaseFiles(request).ConfigureAwait(false);
            var reportUri = GetReportUri(files, baseFiles, request.Language, cacheKey);
			var report = await GetReport(reportUri).ConfigureAwait(false);

			return new GitHubClassroomCheckResult
			{
				ReportId = long.Parse(reportUri.Segments.Last().Trim('/')),
				ReportNo = long.Parse(reportUri.Segments.Reverse().Skip(1).First().Trim('/')),
				Report = report.Where(x => filterRegex.IsMatch(x.FirstFile) || filterRegex.IsMatch(x.SecondFile)).ToArray()
			};
		}

		private async Task<List<(byte[] Contents, string Name, bool isFromCache)>> GetAllFiles(GitHubClassroomCheckRequest request, string cacheKey)
		{
			var fileRegex = new Regex(request.FileRegex);
			var files = new ConcurrentBag<(byte[] Contents, string Name, bool isFromCache)>();
			_logger.LogDebug("Getting repositories for {Organization} with prefix {Prefix} ", request.Organization, request.RepositoryPrefix);
			var repositories = await _gitHubClient.Repository.GetAllForOrg(request.Organization).ConfigureAwait(false);
			var neededRepositories = repositories.Where(x => x.Name.StartsWith(request.RepositoryPrefix)).ToArray();

			_logger.LogDebug("Found {Length} repos: {Repos} ", neededRepositories.Length, string.Join(", ", neededRepositories.Select(x => x.FullName)));
			var repositoriesProcessingTasks = neededRepositories.Select(repository => Task.Run(async () =>
			{
				using var _ = _logger.BeginScope("Processing repo {Repo}", repository.FullName);
				_logger.LogDebug("Getting last commit");
				var commits = await _gitHubClient.Repository.Commit.GetAll(repository.Id).ConfigureAwait(false);
				var lastCommit = commits.OrderByDescending(x => x.Commit.Committer.Date).First();
                _logger.LogDebug("Last commit is {Sha}", lastCommit.Sha);

                _logger.LogDebug("Checking cache");
                var isCached = _cache.Contains(cacheKey, repository.Id, lastCommit.Sha);
                if (!isCached)
				{
                    _logger.LogDebug("Cache miss, downloading repository...");
                    var zipRepoBytes = await _gitHubClient.Repository.Content.GetArchive(repository.Id, ArchiveFormat.Zipball).ConfigureAwait(false);
					var codeFiles = new List<(byte[] Contents, string Name)>();
                    await foreach (var file in _zipArchiveExtractor.Extract(zipRepoBytes, fileRegex).ConfigureAwait(false)) codeFiles.Add(file);

                    _logger.LogDebug("Caching {Count} files...", codeFiles.Count);
                    _cache.Put(cacheKey, repository.Id, lastCommit.Sha, codeFiles);
                }

				var cachedFiles = _cache.GetFiles(cacheKey, repository.Id);
                _logger.LogDebug("Adding {Count} files from cache into list of files for check", cachedFiles.Count);
                cachedFiles.ForEach(f => files.Add((f.Contents, f.Name, isCached)));
			}));
			await Task.WhenAll(repositoriesProcessingTasks.ToArray()).ConfigureAwait(false);

			return files.ToList();
		}

		private async Task<List<(byte[] Contents, string Name)>> GetBaseFiles(GitHubClassroomCheckRequest request)
		{
			var fileRegex = new Regex(request.FileRegex);
			var baseFiles = new List<(byte[] Contents, string Name)>();
			if (request.TemplateRepository != null)
			{
				_logger.LogDebug("Getting template repository {Repository} ", request.TemplateRepository);
				var parts = request.TemplateRepository.Split("/");
				var zipRepoBytes = await _gitHubClient.Repository.Content.GetArchive(parts.First(), parts.Last(), ArchiveFormat.Zipball).ConfigureAwait(false);
				await foreach (var file in _zipArchiveExtractor.Extract(zipRepoBytes, fileRegex).ConfigureAwait(false)) baseFiles.Add(file);
			}

			return baseFiles;
		}

		private Uri GetReportUri(List<(byte[] Contents, string Name, bool isFromCache)> files, List<(byte[] Contents, string Name)> baseFiles, MossLanguage language, string cacheKey)
		{
            _logger.LogDebug("Checking uri cache by key {CacheKey}", cacheKey);
            if (files.All(x => x.isFromCache) && _mossLinksCache.TryGetUri(cacheKey, out var uri))
			{
                _logger.LogDebug("Found uri {Uri}", uri);
                return uri;
			}

			_logger.LogDebug("Sending request to stanford moss server with {BaseCount} base files and {Count} files", baseFiles.Count, files.Count);
			var githubHashRegex = new Regex(@"-[a-z0-9]{40}\/");
			baseFiles.ForEach(f => _mossClient.AddBaseFile(f.Contents, githubHashRegex.Replace(f.Name, "/"), language));
			files.ForEach(f => _mossClient.AddFile(f.Contents, githubHashRegex.Replace(f.Name, "/"), language));
			var resultUri = _mossClient.Submit();

            _logger.LogDebug("Checking uri {Uri}", resultUri);
            _mossLinksCache.Put(cacheKey, resultUri);
			return resultUri;
        }

		private async Task<MossReportRow[]> GetReport(Uri reportUri)
		{
			_logger.LogDebug("Sending request to stanford moss server for report {Report}", reportUri);
			return await _mossReportClient.GetReport(reportUri).ConfigureAwait(false);
		}

		private string GetCacheKey(GitHubClassroomCheckRequest request)
		{
			var requestString = $"{request.Organization}{request.RepositoryPrefix}{request.FileRegex}{request.Language}{request.TemplateRepository}";
			return Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(requestString)));
        }
	}
}