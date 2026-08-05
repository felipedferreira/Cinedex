namespace Cinedex.AppHost;

/// <summary>
/// Every fixed string used to build the resource graph in <see cref="AppHostBuilderExtensions"/> —
/// configuration keys, resource/endpoint names, and the Mailpit dev credentials. Kept in one place so
/// a key never drifts between where it's read (here) and where it's set (<c>appsettings.json</c>,
/// <c>appsettings.Development.json.example</c>, User Secrets).
/// </summary>
internal static class AppHostConstants
{
    /// <summary>
    /// All three hosts read this one fixed configuration key, so the connection string is injected by
    /// name. <c>WithReference</c> would name the variable after the resource instead.
    /// </summary>
    public const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";

    /// <summary>
    /// The two console hosts read <c>DOTNET_ENVIRONMENT</c>; only the web service reads
    /// <c>ASPNETCORE_ENVIRONMENT</c>.
    /// </summary>
    public const string GenericHostEnvironmentVariable = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// Set explicitly on both console hosts. Passing <c>launchProfileName: null</c> means nothing
    /// supplies an environment, and both would otherwise default to Production — skipping the
    /// <c>*.Development.json</c> files that turn on SQL command logging. Compose sets Production
    /// deliberately because it is the prod-like path; this AppHost is the dev loop.
    /// </summary>
    public const string DevelopmentEnvironment = "Development";

    /// <summary>
    /// Configuration key backing the <c>postgres-password</c> parameter. Aspire derives it from the
    /// parameter name, so the two must be kept in step.
    /// </summary>
    public const string PostgresPasswordKey = "Parameters:postgres-password";

    /// <summary>
    /// Configuration key backing the <c>postgres-username</c> parameter. Read manually, same as
    /// <see cref="PostgresPasswordKey"/> — the plain-string overload of <c>AddParameter</c> does not
    /// consult configuration on its own; only the overloads that take a <c>ParameterDefault</c> or no
    /// value at all do that.
    /// </summary>
    public const string PostgresUsernameKey = "Parameters:postgres-username";

    /// <summary>
    /// Default for <see cref="PostgresUsernameKey"/> when it is not set, matching the Postgres role
    /// <c>compose.yaml</c> creates (<c>POSTGRES_USER: movies_rw</c>) so the two dev paths agree on a
    /// role name by default. Not a secret — unlike the password, a developer can still override it
    /// (e.g. to reuse a differently-named role from an existing volume) via
    /// <c>dotnet user-secrets set "Parameters:postgres-username" "&lt;name&gt;"</c>.
    /// </summary>
    public const string PostgresUserName = "movies_rw";

    /// <summary>
    /// Whether the AppHost runs the Mailpit container. Committed as <c>true</c> in
    /// <c>appsettings.json</c>; a developer who doesn't need to see outgoing email can turn it off for
    /// themselves — see <see cref="AppHost"/>'s remarks for how. Nested under <c>Features</c> to match
    /// <see cref="MigrationsEnabledKey"/>.
    /// </summary>
    public const string MailpitEnabledKey = "Features:EnableMailpitSvc";

    /// <summary>Name of Mailpit's SMTP endpoint, shared between the resource that creates it and the
    /// web service that looks it up.</summary>
    public const string SmtpEndpointName = "smtp";

    /// <summary>
    /// Whether the AppHost runs <c>Cinedex.DatabaseMigrator</c> before starting anything that talks to
    /// the database. Committed as <c>true</c> in <c>appsettings.json</c>; a developer who is not
    /// changing the schema can turn it off for themselves — see <see cref="AppHost"/>'s remarks for
    /// how. Nested under <c>Features</c> to match the web service's
    /// <c>Features:ApiDocumentationEnabled</c>.
    /// </summary>
    public const string MigrationsEnabledKey = "Features:EnableDatabaseMigrationsSvc";

    /// <summary>
    /// Whether the AppHost runs the SPA's Vite dev server. Committed as <c>true</c> in
    /// <c>appsettings.json</c>; a developer working only on the backend — or without Node on
    /// <c>PATH</c> — can turn it off for themselves, see <see cref="AppHost"/>'s remarks for how.
    /// Nested under <c>Features</c> to match <see cref="MailpitEnabledKey"/>.
    /// </summary>
    public const string FrontendUiEnabledKey = "Features:EnableFrontendUiSvc";

    /// <summary>
    /// The SPA's directory (the one holding its <c>package.json</c>), relative to this project's
    /// folder: three levels up is the repository root. Deliberately outside <c>backend/</c> and
    /// outside <c>Cinedex.slnx</c> — Aspire launches it as an external process, it is not a
    /// <c>ProjectReference</c>.
    /// <para>
    /// This is the app package inside the <c>frontend/</c> npm workspace, not the workspace root.
    /// That matters: the package's <c>dev</c> script is plain <c>vite</c>, so the <c>--port</c>
    /// <c>AddViteApp</c> appends reaches it, whereas the root script delegates through
    /// <c>-w cinadex-ui</c> and would swallow it. Installing from here is still correct — npm walks
    /// up to the workspace root, so the single hoisted lockfile and the <c>@cinedex/ui</c> link are
    /// what a fresh clone gets.
    /// </para>
    /// </summary>
    public const string FrontendAppDirectory = "../../../frontend/apps/cinadex-ui";

    /// <summary>
    /// Fixed host port for the dev server, matching the port <c>vite.config.ts</c> defaults to and
    /// the one <c>compose.yaml</c> publishes the SPA on. Fixed rather than dynamic so the URL is the
    /// same in every path, and so it keeps matching the web service's <c>Frontend:BaseUrl</c>, which
    /// is what password-reset links are built from.
    /// </summary>
    public const int FrontendPort = 9000;

    /// <summary>
    /// Read by <c>vite.config.ts</c> to target its <c>/movies-svc</c> dev proxy. Its default there is
    /// the bare <c>dotnet run</c> address, which is not where the AppHost puts the web service.
    /// </summary>
    public const string ViteApiProxyTargetVariable = "VITE_API_PROXY_TARGET";

    /// <summary>
    /// The variable Aspire exports the dev server's allocated port to, which <c>vite.config.ts</c>
    /// reads — Vite does not pick <c>PORT</c> up on its own. <c>AddViteApp</c> separately appends the
    /// same port to the npm command as <c>--port</c>.
    /// </summary>
    public const string FrontendPortVariable = "PORT";

    /// <summary>
    /// Scalar API docs, relative to the web service endpoint. Carries the <c>/movies-svc</c> base that
    /// <c>UsePathBase</c> applies, and is served only because the AppHost forces
    /// <c>Features__ApiDocumentationEnabled</c> on.
    /// </summary>
    public const string ApiDocsPath = "/movies-svc/api-docs/v1";

    /// <summary>Label shown for <see cref="ApiDocsPath"/> in the dashboard's URLs column.</summary>
    public const string ApiDocsDisplayText = "api-docs/v1";

    /// <summary>
    /// Not a secret: Mailpit runs with MP_SMTP_AUTH_ACCEPT_ANY and accepts any credentials. These
    /// exist only to satisfy the web service's SMTP options validation, which requires both.
    /// </summary>
    public const string MailpitUser = "cinedex";

    /// <inheritdoc cref="MailpitUser"/>
    public const string MailpitPassword = "cinedex";
}
