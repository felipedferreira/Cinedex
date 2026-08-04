namespace Cinedex.AppHost;

/// <summary>
/// Aspire orchestration host for the Cinedex backend: Postgres and Mailpit as containers, the
/// three .NET hosts as local processes.
/// </summary>
/// <remarks>
/// <para>
/// This is an alternative to <c>docker compose up</c>, not a replacement — <c>compose.yaml</c> stays
/// the prod-like path (built images, Nginx/HTTPS proxy, Seq, the SPA). This host exists for the inner
/// loop: no image rebuild per change, and migrations are applied for you.
/// </para>
/// <para>
/// There is no <c>Cinedex.ServiceDefaults</c> project on purpose. Every host already calls
/// <c>AddObservability()</c> from <c>FoundryOceanus.Observability.OpenTelemetry</c>, which reads the standard
/// <c>OTEL_EXPORTER_OTLP_*</c> variables this AppHost injects — so logs and traces reach the Aspire
/// dashboard with no change to any service. Health endpoints already exist too. The only things
/// ServiceDefaults would add are service discovery and HTTP resilience defaults, neither of which
/// this codebase uses.
/// </para>
/// <para>
/// <b>Per-developer configuration.</b> Committed defaults live in <c>appsettings.json</c>. Either of
/// these overrides them without touching a tracked file — user secrets win over the local file,
/// because the host adds them after <c>appsettings.Development.json</c>. Both only apply when the
/// host runs in Development, which the launch profiles set; launching the built executable directly,
/// with no profile, falls back to Production and silently ignores both.
/// </para>
/// <list type="bullet">
/// <item><description>
/// User Secrets, stored outside the repo entirely. From this project's folder:
/// <c>dotnet user-secrets set "Features:EnableDatabaseMigrationsSvc" "false"</c>.
/// </description></item>
/// <item><description>
/// <b>Required:</b> the Postgres password. The AppHost will not start without it. From this
/// project's folder: <c>dotnet user-secrets set "Parameters:postgres-password" "&lt;your password&gt;"</c>.
/// Keep it in User Secrets rather than the local settings file — that file sits inside the repo, and
/// a git-ignored password is still a password on disk in a working tree.
/// </description></item>
/// <item><description>
/// <c>appsettings.Development.json</c>, git-ignored for this project only. Copy
/// <c>appsettings.Development.json.example</c> next to it and edit.
/// </description></item>
/// </list>
/// <para>
/// Turning migrations off starts the stack faster but assumes the schema is already current. Against
/// a fresh database — or after pulling a new migration — leave the flag on for one run, or apply both
/// contexts by hand with <c>dotnet ef database update</c>.
/// </para>
/// <para>
/// <c>Features:EnableMailpitSvc</c> (default <c>true</c>) controls whether the Mailpit container
/// runs, same override channels as above. Turning it off starts the stack faster and is fine day to
/// day — email delivery already tolerates failure (see <c>EmailDeliveryWorker</c>) — but there is then
/// nowhere to read a password-reset email, so turn it back on when you need one.
/// </para>
/// </remarks>
public sealed class AppHost
{
    /// <summary>Builds the resource graph and runs the distributed application.</summary>
    /// <param name="args">Command-line arguments forwarded to the Aspire host.</param>
    /// <returns>A task that completes when the application shuts down.</returns>
    /// <remarks>
    /// Each resource's own construction — container/project configuration, feature-flag checks — lives
    /// in the <c>AddCinedex*</c> extension methods in <see cref="AppHostBuilderExtensions"/>. This
    /// method is deliberately just the list of what gets added and how the pieces depend on each other.
    /// </remarks>
    public static async Task Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

        var moviesDb = builder.AddCinedexPostgres();
        var mailpit = builder.AddCinedexMailpit();
        var migrator = builder.AddCinedexMigrator(moviesDb);
        var webservice = builder.AddCinedexWebService(moviesDb, mailpit);
        var schedulerWorker = builder.AddCinedexSchedulerWorker(moviesDb);

        new[] { webservice, schedulerWorker }.WaitForDatabaseAvailability(migrator, moviesDb);

        await builder.Build().RunAsync();
    }
}
