using Cinedex.Auth.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cinedex.WebService.IntegrationTests.Auth;

[Collection(RefreshTokenCleanupCollection.Name)]
public sealed class RefreshTokenCleanupTests(RefreshTokenCleanupFixture fixture)
{
    // Long enough that only the startup sweep runs, so every assertion below describes the effect of
    // exactly one sweep rather than however many ticks happened to fit in the polling window.
    private const string SingleSweepInterval = "01:00:00";

    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Sweep_WithExpiredTokenPastRetention_DeletesIt()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        // Expired three days ago, retention is one day.
        var stale = await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);

        await this.RunSweepUntilCountAsync(0);

        Assert.DoesNotContain(stale, await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithExpiredTokenInsideRetention_KeepsIt()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        // Expired an hour ago; retention is one day, so it is not eligible yet.
        var recent = await this.SeedTokenAsync(userId, now.AddHours(-1), revokedAtUtc: null);

        // A row that must go, so the poll has something to wait on and cannot pass vacuously.
        await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);

        await this.RunSweepUntilCountAsync(1);

        Assert.Equal([recent], await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithRevokedTokenPastReuseWindow_DeletesIt()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        // Revoked 20 days ago, reuse-detection window is 14.
        var stale = await this.SeedTokenAsync(userId, now.AddDays(-13), revokedAtUtc: now.AddDays(-20));

        await this.RunSweepUntilCountAsync(0);

        Assert.DoesNotContain(stale, await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithRevokedTokenInsideReuseWindow_KeepsIt()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        // Revoked 10 days ago and long expired. It stays because it is inside the reuse-detection
        // window: this row is what makes a replay of that token recognisable as reuse rather than as
        // an unknown token.
        var revoked = await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: now.AddDays(-10));

        await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);

        await this.RunSweepUntilCountAsync(1);

        Assert.Equal([revoked], await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithLiveUnexpiredToken_KeepsIt()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        var live = await this.SeedTokenAsync(userId, now.AddDays(7), revokedAtUtc: null);
        await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);

        await this.RunSweepUntilCountAsync(1);

        Assert.Equal([live], await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithActiveFamily_KeepsTheUnrevokedTail()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;
        var familyId = Guid.CreateVersion7();

        // A long-lived session: ancestors revoked well outside the reuse window, and a live tail.
        // Deleting the tail would orphan the session that family-wide revocation has to reach.
        await this.SeedTokenAsync(userId, now.AddDays(-20), revokedAtUtc: now.AddDays(-27), familyId: familyId);
        await this.SeedTokenAsync(userId, now.AddDays(-13), revokedAtUtc: now.AddDays(-20), familyId: familyId);
        var tail = await this.SeedTokenAsync(userId, now.AddDays(7), revokedAtUtc: null, familyId: familyId);

        await this.RunSweepUntilCountAsync(1);

        Assert.Equal([tail], await this.GetTokenIdsAsync());
    }

    [Fact]
    public async Task Sweep_WithMoreRowsThanOneBatch_DeletesAcrossBatches()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        for (var index = 0; index < 7; index++)
        {
            await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);
        }

        // Batch of 2 with a budget of 20 leaves room for all four batches the 7 rows need.
        await this.RunSweepUntilCountAsync(
            0,
            overrides: new Dictionary<string, string?>
            {
                ["RefreshTokenCleanup:BatchSize"] = "2",
            });
    }

    [Fact]
    public async Task Sweep_WithBatchBudgetExhausted_LeavesRemainderForTheNextSweep()
    {
        var userId = await this.ResetAndSeedUserAsync();
        var now = DateTime.UtcNow;

        for (var index = 0; index < 10; index++)
        {
            await this.SeedTokenAsync(userId, now.AddDays(-3), revokedAtUtc: null);
        }

        // 2 rows per batch, 3 batches per run: one sweep may delete at most 6, leaving 4.
        await this.RunSweepUntilCountAsync(
            4,
            overrides: new Dictionary<string, string?>
            {
                ["RefreshTokenCleanup:BatchSize"] = "2",
                ["RefreshTokenCleanup:MaxBatchesPerRun"] = "3",
            });

        // Still 4 after the sweep has settled: the budget capped the run rather than the predicate
        // running out of matching rows.
        await Task.Delay(200);
        Assert.Equal(4, await this.CountTokensAsync());
    }

    // Starts a host wired exactly as Cinedex.SchedulerWorker wires it, then waits for the startup
    // sweep to leave the table at the expected size. Polling rather than a fixed delay: the sweep is
    // in flight by the time StartAsync returns.
    private async Task RunSweepUntilCountAsync(
        int expectedCount,
        Dictionary<string, string?>? overrides = null)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
            ["RefreshTokenCleanup:Interval"] = SingleSweepInterval,
            ["RefreshTokenCleanup:ExpiredRetention"] = "1.00:00:00",
            ["RefreshTokenCleanup:ReuseDetectionWindow"] = "14.00:00:00",
            ["RefreshTokenCleanup:BatchSize"] = "500",
            ["RefreshTokenCleanup:MaxBatchesPerRun"] = "20",
        };

        foreach (var (key, value) in overrides ?? [])
        {
            configuration[key] = value;
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddAuthenticationPersistence();
        builder.Services.AddRefreshTokenCleanup();

        using var host = builder.Build();
        await host.StartAsync(CancellationToken.None);

        try
        {
            var deadline = DateTime.UtcNow.Add(PollTimeout);
            var actual = await this.CountTokensAsync();

            while (actual != expectedCount && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                actual = await this.CountTokensAsync();
            }

            Assert.Equal(expectedCount, actual);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }

    private async Task<Guid> ResetAndSeedUserAsync()
    {
        var userId = Guid.CreateVersion7();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        // Each test owns the whole table; xUnit runs methods within a class sequentially.
        await using (var truncate = new NpgsqlCommand(
            """TRUNCATE TABLE auth."refreshTokens";""",
            connection))
        {
            await truncate.ExecuteNonQueryAsync();
        }

        // refreshTokens.userId is a cascading FK, so a token needs a user to hang off. Only the
        // boolean and counter columns are NOT NULL, so the rest can be left unset.
        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO auth."AspNetUsers"
                (id, "emailConfirmed", "phoneNumberConfirmed", "twoFactorEnabled", "lockoutEnabled", "accessFailedCount")
            VALUES (@id, false, false, false, false, 0);
            """,
            connection);
        insert.Parameters.AddWithValue("id", userId);
        await insert.ExecuteNonQueryAsync();

        return userId;
    }

    private async Task<Guid> SeedTokenAsync(
        Guid userId,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        Guid? familyId = null)
    {
        var id = Guid.CreateVersion7();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO auth."refreshTokens"
                (id, "userId", "familyId", "tokenHash", "expiresAtUtc", "createdAtUtc", "revokedAtUtc")
            VALUES (@id, @userId, @familyId, @tokenHash, @expiresAtUtc, @createdAtUtc, @revokedAtUtc);
            """,
            connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("familyId", familyId ?? Guid.CreateVersion7());
        command.Parameters.AddWithValue("tokenHash", Convert.ToHexString(id.ToByteArray()));
        command.Parameters.AddWithValue("expiresAtUtc", expiresAtUtc);
        command.Parameters.AddWithValue("createdAtUtc", expiresAtUtc.AddDays(-7));
        command.Parameters.AddWithValue("revokedAtUtc", (object?)revokedAtUtc ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return id;
    }

    private async Task<int> CountTokensAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """SELECT count(*) FROM auth."refreshTokens";""",
            connection);

        return (int)(long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<HashSet<Guid>> GetTokenIdsAsync()
    {
        var ids = new HashSet<Guid>();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """SELECT id FROM auth."refreshTokens";""",
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }
}
