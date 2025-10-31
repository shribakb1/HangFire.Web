using Hangfire;
using HangFire.Web.Jobs;
using System.Text.Json;

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


RecurringJob.AddOrUpdate<WebPuller>(
    "pull-rss-feed",
    x => x.GetRssItemUrlAsync("https://consultwithgriff.com/rss.xml", "d:\\rss\\consultwithgriff_rss_urls.json"),
    "* * * * *");

app.MapGet("/pull", (IBackgroundJobClient client) =>
{
    var url = "https://consultwithgriff.com/rss.xml";
    var directory = $"d:\\rss";
    var fileName = "consultwithgriff_rss_urls.json";
    var tempPath = Path.Combine(directory, fileName);

    client.Enqueue<WebPuller>(x => x.GetRssItemUrlAsync(url, tempPath));
});

app.MapGet("/sync", (IBackgroundJobClient client) =>
{
    var directory = $"d:\\rss";
    var fileName = "consultwithgriff_rss_urls.json";

    var path = Path.Combine(directory, fileName);
    var json = File.ReadAllText(path);
    var rssItemUrls = JsonSerializer.Deserialize<List<string>>(json);

    var outputPath = Path.Combine(directory, "output");
    if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

    if (rssItemUrls == null || rssItemUrls.Count == 0) return;

    var delayInSeconds = 5;

    foreach (var url in rssItemUrls)
    {
        var u = new Uri(url);
        var stub = u.Segments.Last();

        if (stub.EndsWith("/")) stub = stub.Substring(0, stub.Length - 1);
        stub += ".html";

        var filePath = Path.Combine(outputPath, stub);

        client.Schedule<WebPuller>(p => p.DownloadFileFromUrl(url, filePath),
            delay: TimeSpan.FromSeconds(delayInSeconds));

        delayInSeconds += 5;
    }

});


app.Run();
