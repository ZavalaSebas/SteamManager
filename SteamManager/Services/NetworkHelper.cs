using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SteamManager.Services;

public static class NetworkHelper
{
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(Config.UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(Config.RequestTimeoutSeconds);
        return client;
    }

    public static async Task<JsonElement> FetchJsonAsync(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException($"Request to {url} failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new NetworkException($"Request to {url} timed out: {ex.Message}", ex);
        }
    }

    public static async Task DownloadFileAsync(string url, string destPath, IProgress<double>? progress = null)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        try
        {
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
            await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
                totalRead += bytesRead;
                if (totalBytes > 0)
                    progress?.Report((double)totalRead / totalBytes);
            }
        }
        catch (OperationCanceledException)
        {
            throw new NetworkException($"Download from {url} timed out", null!);
        }
        catch (Exception ex)
        {
            throw new NetworkException($"Download from {url} failed: {ex.Message}", ex);
        }
    }
}

public class NetworkException : Exception
{
    public NetworkException(string message, Exception? innerException) : base(message, innerException) { }
}
