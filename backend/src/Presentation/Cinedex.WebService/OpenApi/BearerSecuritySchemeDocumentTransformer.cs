using Cinedex.WebService.Constants;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cinedex.WebService.OpenApi;

/// <summary>
/// Declares the JWT bearer security scheme in the generated OpenAPI document.
/// </summary>
/// <remarks>
/// Neither <c>AddOpenApi</c> nor FastEndpoints infers this from the configured authentication
/// handler, and without a scheme in <c>components.securitySchemes</c> the Scalar API reference has
/// nothing to render an authentication panel from — the same way Swagger UI only shows its
/// "Authorize" button once the document declares a scheme.
/// </remarks>
internal sealed class BearerSecuritySchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);

        document.Components.SecuritySchemes[ApiConstants.Security.BearerScheme] = new OpenApiSecurityScheme
        {
            // "http" + "bearer" is what makes clients send `Authorization: Bearer <token>`; an
            // apiKey scheme would make them prompt for the whole header value instead.
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Paste the access token returned by POST /auth/login. "
                + "Do not include the \"Bearer \" prefix — it is added for you.",
        };

        return Task.CompletedTask;
    }
}
