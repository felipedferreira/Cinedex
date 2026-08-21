using Cinedex.WebService.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cinedex.WebService.OpenApi;

/// <summary>
/// Marks every operation that requires authorization with the JWT bearer security requirement.
/// </summary>
/// <remarks>
/// Declared per-operation rather than once at the document root so the endpoints FastEndpoints
/// configured with <c>AllowAnonymous()</c> (login, register, refresh, the password-reset pair)
/// stay unlocked in the API reference instead of inheriting a requirement they do not enforce.
/// </remarks>
internal sealed class BearerSecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any()
            && !metadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            // The reference is resolved against the host document at serialization time, so it
            // still binds to the scheme the document transformer adds afterwards.
            [new OpenApiSecuritySchemeReference(ApiConstants.Security.BearerScheme, context.Document)] = [],
        });

        return Task.CompletedTask;
    }
}
