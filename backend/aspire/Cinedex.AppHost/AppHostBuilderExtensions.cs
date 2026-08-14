using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.Configuration;

namespace Cinedex.AppHost;

/// <summary>
/// Builds each resource in <see cref="AppHost.Main"/>'s resource graph. Split out of that method so
/// it stays a short list of "add this resource, wire it to that one" statements instead of the
/// container/project configuration for five different resources in a single method body.
/// </summary>
/// <remarks>
/// Per-developer configuration (User Secrets, <c>appsettings.Development.json</c>, the required
/// Postgres password, and what each <c>Features:*</c> flag does) is documented on
/// <see cref="AppHost"/>, not here — that's the type a developer actually opens first. The fixed
/// strings each method reads or sets live in <see cref="AppHostConstants"/>.
/// </remarks>
internal static class AppHostBuilderExtensions
{
    /// <summary>Adds the Postgres server and the <c>movies</c> database on it.</summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <returns>The <c>movies</c> database resource, referenced by every consumer of the connection string.</returns>
    /// <exception cref="InvalidOperationException">The Postgres password is not configured.</exception>
    public static IResourceBuilder<PostgresDatabaseResource> AddCinedexPostgres(
        this IDistributedApplicationBuilder builder)
    {
        // Supplied by the developer, never committed and never generated. Aspire resolves this from
        // Parameters:postgres-password, which is the same key its own generated password used, so an
        // existing secrets file keeps working.
        //
        // Checked up front because the failure is otherwise silent: Aspire leaves the unresolved
        // parameter to the dashboard, starts the containers that do not depend on it (Mailpit comes
        // up, Postgres does not) and writes nothing to the console explaining why.
        if (string.IsNullOrWhiteSpace(builder.Configuration[AppHostConstants.PostgresPasswordKey]))
        {
            throw new InvalidOperationException(
                $"'{AppHostConstants.PostgresPasswordKey}' is not configured, so Postgres cannot " +
                "start. Set it from backend/aspire/Cinedex.AppHost with: " +
                $"dotnet user-secrets set \"{AppHostConstants.PostgresPasswordKey}\" \"<password>\". " +
                "If you are reusing an existing cinedex-aspire-pgdata volume, it must be the " +
                "password that volume was initialized with; otherwise remove the volume first.");
        }

        var postgresPassword = builder.AddParameter("postgres-password", secret: true);

        var postgresUsernameValue =
            builder.Configuration[AppHostConstants.PostgresUsernameKey] ?? AppHostConstants.PostgresUserName;
        var postgresUsername = builder.AddParameter("postgres-username", postgresUsernameValue);

        // Host port 5432 matches compose.yaml, which means the two stacks cannot run at the same time
        // — whichever starts second fails to bind. The volume name still differs from compose's
        // cinedex_postgres_data on purpose: sharing a port is a startup error you see immediately,
        // whereas sharing a data directory would silently mix two databases.
        //
        // Note the username and password are only read when the volume is first initialized. Changing
        // either later does not rename or re-key an existing database; you have to
        // `docker volume rm cinedex-aspire-pgdata` first.
        var postgres = builder.AddPostgres("postgres", userName: postgresUsername, password: postgresPassword)
            .WithImageTag("17-alpine")
            .WithDataVolume("cinedex-aspire-pgdata")
            .WithHostPort(5432);

        return postgres.AddDatabase("movies");
    }

