using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class GoogleSearchConsoleSitemapSubmitter(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<GoogleSearchConsoleSitemapSubmitter> logger)
{
    public async Task SubmitAsync(string sitemapUrl, CancellationToken cancellationToken = default)
    {
        var clientId = configuration["GoogleSearchConsole:ClientId"];
        var clientSecret = configuration["GoogleSearchConsole:ClientSecret"];
        var refreshToken = configuration["GoogleSearchConsole:RefreshToken"];
        var siteUrl = configuration["GoogleSearchConsole:SiteUrl"];
        var settings = new[] { clientId, clientSecret, refreshToken, siteUrl };

        if (settings.All(string.IsNullOrWhiteSpace))
        {
            logger.LogInformation("Google Search Console submission is disabled because credentials are not configured.");
            return;
        }

        if (settings.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "GoogleSearchConsole:ClientId, ClientSecret, RefreshToken, and SiteUrl must all be configured together.");
        }

        var tokenClient = httpClientFactory.CreateClient();
        using var tokenResponse = await tokenClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["refresh_token"] = refreshToken!,
                ["grant_type"] = "refresh_token"
            }),
            cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        await using var tokenStream = await tokenResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var tokenDocument = await JsonDocument.ParseAsync(tokenStream, cancellationToken: cancellationToken);
        var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Google OAuth token response did not include an access_token.");
        }

        var submitUrl = "https://www.googleapis.com/webmasters/v3/sites/" +
                        Uri.EscapeDataString(siteUrl!) + "/sitemaps/" +
                        Uri.EscapeDataString(sitemapUrl);
        var searchConsoleClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, submitUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await searchConsoleClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Submitted sitemap {SitemapUrl} to Google Search Console.", sitemapUrl);
    }
}
