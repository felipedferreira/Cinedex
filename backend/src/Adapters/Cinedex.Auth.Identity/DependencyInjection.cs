using Cinedex.Application.Abstractions;
using Cinedex.Auth.Identity.Constants;
using Cinedex.Auth.Identity.Entities;
using Cinedex.Auth.Identity.Options;
using Cinedex.Auth.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Auth.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthenticationAdapter(this IServiceCollection services)
    {
        services.AddDbContext<AuthDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        AuthDatabaseConstants.MigrationsHistoryTable,
                        AuthDatabaseConstants.AuthSchema))
                .UseCamelCaseNamingConvention();
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Configure<IConfiguration>((jwtOptions, configuration) =>
            {
                var section = configuration.GetSection(JwtOptions.SectionName);
                jwtOptions.Issuer = section["Issuer"] ?? string.Empty;
                jwtOptions.Audience = section["Audience"] ?? string.Empty;
                jwtOptions.SigningKey = section["SigningKey"] ?? string.Empty;

                if (int.TryParse(section["AccessTokenMinutes"], out var accessTokenMinutes))
                {
                    jwtOptions.AccessTokenMinutes = accessTokenMinutes;
                }

                if (int.TryParse(section["RefreshTokenDays"], out var refreshTokenDays))
                {
                    jwtOptions.RefreshTokenDays = refreshTokenDays;
                }
            });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
