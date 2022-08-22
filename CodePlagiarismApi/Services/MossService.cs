using System.Text.RegularExpressions;
using CodePlagiarismApi.Contract;
using CodePlagiarismApi.Extractors;
using Moss.Client;
using Moss.Report.Client;

namespace CodePlagiarismApi.Services
{
	public class MossService
	{
		private readonly MossClient _mossClient;
		private readonly MossReportClient _mossReportClient;
		private readonly ZipArchiveExtractor _zipArchiveExtractor;
		private readonly ILogger<MossService> _logger;

		public MossService(
			MossClient mossClient,
			MossReportClient mossReportClient,
			ZipArchiveExtractor zipArchiveExtractor,
			ILogger<MossService> logger)
		{
			_mossClient = mossClient;
			_mossReportClient = mossReportClient;
			_zipArchiveExtractor = zipArchiveExtractor;
			_logger = logger;
		}

		public async Task<MossCheckResult> CreateReport(MossLanguage language, Regex fileRegex, IFormFile zip, IFormFile? baseZip)
		{
			var reportUri = await GetReportUri(language, fileRegex, zip, baseZip).ConfigureAwait(false);
			var report = await GetReport(reportUri).ConfigureAwait(false);

			return new MossCheckResult()
			{
				ReportId = long.Parse(reportUri.Segments.Last().Trim('/')),
				ReportNo = long.Parse(reportUri.Segments.Reverse().Skip(1).First().Trim('/')),
				Report = report
			};
		}

		private async Task<Uri> GetReportUri(MossLanguage language, Regex fileRegex, IFormFile zip, IFormFile? baseZip)
		{
			_logger.LogDebug("Preparing zip files");
			var baseFiles = new List<(byte[] Contents, string Name)>();
			var files = new List<(byte[] Contents, string Name)>();

			if (baseZip != null)
			{
				var baseZipBytes = await GetFileBytes(baseZip).ConfigureAwait(false);
				await foreach (var file in _zipArchiveExtractor.Extract(baseZipBytes, fileRegex).ConfigureAwait(false)) baseFiles.Add(file);
			}

			var zipBytes = await GetFileBytes(zip).ConfigureAwait(false);
			await foreach (var file in _zipArchiveExtractor.Extract(zipBytes, fileRegex).ConfigureAwait(false)) files.Add(file);


			_logger.LogDebug("Sending request to stanford moss server with {Count} base files and {Count} files", baseFiles.Count, files.Count);
			baseFiles.ForEach(f => _mossClient.AddBaseFile(f.Contents, f.Name, language));
			files.ForEach(f => _mossClient.AddFile(f.Contents, f.Name, language));
			return _mossClient.Submit();
		}

		private async Task<MossReportRow[]> GetReport(Uri reportUri)
		{
			_logger.LogDebug("Sending request to stanford moss server for report {Report}", reportUri);
			return await _mossReportClient.GetReport(reportUri).ConfigureAwait(false);
		}

		private async Task<byte[]> GetFileBytes(IFormFile file)
		{
			await using var memoryStream = new MemoryStream();
			await file.CopyToAsync(memoryStream).ConfigureAwait(false);
			return memoryStream.ToArray();
		}
	}
}