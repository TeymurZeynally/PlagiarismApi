using Moss.Report.Client;

namespace CodePlagiarismApi.Contract
{
	public class GitHubClassroomCheckResult
	{
		public long ReportNo { get; set; }

		public long ReportId { get; set; }

		public MossReportRow[] Report { get; set; }
	}
}
