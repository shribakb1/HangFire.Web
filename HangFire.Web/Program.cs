using Hangfire;
using HangFire.Web.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddHangfire(config =>
{
    var connectionsString = builder.Configuration.GetConnectionString("HangFireConnection");

    config.UseSqlServerStorage(connectionsString);
    config.UseColouredConsoleLogProvider();
});

builder.Services.AddHangfireServer();

var app = builder.Build();

app.MapHangfireDashboard();

app.MapGet("/", () => "Hello World!");

app.MapGet("/pull", (IBackgroundJobClient client) =>
{
    var url = "https://consultwithgriff.com/rss.xml";
    var directory = $"d:\\rss";
    var fileName = "consultwithgriff_rss_urls.json";
    var tempPath = Path.Combine(directory, fileName);

    client.Enqueue<WebPuller>(x => x.GetRssItemUrlAsync(url, tempPath));
});

app.Run();
