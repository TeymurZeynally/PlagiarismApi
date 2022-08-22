using CodePlagiarismApi.Contract;
using CodePlagiarismApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodePlagiarismApi.Controllers
{
	[ApiController]
	[Route("api/github")]
	public class GitHubClassroomController : ControllerBase
	{
		private readonly GitHubClassroomService _service;
		private readonly ILogger<GitHubClassroomController> _logger;

		public GitHubClassroomController(GitHubClassroomService service, ILogger<GitHubClassroomController> logger)
		{
			_service = service;
			_logger = logger;
		}

		[HttpPost]
		public async Task<GitHubClassroomCheckResult> CreateReport(GitHubClassroomCheckRequest request)
		{
			using var _ = _logger.BeginScope("GitHubClassroom request. Language: {Language}", request.Language);
			_logger.LogDebug(
				"Organization: {Organization} RepositoryPrefix: {RepositoryPrefix} ReportFilterRegex: {ReportFilterRegex} FileRegex: {FileRegex} Language: {Language} TemplateRepository: {TemplateRepository}",
				request.Organization,
				request.RepositoryPrefix,
				request.ReportFilterRegex,
				request.FileRegex,
				request.Language,
				request.TemplateRepository);
			return await _service.CreateReport(request);
		}
	}
}