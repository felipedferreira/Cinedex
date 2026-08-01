using Cinedex.Application.Abstractions;
using Cinedex.Application.Auth;
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
    public static IServiceCollection AddAuthenticationPersistence(this IServiceCollection services)
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

        return services;
    }

    /// <summary>
    /// Registers the background sweep that deletes refresh-token rows past their retention windows.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="AddAuthenticationAdapter"/> rather than folded into it.
    /// The web service has no business deleting rows on a timer, and — because the integration-test
    /// host runs real hosted services — registering this alongside the rest of the auth adapter
    /// would have the sweep running underneath tests that assert exact refresh-token row counts.
    /// Only <c>Cinedex.SchedulerWorker</c> calls this.
    /// </remarks>
    /// <param name="services">The service collection to add the sweep to.</param>
    /// <returns>The same <paramref name="services"/> instance so calls can be chained.</returns>
    public static IServiceCollection AddRefreshTokenCleanup(this IServiceCollection services)
    {
        services.AddOptions<RefreshTokenCleanupOptions>()
            .BindConfiguration(RefreshTokenCleanupOptions.SectionName)
            .Validate(
                options => options.Interval > TimeSpan.Zero,
                $"{RefreshTokenCleanupOptions.SectionName}:Interval must be greater than zero.")
            .Validate(
                options => options.ExpiredRetention >= TimeSpan.Zero,
                $"{RefreshTokenCleanupOptions.SectionName}:ExpiredRetention must not be negative.")
            .Validate(
                options => options.ReuseDetectionWindow > TimeSpan.Zero,
                $"{RefreshTokenCleanupOptions.SectionName}:ReuseDetectionWindow must be greater than zero.")
            .Validate(
                options => options.ReuseDetectionWindow >= options.ExpiredRetention,
                $"{RefreshTokenCleanupOptions.SectionName}:ReuseDetectionWindow must be at least as long as ExpiredRetention.")
            .Validate(
                options => options.BatchSize is > 0 and <= 10_000,
                $"{RefreshTokenCleanupOptions.SectionName}:BatchSize must be between 1 and 10000.")
            .Validate(
                options => options.MaxBatchesPerRun > 0,
                $"{RefreshTokenCleanupOptions.SectionName}:MaxBatchesPerRun must be greater than zero.")
            .ValidateOnStart();

        services.AddHostedService<RefreshTokenCleanupWorker>();

        return services;
    }

    public static IServiceCollection AddAuthenticationAdapter(this IServiceCollection services)
    {
        services.AddAuthenticationPersistence();

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

        // Shortens the password-reset token lifespan from the Identity default of 1 day. The value
        // comes from PasswordResetTokenPolicy, which the reset email also formats its "expires in"
        // copy from, so the configured expiry and what the recipient is told cannot drift apart.
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = PasswordResetTokenPolicy.Lifespan);

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
