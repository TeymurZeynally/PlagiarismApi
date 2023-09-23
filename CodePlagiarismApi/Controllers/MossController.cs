using System.Text.RegularExpressions;
using CodePlagiarismApi.Contract;
using CodePlagiarismApi.Filters;
using CodePlagiarismApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moss.Client;

namespace CodePlagiarismApi.Controllers
{
	[ApiController]
	[Route("api/moss")]
	[TypeFilter(typeof(MossExceptionFilterAttribute))]
	public class MossController : ControllerBase
	{
		private readonly MossService _service;
		private readonly ILogger<MossController> _logger;

		public MossController(MossService service, ILogger<MossController> logger)
		{
			_service = service;
			_logger = logger;
		}

		[HttpPost]
		public async Task<MossCheckResult> CreateReport(MossLanguage language, string fileRegex, IFormFile zip, IFormFile? baseZip)
		{
			using var _ = _logger.BeginScope("Moss request. Language: {Language}", language);
			return await _service.CreateReport(language, new Regex(fileRegex), zip, baseZip).ConfigureAwait(false);
		}
	}
}