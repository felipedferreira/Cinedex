namespace Cinedex.AppHost;

/// <summary>
/// Aspire orchestration host for the Cinedex stack: Postgres and Mailpit as containers, the three
/// .NET hosts and the two frontend dev servers — the SPA's and Storybook's — as local processes.
/// </summary>
/// <remarks>
/// <para>
/// This is an alternative to <c>docker compose up</c>, not a replacement — <c>compose.yaml</c> stays
/// the prod-like path (built images, Nginx terminating HTTPS in front of the API, Seq, the SPA as a
/// static bundle). This host exists for the inner loop: no image rebuild per change, migrations are
/// applied for you, and the SPA runs from the Vite dev server with hot reload instead. Both publish
/// the SPA on <c>https://localhost:9000</c> and Postgres on 5432, so the two cannot run at once.
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
/// <para>
/// <c>Features:EnableFrontendUiSvc</c> (default <c>true</c>) controls whether the SPA's Vite dev
/// server runs, same override channels again. It is the one resource that needs Node and npm on
/// <c>PATH</c> — turn it off on a machine without them, or when the session is backend-only. With it
/// on, the SPA is served at <c>https://localhost:9000</c> (self-signed, so expect the browser
/// warning) and its <c>/movies-svc</c> proxy already points at this host's web service.
/// </para>
/// <para>
/// <c>Features:EnableStorybookSvc</c> (default <c>true</c>) controls whether the component library's
/// Storybook runs, same override channels again, and it needs Node and npm on <c>PATH</c> for the
/// same reason the SPA does. On, it serves at <c>http://localhost:9001</c> — plain http, since a
/// workbench that calls no API has nothing to terminate TLS for. It references no other resource and
/// waits on nothing, so it is equally useful with the rest of the stack switched off. Note this is
/// the dev server with hot reload; <c>compose.yaml</c> publishes the built static bundle on that same
/// port — the two never run at once anyway, since Aspire and compose already collide on Postgres and
/// the SPA.
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
        var frontend = builder.AddCinedexFrontendGroup();
        builder.AddCinedexFrontendUi(webservice, frontend);
        builder.AddCinedexStorybook(frontend);

        new[] { webservice, schedulerWorker }.WaitForDatabaseAvailability(migrator, moviesDb);

        await builder.Build().RunAsync();
    }
}
