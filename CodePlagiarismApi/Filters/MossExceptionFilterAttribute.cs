using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Moss.Client.Exceptions;
using Moss.Report.Client.Exceptions;
using System.Net;

namespace CodePlagiarismApi.Filters
{
	internal class MossExceptionFilterAttribute : ExceptionFilterAttribute
	{
		private readonly ILogger<MossExceptionFilterAttribute> _logger;

		public MossExceptionFilterAttribute(ILogger<MossExceptionFilterAttribute> logger)
		{
			_logger = logger;
		}

		public override void OnException(ExceptionContext context)
		{
			if (context.Exception is MossClientException)
			{
				_logger.LogError(new EventId(), context.Exception, "Unable to communicate with Moss");
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.FailedDependency;
				context.HttpContext.Response.HttpContext.Features.GetRequiredFeature<IHttpResponseFeature>().ReasonPhrase = "Unable to communicate with Moss";
				context.ExceptionHandled = true;
			}

			if (context.Exception is MossReportClientException)
			{
				_logger.LogError(new EventId(), context.Exception, "Unable to download Moss report");
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.FailedDependency;
				context.HttpContext.Response.HttpContext.Features.GetRequiredFeature<IHttpResponseFeature>().ReasonPhrase = "Unable to download Moss report";
				context.ExceptionHandled = true;
			}
		}
	}
}
