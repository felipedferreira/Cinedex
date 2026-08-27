# Cinedex Scheduler Worker

The Cinedex background worker, running scheduled maintenance jobs such as the expired
refresh-token cleanup.

`Cinedex.SchedulerWorker` is a Generic Host process that runs jobs on a timer, separate from the
web service. The separation is deliberate: the web service has no business deleting rows on a
timer, and because the integration-test host runs real hosted services, registering the sweep
alongside the auth adapter would have it running underneath tests that assert exact refresh-token
row counts. Only this project calls `AddRefreshTokenCleanup()`.

## Jobs

### Refresh-token cleanup

Nothing else in the system ever deletes a refresh token—a rotation revokes its predecessor and
inserts a replacement—so without this worker the table and its indexes grow forever.

Two categories of dead row are deleted on two different schedules, because they are dead for
different reasons:

| Category | Setting | Default | Why |
|----------|---------|---------|-----|
| Expired, never revoked | `ExpiredRetention` | 1 day | **Not** a security boundary. The row is unreachable by every code path the moment it expires; the extra day is operational slack for clock skew and for keeping the row queryable. |
| Revoked | `ReuseDetectionWindow` | 14 days | **Is** a security boundary. The revoked row is the only evidence that makes a replayed, already-rotated token recognisable as reuse rather than as an unknown token. It must comfortably exceed `Jwt:RefreshTokenDays`. |

Deletion runs in bounded batches so no single statement holds row locks for long, and so a large
backlog drains over several sweeps instead of turning one sweep into a long-running job.

## Configuration

The required setting is:

```text
ConnectionStrings:DefaultConnection
```

Configuration is loaded in this order, with later sources overriding earlier ones:

1. `application.json`
2. `application.{DOTNET_ENVIRONMENT}.json`
3. .NET User Secrets when `DOTNET_ENVIRONMENT=Development`
4. Environment variables
5. Command-line arguments

The `RefreshTokenCleanup` section carries the job's knobs:

| Key | Default | Purpose |
|-----|---------|---------|
| `Interval` | `00:10:00` | Wait between sweeps |
| `ExpiredRetention` | `1.00:00:00` | How long an expired, never-revoked row is kept past expiry |
| `ReuseDetectionWindow` | `14.00:00:00` | How long a revoked row is kept past revocation |
| `BatchSize` | `500` | Rows deleted per statement |
| `MaxBatchesPerRun` | `20` | Batches per sweep |

All are validated with `ValidateOnStart`, so a bad value fails the process at startup rather than
at the first sweep. `ReuseDetectionWindow` must be at least as long as `ExpiredRetention`.

Environment variables use the standard .NET double-underscore syntax:

```text
ConnectionStrings__DefaultConnection
RefreshTokenCleanup__Interval
```

`application.json` contains only a placeholder connection string. Never commit a real database
password.

## Execution Model

`Program.Main` returns `0` on clean shutdown and `1` when the host terminates unexpectedly, logging
the reason itself—without that, a startup failure would exit `1` with the cause visible only in the
framework's own "Hosting failed to start" line, and would never reach Seq if the exporter had not
started yet. The startup log line names the environment, whether telemetry export is configured,
and every scheduled job registered.

## Related

- [Backend overview](../../../README.md)
- [Auth and security model](../../../../docs/auth-security-model.md)
- [Web service](../Cinedex.WebService/README.md)
