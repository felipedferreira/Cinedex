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

                // Identity is the single source of truth for password policy. Every rule is defined
                // in PasswordPolicyConstants so the whole policy is visible in one place, and is set
                // explicitly here (even where it matches a framework default) so it cannot change if
                // a default changes.
                options.Password.RequiredLength = PasswordPolicyConstants.MinimumLength;
                options.Password.RequireDigit = PasswordPolicyConstants.RequireDigit;
                options.Password.RequireLowercase = PasswordPolicyConstants.RequireLowercase;
                options.Password.RequireUppercase = PasswordPolicyConstants.RequireUppercase;
                options.Password.RequireNonAlphanumeric = PasswordPolicyConstants.RequireNonAlphanumeric;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        // Shortens the password-reset token lifespan from the Identity default of 1 day.
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(1));

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
        services.AddScoped<IEmailSender<ApplicationUser>, EmailSenderAdapter>();

        return services;
    }
}