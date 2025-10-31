using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;

namespace HangFire.Web.Jobs
{
    public class WebPuller
    {
        public readonly ILogger<WebPuller> _logger;

        public WebPuller(ILogger<WebPuller> logger)
        {
            _logger = logger;
        }

        public async Task GetRssItemUrlAsync(string rssFeedUrl, string fileName)
        {
            var directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var client = new HttpClient();
            var rssContent = await client.GetStringAsync(rssFeedUrl);

            using var xmlReader = XmlReader.Create(new StringReader(rssContent));
            var feed = SyndicationFeed.Load(xmlReader);
            
            var rssItemUrls = feed.Items.Select(item => item.Links.FirstOrDefault()?.Uri.ToString()).Where(url => url != null).ToList();

            var json = JsonSerializer.Serialize(rssItemUrls);
            await File.WriteAllTextAsync(fileName, json);
        }

        public async Task DownloadFileFromUrl(string url, string filePath)
        {
            using var client = new HttpClient();

            using (_logger.BeginScope("DownloadFileFormUrl({url}, {filePath})", url, filePath));
            {
                try
                {
                    _logger.LogInformation("Downloading file from {url}...", url);
                    using var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    _logger.LogInformation("Saving file to {filePath}...", filePath);
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

                } catch (HttpRequestException e)
                {
                    _logger.LogError("Error downloading file from {url}: {message}", url, e.Message);
                }
            }
        }
    }
}
