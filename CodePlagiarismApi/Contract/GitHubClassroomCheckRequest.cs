using Moss.Client;

namespace CodePlagiarismApi.Contract
{
	public class GitHubClassroomCheckRequest
	{
		public string Organization { get; set; }

		public string RepositoryPrefix { get; set; }

		public string? ReportFilterRegex { get; set; }

		public string FileRegex { get; set; }

		public MossLanguage Language { get; set; }

		public string? TemplateRepository { get; set; }
	}
}