    /// <summary>Adds the Mailpit container, unless <see cref="AppHostConstants.MailpitEnabledKey"/> disables it.</summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <returns>The Mailpit resource, or <see langword="null"/> when the feature flag is off.</returns>
    public static IResourceBuilder<ContainerResource>? AddCinedexMailpit(this IDistributedApplicationBuilder builder)
    {
        // Defaults to true when the key is absent, same reasoning as MigrationsEnabledKey: a config
        // file that predates the flag should still start Mailpit rather than silently drop it.
        var mailpitEnabled = builder.Configuration.GetValue(AppHostConstants.MailpitEnabledKey, defaultValue: true);

        if (!mailpitEnabled)
        {
            return null;
        }

        // Same image pin compose.yaml and SmtpEmailSenderTests use. Unlike compose this does not set up
        // an MP_SMTP_AUTH_FILE: MP_SMTP_AUTH_ACCEPT_ANY takes any credentials, which keeps the resource
        // declarative rather than needing a shell entrypoint.
        return builder.AddContainer("mailpit", "axllent/mailpit", "v1.30.0")
            .WithEnvironment("MP_MAX_MESSAGES", "500")
            .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "true")
            .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
            .WithEndpoint(targetPort: 1025, name: AppHostConstants.SmtpEndpointName)
            .WithHttpEndpoint(targetPort: 8025, name: "http");
    }

    /// <summary>
    /// Adds <c>Cinedex.DatabaseMigrator</c>, unless <see cref="AppHostConstants.MigrationsEnabledKey"/> disables it.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <param name="moviesDb">The database the migrator applies both contexts' migrations against.</param>
    /// <returns>The migrator resource, or <see langword="null"/> when the feature flag is off.</returns>
    public static IResourceBuilder<ProjectResource>? AddCinedexMigrator(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> moviesDb)
    {
        // Defaults to true when the key is absent, so a config file that predates the flag still
        // migrates rather than silently starting the services against an unmigrated schema.
        var migrationsEnabled =
            builder.Configuration.GetValue(AppHostConstants.MigrationsEnabledKey, defaultValue: true);

        if (!migrationsEnabled)
        {
            return null;
        }

        // Applies migrations for both DbContexts and exits: DatabaseMigrationHostedService calls
        // StopApplication() in a finally. That makes WaitForCompletion (see WaitForDatabaseAvailability)
        // the exact analogue of compose's `condition: service_completed_successfully`, and it is what
        // removes the "migrations are never applied automatically" step from the Aspire path.
        return builder.AddProject<Projects.Cinedex_DatabaseMigrator>("migrator")
            .WithEnvironment(AppHostConstants.GenericHostEnvironmentVariable, AppHostConstants.DevelopmentEnvironment)
            .WithEnvironment(AppHostConstants.ConnectionStringVariable, moviesDb)
            .WaitFor(moviesDb);
    }

    /// <summary>Adds the web service, wired to Postgres and (if enabled) Mailpit.</summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <param name="moviesDb">The database the web service connects to.</param>
    /// <param name="mailpit">The Mailpit resource from <see cref="AddCinedexMailpit"/>, or <see langword="null"/>.</param>
    /// <returns>The web service resource.</returns>
    public static IResourceBuilder<ProjectResource> AddCinedexWebService(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> moviesDb,
        IResourceBuilder<ContainerResource>? mailpit)
    {
        // The profile must be named explicitly: the first entry in the web service's
        // launchSettings.json is "docker-compose" (commandName DockerCompose), which Aspire cannot
        // launch. The dedicated "aspire" profile is http-only, which keeps UseHttpsRedirection from
        // answering the health probe with a 307 and avoids depending on the dev certificate.
        //
        // Do NOT replace this with launchProfileName: null and a synthesized WithHttpEndpoint. That
        // works under `dotnet run`, where DCP starts the child and injects the port, but Visual Studio
        // launches Aspire project resources itself and takes a web project's URLs from its launch
        // profile — with no profile Kestrel falls back to 5000/5001 while Aspire's proxy forwards to
        // the port it allocated, so every request hangs and the resource sits at Running (Unhealthy).
        //
        // Browser auth is unaffected by the lack of https: `Secure` cookies are accepted on
        // http://localhost, which is a trustworthy origin.
        //
        // Features__ApiDocumentationEnabled defaults to false in appsettings.json, so Scalar would 404
        // without it. The health path carries the /movies-svc base that UsePathBase applies.
        // WithEndpoint turns the proxy off, so the endpoint is the port the app itself listens on. By
        // default Aspire publishes the profile's port (9002) from a proxy and moves Kestrel to a
        // dynamic one, which only holds if whoever launches the process honours the port Aspire
        // assigns. Visual Studio launches project resources itself and takes a web project's URLs from
        // its launch profile, so Kestrel would bind 9002 too and collide with the proxy. Unproxied,
        // the topology is identical under `dotnet run`, Rider and Visual Studio.
        //
        // Smtp__Username/Password stay unconditional even with Mailpit disabled: SmtpOptions
        // .ValidateOnStart requires both non-empty regardless, and EmailDeliveryWorker already
        // swallows send failures (logged, not fatal — see its class remarks), so pointing them at a
        // Mailpit that doesn't exist is harmless. Host/Port are the opposite: only Mailpit's dynamic
        // container port is correct when it's running, so those two are set below, conditionally.
        // With Mailpit disabled they're left unset, which falls back to the web service's own
        // appsettings.Development.json ("localhost", 1025) — nothing listens there either, so delivery
        // still just fails quietly rather than the app failing to start.
        var webservice = builder.AddProject<Projects.Cinedex_WebService>("webservice", launchProfileName: "aspire")
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithEnvironment(AppHostConstants.ConnectionStringVariable, moviesDb)
            .WithEnvironment("Features__ApiDocumentationEnabled", "true")
            .WithEnvironment("Smtp__Username", AppHostConstants.MailpitUser)
            .WithEnvironment("Smtp__Password", AppHostConstants.MailpitPassword)
            .WithEnvironment("Smtp__FromAddress", "no-reply@cinedex.local")
            .WithEnvironment("Smtp__FromName", "Cinedex")
            .WithEnvironment("Smtp__SecureSocketOptions", "None")
            .WithHttpHealthCheck("/movies-svc/health/ready");

        EndpointReference? smtp = mailpit?.GetEndpoint(AppHostConstants.SmtpEndpointName);

        if (mailpit is not null && smtp is not null)
        {
            webservice
                .WithEnvironment("Smtp__Host", smtp.Property(EndpointProperty.Host))
                .WithEnvironment("Smtp__Port", smtp.Property(EndpointProperty.Port))
                .WaitFor(mailpit);
        }

        // Point the dashboard link at the Scalar docs rather than the endpoint root. The root serves
        // nothing useful — everything is behind the /movies-svc path base — so the default link lands
        // on a 404. Applied as its own statement rather than inside the chain above because a
        // multi-line lambda mid-chain trips IDE0055, which is an error here.
        webservice.WithUrlForEndpoint("http", url =>
        {
            url.Url = AppHostConstants.ApiDocsPath;
            url.DisplayText = AppHostConstants.ApiDocsDisplayText;
        });

        return webservice;
    }

    /// <summary>
    /// Adds the dashboard-only <c>frontend</c> node the SPA and Storybook are nested under, unless
    /// both of them are disabled.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <returns>
    /// The grouping resource, or <see langword="null"/> when neither child would exist — a parent node
    /// with nothing under it is worse than no node at all.
    /// </returns>
    /// <remarks>
    /// <see cref="FrontendGroupResource"/> never runs, so nothing publishes a state for it and the
    /// dashboard would otherwise show a blank row. <c>WithInitialState</c> supplies one instead:
    /// <c>Running</c> because the node stands for whichever dev servers are underneath it, and an
    /// empty property list because a grouping node has nothing of its own to show.
    /// </remarks>
    public static IResourceBuilder<FrontendGroupResource>? AddCinedexFrontendGroup(
        this IDistributedApplicationBuilder builder)
    {
        if (!builder.IsFrontendUiEnabled() && !builder.IsStorybookEnabled())
        {
            return null;
        }

        return builder.AddResource(new FrontendGroupResource(AppHostConstants.FrontendGroupName))
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = AppHostConstants.FrontendGroupResourceType,
                State = KnownResourceStates.Running,
                Properties = [],
            });
    }

    /// <summary>
    /// Adds the SPA's Vite dev server, unless <see cref="AppHostConstants.FrontendUiEnabledKey"/>
    /// disables it.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <param name="webservice">The web service the dev server proxies <c>/movies-svc</c> to.</param>
    /// <param name="frontend">The grouping node to nest under, from <see cref="AddCinedexFrontendGroup"/>.</param>
    /// <returns>The dev server resource, or <see langword="null"/> when the feature flag is off.</returns>
    public static IResourceBuilder<ViteAppResource>? AddCinedexFrontendUi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> webservice,
        IResourceBuilder<FrontendGroupResource>? frontend)
    {
        if (!builder.IsFrontendUiEnabled())
        {
            return null;
        }

        // AddViteApp runs the package.json "start" script and installs dependencies first when
        // node_modules is missing, which is what
        // makes this work on a fresh clone. AddNpmApp is the Aspire 12 spelling and is obsolete —
        // warnings are errors here, so it would not compile.
        //
        // The endpoint is declared https because vite.config.ts serves TLS itself through
        // @vitejs/plugin-basic-ssl; Aspire is not terminating it and never sees the certificate. Port
        // and target port are pinned to the same fixed 9000 vite.config.ts defaults to, and the proxy
        // is off, for the reason it is off on the web service: Vite owns that socket. It binds with
        // strictPort, so a proxy in front of it would only add a second port to an address that has to
        // stay https://localhost:9000 anyway — that is the URL the web service builds password-reset
        // links from (Frontend:BaseUrl) and the one compose publishes the SPA on.
        //
        // env: PORT is how Aspire tells the dev server which port it allocated — belt and braces, since
        // AddViteApp also appends `--port` to the npm command from the same endpoint. Vite does not
        // read PORT on its own, so vite.config.ts reads it explicitly and falls back to 9000. Unproxied,
        // all three agree; the wiring still holds if the port ever moves.
        //
        // VITE_OPEN_BROWSER suppresses the tab Vite would otherwise open. The dashboard already links
        // to the SPA, so under this host the tab is a duplicate — and with Storybook alongside it, two
        // of them on every `dotnet run`. Set here rather than committed as `open: false` in
        // vite.config.ts so a bare `npm run start` still opens the browser the way it always has.
        var ui = builder.AddViteApp("ui", AppHostConstants.FrontendAppDirectory, "start")
            .WithHttpsEndpoint(
                port: AppHostConstants.FrontendPort,
                targetPort: AppHostConstants.FrontendPort,
                env: AppHostConstants.FrontendPortVariable,
                isProxied: false)
            .WithEnvironment(
                AppHostConstants.ViteApiProxyTargetVariable,
                webservice.GetEndpoint("http"))
            .WithEnvironment(
                AppHostConstants.ViteOpenBrowserVariable,
                AppHostConstants.OpenBrowserDisabledValue)
            .WaitFor(webservice);

        // Presentational only: the WaitFor above, not this, is what orders startup.
        if (frontend is not null)
        {
            ui.WithParentRelationship(frontend);
        }

        return ui;
    }

    /// <summary>
    /// Adds the component library's Storybook dev server, unless
    /// <see cref="AppHostConstants.StorybookEnabledKey"/> disables it.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <param name="frontend">The grouping node to nest under, from <see cref="AddCinedexFrontendGroup"/>.</param>
    /// <returns>The Storybook resource, or <see langword="null"/> when the feature flag is off.</returns>
    public static IResourceBuilder<ViteAppResource>? AddCinedexStorybook(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<FrontendGroupResource>? frontend)
    {
        if (!builder.IsStorybookEnabled())
        {
            return null;
        }

        // The run script has to be named explicitly: AddViteApp defaults to "dev", and this package
        // deliberately has no such script — `storybook` is what a developer runs by hand, and keeping
        // one entry point means the AppHost cannot drift from it.
        //
        // Storybook 10 builds on @storybook/react-vite, so this is a Vite app underneath and
        // AddViteApp's port handling applies: it appends `--port` to the npm command. The package's
        // own script already carries `-p 9001`, and Storybook's parser takes the last occurrence, so
        // Aspire's flag wins — pinning both to the same port just makes that a no-op rather than
        // something to reason about.
        //
        // Plain http, unlike the SPA: Storybook serves no TLS of its own, and there is nothing to
        // terminate for a workbench that calls no API. Unproxied and pinned for the same reason the
        // SPA is — the dashboard URL then matches the one the README and `npm run storybook` promise.
        //
        // No WaitFor and no reference to any other resource: Storybook renders the @cinedex/* component
        // libraries in isolation and talks to nothing, so it is the one resource that can start whenever
        // it likes.
        // That also means it stays useful when the backend half of the stack is switched off.
        //
        // --no-open suppresses the browser tab, the same call the SPA makes through VITE_OPEN_BROWSER
        // — but it has to be a CLI flag here, because Storybook opens the tab itself rather than
        // through Vite's server.open, so this package's vite.config.ts has no say in it. WithArgs
        // appends after the `--port` AddViteApp adds, which puts it past npm's `--` separator and in
        // storybook dev's own argv. Passed from here rather than baked into the package's `storybook`
        // script so a bare `npm run storybook` still opens the browser.
        var storybook = builder.AddViteApp(
                "storybook",
                AppHostConstants.StorybookAppDirectory,
                AppHostConstants.StorybookRunScript)
            .WithHttpEndpoint(
                port: AppHostConstants.StorybookPort,
                targetPort: AppHostConstants.StorybookPort,
                isProxied: false)
            .WithArgs(AppHostConstants.StorybookNoOpenArgument);

        // Presentational only, and specifically not a dependency: nesting Storybook under the frontend
        // node does not make it wait on anything, which is what keeps it usable with the rest of the
        // stack switched off.
        if (frontend is not null)
        {
            storybook.WithParentRelationship(frontend);
        }

        return storybook;
    }

    /// <summary>Adds the scheduler worker, wired to Postgres.</summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <param name="moviesDb">The database the scheduler worker connects to.</param>
    /// <returns>The scheduler worker resource.</returns>
    public static IResourceBuilder<ProjectResource> AddCinedexSchedulerWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> moviesDb) =>
        builder.AddProject<Projects.Cinedex_SchedulerWorker>("schedulerworker")
            .WithEnvironment(AppHostConstants.GenericHostEnvironmentVariable, AppHostConstants.DevelopmentEnvironment)
            .WithEnvironment(AppHostConstants.ConnectionStringVariable, moviesDb);

    /// <summary>
    /// Makes each of <paramref name="consumers"/> wait on Postgres being ready before starting —
    /// through the migrator's completion when one is in the graph, or directly on the database when
    /// migrations are disabled.
    /// </summary>
    /// <param name="consumers">The resources that need Postgres before they can start.</param>
    /// <param name="migrator">The migrator resource from <see cref="AddCinedexMigrator"/>, or <see langword="null"/>.</param>
    /// <param name="moviesDb">The database to wait on directly when there is no migrator.</param>
    /// <remarks>
    /// With the migrator in the graph, waiting for it to exit implies Postgres is already up — it
    /// waits on the database itself. Without it, consumers still have to wait for Postgres directly,
    /// or they would race a database that is not accepting connections yet.
    /// </remarks>
    public static void WaitForDatabaseAvailability(
        this IEnumerable<IResourceBuilder<ProjectResource>> consumers,
        IResourceBuilder<ProjectResource>? migrator,
        IResourceBuilder<PostgresDatabaseResource> moviesDb)
    {
        foreach (var consumer in consumers)
        {
            if (migrator is not null)
            {
                consumer.WaitForCompletion(migrator);
            }
            else
            {
                consumer.WaitFor(moviesDb);
            }
        }
    }

    /// <summary>
    /// Whether the SPA's Vite dev server is enabled. Defaults to <see langword="true"/> when the key
    /// is absent, same reasoning as the other flags. Unlike most of them this one also needs Node and
    /// npm on PATH; turning it off is the escape hatch for a machine that has neither, or for a
    /// backend-only session.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <returns><see langword="true"/> when the SPA should run.</returns>
    private static bool IsFrontendUiEnabled(this IDistributedApplicationBuilder builder) =>
        builder.Configuration.GetValue(AppHostConstants.FrontendUiEnabledKey, defaultValue: true);

    /// <summary>
    /// Whether Storybook is enabled. Defaults to <see langword="true"/> when the key is absent, and
    /// shares <see cref="IsFrontendUiEnabled"/>'s Node-and-npm-on-PATH requirement.
    /// </summary>
    /// <param name="builder">Distributed application builder.</param>
    /// <returns><see langword="true"/> when Storybook should run.</returns>
    /// <remarks>
    /// Both flags are read through helpers rather than inline so <see cref="AddCinedexFrontendGroup"/>
    /// — which has to know whether either child will exist before it adds their parent — cannot drift
    /// from the methods that actually add those resources.
    /// </remarks>
    private static bool IsStorybookEnabled(this IDistributedApplicationBuilder builder) =>
        builder.Configuration.GetValue(AppHostConstants.StorybookEnabledKey, defaultValue: true);
}
