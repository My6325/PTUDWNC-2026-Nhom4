using System.Diagnostics.Metrics;

namespace CulinaryBlog.Application.Observability;

public static class RecipeMetrics
{
    public const string MeterName = "CulinaryBlog.Recipes";

    private static readonly Meter Meter = new(MeterName);
    internal static readonly Counter<long> Created = Meter.CreateCounter<long>("culinaryblog.recipes.created", "{recipe}");
    internal static readonly Counter<long> Published = Meter.CreateCounter<long>("culinaryblog.recipes.published", "{recipe}");
}
