using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
		private readonly ILogger<GitHubClassroomService> _logger;

		public GitHubClassroomService(
			GitHubClient gitHubClient,
			MossClient mossClient,
			MossReportClient mossReportClient,
			ZipArchiveExtractor zipArchiveExtractor,
			ILogger<GitHubClassroomService> logger)
		{
			_gitHubClient = gitHubClient;
			_mossClient = mossClient;
			_mossReportClient = mossReportClient;
			_zipArchiveExtractor = zipArchiveExtractor;
			_logger = logger;
		}

		public async Task<GitHubClassroomCheckResult> CreateReport(GitHubClassroomCheckRequest request)
		{
			var filterRegex = new Regex(request.ReportFilterRegex ?? ".*");
			var files = await GetAllFiles(request).ConfigureAwait(false);
			var baseFiles = await GetBaseFiles(request).ConfigureAwait(false);
			var reportUri = GetReportUri(files, baseFiles, request.Language);
			var report = await GetReport(reportUri).ConfigureAwait(false);

			return new GitHubClassroomCheckResult
			{
				ReportId = long.Parse(reportUri.Segments.Last().Trim('/')),
				ReportNo = long.Parse(reportUri.Segments.Reverse().Skip(1).First().Trim('/')),
				Report = report.Where(x => filterRegex.IsMatch(x.FirstFile) || filterRegex.IsMatch(x.SecondFile)).ToArray()
			};
		}

		private async Task<List<(byte[] Contents, string Name)>> GetAllFiles(GitHubClassroomCheckRequest request)
		{
			var fileRegex = new Regex(request.FileRegex);
			var files = new ConcurrentBag<(byte[] Contents, string Name)>();
			_logger.LogDebug("Getting repositories for {Organization} with prefix {Prefix} ", request.Organization, request.RepositoryPrefix);
			var repositories = await _gitHubClient.Repository.GetAllForOrg(request.Organization).ConfigureAwait(false);
			var neededRepositories = repositories.Where(x => x.Name.StartsWith(request.RepositoryPrefix)).ToArray();

			_logger.LogDebug("Found {Length} repos: {Repos} ", neededRepositories.Length, string.Join(", ", neededRepositories.Select(x => x.FullName)));
			var repositoriesProcessingTasks = neededRepositories.Select(repository => Task.Run(async () =>
			{
				_logger.LogDebug("Processing repo {Repo} ", repository.FullName);
				var zipRepoBytes = await _gitHubClient.Repository.Content.GetArchive(repository.Id, ArchiveFormat.Zipball).ConfigureAwait(false);
				await foreach (var file in _zipArchiveExtractor.Extract(zipRepoBytes, fileRegex).ConfigureAwait(false)) files.Add(file);
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

		private Uri GetReportUri(List<(byte[] Contents, string Name)> files, List<(byte[] Contents, string Name)> baseFiles, MossLanguage language)
		{
			_logger.LogDebug("Sending request to stanford moss server with {BaseFilesCount} base files and {FilesCount} files", baseFiles.Count, files.Count);
			var githubHashRegex = new Regex(@"-[a-z0-9]{40}\/");
			baseFiles.ForEach(f => _mossClient.AddBaseFile(f.Contents, githubHashRegex.Replace(f.Name, "/"), language));
			files.ForEach(f => _mossClient.AddFile(f.Contents, githubHashRegex.Replace(f.Name, "/"), language));
			return _mossClient.Submit();
		}

		private async Task<MossReportRow[]> GetReport(Uri reportUri)
		{
			_logger.LogDebug("Sending request to stanford moss server for report {Report}", reportUri);
			return await _mossReportClient.GetReport(reportUri).ConfigureAwait(false);
		}
	}
}