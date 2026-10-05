using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CulinaryBlog.API.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Dán accessToken (không cần gõ chữ Bearer)."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}