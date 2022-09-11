using System.Text.Json.Serialization;
using CodePlagiarismApi.Cache;
using CodePlagiarismApi.Extractors;
using CodePlagiarismApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moss.Client;
using Moss.Report.Client;
using Octokit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient((r) => new GitHubClient(new ProductHeaderValue("GitHubClassroomPlagiarismApi")) { Credentials = new Credentials(builder.Configuration.GetValue<string>("Secrets:GitHubToken")) });
builder.Services.AddTransient((r) => new MossClient(builder.Configuration.GetValue<long>("Secrets:MossUserId")));
builder.Services.AddHttpClient();
builder.Services.AddTransient((r) => new MossReportClient(r.GetRequiredService<IHttpClientFactory>().CreateClient()));
builder.Services.AddTransient<ZipArchiveExtractor>();
builder.Services.AddSingleton(r => new MossLinksCache(r.GetRequiredService<IMemoryCache>(), builder.Configuration.GetValue<TimeSpan>("Cache:Expiration")));
builder.Services.AddSingleton(r => new CodeFilesCache(r.GetRequiredService<IMemoryCache>(), builder.Configuration.GetValue<TimeSpan>("Cache:Expiration")));
builder.Services.AddTransient<GitHubClassroomService>();
builder.Services.AddTransient<MossService>();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
