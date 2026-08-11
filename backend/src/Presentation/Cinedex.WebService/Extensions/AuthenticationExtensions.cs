using System.Text;
using Cinedex.Auth.Identity.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cinedex.WebService.Extensions;

/// <summary>
/// Registers JWT bearer authentication and authorization for the API.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT bearer authentication using the <c>Jwt</c> configuration section and enables
    /// authorization. The signing key must match the one the identity adapter signs tokens with.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance so calls can be chained.</returns>
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                if (string.IsNullOrWhiteSpace(jwt.SigningKey))
                {
                    throw new InvalidOperationException(
                        $"Configuration '{JwtOptions.SectionName}:SigningKey' is not configured.");
                }

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
