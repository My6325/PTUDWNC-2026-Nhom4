using System.Xml.Linq;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed record SitemapXmlEntry(string RelativePath, DateTime LastModified);

public static class SitemapXmlBuilder
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static bool IsValidPublicBaseUrl(string? publicBaseUrl) =>
        Uri.TryCreate(publicBaseUrl?.TrimEnd('/'), UriKind.Absolute, out var publicUri) &&
        (publicUri.Scheme == Uri.UriSchemeHttps || publicUri.Scheme == Uri.UriSchemeHttp);

    public static string Build(string publicBaseUrl, IEnumerable<SitemapXmlEntry> entries)
    {
        if (!IsValidPublicBaseUrl(publicBaseUrl) ||
            !Uri.TryCreate(publicBaseUrl.TrimEnd('/'), UriKind.Absolute, out var publicUri))
        {
            throw new InvalidOperationException("PublicSite:BaseUrl must be configured as an absolute HTTP(S) URL.");
        }

        var urls = entries.Select(entry => new XElement(SitemapNamespace + "url",
            new XElement(SitemapNamespace + "loc", new Uri(publicUri, entry.RelativePath.TrimStart('/')).AbsoluteUri),
            new XElement(SitemapNamespace + "lastmod", entry.LastModified.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")),
            new XElement(SitemapNamespace + "changefreq", "weekly")));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset", urls));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}

public sealed class SitemapGenerationJob(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    GoogleSearchConsoleSitemapSubmitter searchConsoleSubmitter)
{
    public async Task<string> GenerateXmlAsync(CancellationToken cancellationToken = default)
    {
        var publicBaseUrl = configuration["PublicSite:BaseUrl"];
        var recipes = await dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published && !recipe.IsDeleted)
            .Select(recipe => new SitemapXmlEntry(
                $"recipes/{Uri.EscapeDataString(recipe.Slug)}",
                recipe.UpdatedAt ?? recipe.PublishedAt ?? recipe.CreatedAt))
            .ToListAsync(cancellationToken);
        var categories = await dbContext.Categories.AsNoTracking()
            .Where(category => !category.IsDeleted)
            .Select(category => new SitemapXmlEntry(
                $"categories/{Uri.EscapeDataString(category.Slug)}",
                category.UpdatedAt ?? category.CreatedAt))
            .ToListAsync(cancellationToken);

        return SitemapXmlBuilder.Build(publicBaseUrl ?? string.Empty, recipes.Concat(categories));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var xml = await GenerateXmlAsync(cancellationToken);
        var outputPath = configuration["Sitemap:OutputPath"] ?? Path.Combine("wwwroot", "sitemap.xml");
        var fullPath = Path.GetFullPath(Path.IsPathRooted(outputPath)
            ? outputPath
            : Path.Combine(hostEnvironment.ContentRootPath, outputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, xml, cancellationToken);

        var publicBaseUrl = configuration["PublicSite:BaseUrl"]!;
        var sitemapUrl = new Uri(new Uri(publicBaseUrl.TrimEnd('/') + "/"), "sitemap.xml").AbsoluteUri;
        await searchConsoleSubmitter.SubmitAsync(sitemapUrl, cancellationToken);
    }
}
