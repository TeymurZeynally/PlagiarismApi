using System.Text.Json.Serialization;
using CodePlagiarismApi.Extractors;
using CodePlagiarismApi.Services;
using Moss.Client;
using Moss.Report.Client;
using Octokit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient((r) => new GitHubClient(new ProductHeaderValue("GitHubClassroomPlagiarismApi")) { Credentials = new Credentials(builder.Configuration.GetValue<string>("Secrets:GitHubToken")) });
builder.Services.AddTransient((r) => new MossClient(builder.Configuration.GetValue<long>("Secrets:MossUserId")));
builder.Services.AddHttpClient();
builder.Services.AddTransient((r) => new MossReportClient(r.GetRequiredService<IHttpClientFactory>().CreateClient()));
builder.Services.AddTransient<ZipArchiveExtractor>();
builder.Services.AddTransient<GitHubClassroomService>();
builder.Services.AddTransient<MossService>();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.CreateLogger());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
