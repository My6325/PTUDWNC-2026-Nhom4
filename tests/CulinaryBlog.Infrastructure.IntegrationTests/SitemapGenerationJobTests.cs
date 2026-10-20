using System.Xml.Linq;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class SitemapGenerationJobTests
{
    [Fact]
    public async Task Execute_ShouldWriteOnlyPublicEntriesAndEscapeXml()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "culinary-sitemap-tests", Guid.NewGuid().ToString("N"));
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Recipes.AddRange(
            new Recipe { Title = "Public", Slug = "public", Status = RecipeStatus.Published },
            new Recipe { Title = "Draft", Slug = "draft", Status = RecipeStatus.Draft },
            new Recipe { Title = "Archived", Slug = "archived", Status = RecipeStatus.Archived },
            new Recipe { Title = "Deleted", Slug = "deleted", Status = RecipeStatus.Published, IsDeleted = true });
        await context.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicSite:BaseUrl"] = "https://example.com/blog/",
            ["Sitemap:OutputPath"] = "wwwroot/sitemap.xml"
        }).Build();
        var submitter = new GoogleSearchConsoleSitemapSubmitter(config, new NoNetworkClientFactory(),
            NullLogger<GoogleSearchConsoleSitemapSubmitter>.Instance);
        var job = new SitemapGenerationJob(context, config, new TestHostEnvironment(outputDirectory), submitter);

        try
        {
            await job.ExecuteAsync();
            var xml = XDocument.Load(Path.Combine(outputDirectory, "wwwroot", "sitemap.xml"));
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var url = Assert.Single(xml.Root!.Elements(ns + "url"));
            Assert.Equal("https://example.com/blog/recipes/public", url.Element(ns + "loc")!.Value);
            Assert.EndsWith("Z", url.Element(ns + "lastmod")!.Value);

            var escapedXml = SitemapXmlBuilder.Build("https://example.com/", [
                new SitemapXmlEntry("recipes/public?a=1&b=2", DateTime.UtcNow)]);
            Assert.Contains("&amp;", escapedXml);
            Assert.NotNull(XDocument.Parse(escapedXml).Root);
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("file:///tmp")]
    public void Builder_ShouldRejectNonPublicUrl(string url) =>
        Assert.Throws<InvalidOperationException>(() => SitemapXmlBuilder.Build(url, []));

    private sealed class NoNetworkClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Unexpected network access.");
    }

    private sealed class TestHostEnvironment(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "SitemapTests";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
