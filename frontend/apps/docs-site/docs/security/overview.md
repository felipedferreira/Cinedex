---
sidebar_position: 1
---

# Overview

How authentication works in Cinedex: JWT access tokens, rotating refresh tokens, and where ASP.NET
Core Identity is allowed to live.

:::info Adapted from the repository's design docs
This section is a curated adaptation of
[`docs/auth-security-model.md`](https://github.com/felipedferreira/Cinedex/blob/main/docs/auth-security-model.md)
and the auth design specs under `docs/superpowers/specs/`. It is **not** regenerated from them, so
if these pages ever disagree with the code, the code and those source docs win.
:::

## Layering

Identity is a framework detail, so it's confined to a single adapter behind application-layer
ports. The domain and application layers never reference ASP.NET Core Identity.

| Layer        | Type                                                                    | Responsibility                                                                                                                                                                                                     |
| ------------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Domain       | `UserAggregate/User`                                                    | Framework-free user aggregate. No password hashes, no tokens.                                                                                                                                                      |
| Application  | `IIdentityService`, `ITokenService`, `IEmailSender`, `IEmailDispatcher` | Ports the use cases depend on. `IEmailSender` delivers; `IEmailDispatcher` only queues, and is what request-path code uses.                                                                                        |
| Application  | `Auth/{Register,Login,Logout,Refresh,ForgotPassword,ResetPassword}`     | One handler slice per use case, each with a FluentValidation validator.                                                                                                                                            |
| Adapter      | `Cinedex.Auth.Identity`                                                 | Implements `IIdentityService` and `ITokenService`: ASP.NET Core Identity for accounts, JWT for tokens, EF Core for hashed refresh-token storage. `ApplicationUser : IdentityUser<Guid>` maps to the domain `User`. |
| Adapter      | `Cinedex.Email.Smtp`                                                    | Implements `IEmailSender` with MailKit, plus `IEmailDispatcher` and the background worker that drains the queue. Email delivery is a messaging concern, not authentication, so it lives in its own adapter.        |
| Presentation | `AuthenticationExtensions`                                              | JWT bearer validation, authorization middleware.                                                                                                                                                                   |
| Presentation | `RefreshTokenCookie`                                                    | Reads, sets, and clears the HttpOnly refresh-token cookie. Keeps the cookie a transport detail the Application layer never sees.                                                                                   |

See [Features → Architecture](../features/architecture.md) for how this fits into the rest of the
solution.

## Endpoints

All routes are relative to the `/movies-svc` base path.

| Method & route                          | Auth       | Behavior                                                                 |
| --------------------------------------- | ---------- | ------------------------------------------------------------------------ |
| `POST /movies-svc/auth/register`        | Anonymous  | Create a user. `201 Created`.                                            |
| `POST /movies-svc/auth/login`           | Anonymous  | Validate credentials; returns an access token + refresh token.           |
| `POST /movies-svc/auth/refresh`         | Anonymous  | Exchange a refresh token for a rotated pair.                             |
| `POST /movies-svc/auth/logout`          | **Bearer** | End the caller's session named by the cookie. `204 No Content`.          |
| `DELETE /movies-svc/auth/sessions/all`  | **Bearer** | End **every** session the caller has, on every device. `204 No Content`. |
| `POST /movies-svc/auth/password/forgot` | Anonymous  | Always `202 Accepted`.                                                   |
| `POST /movies-svc/auth/password/reset`  | Anonymous  | Reset the password with a valid reset token. `204 No Content`.           |

### Ending sessions: two endpoints, two units

`logout` and `sessions/all` differ in what they revoke, and the difference is deliberate.

- **`POST /auth/logout` ends one session.** The unit is the _family_ the presented cookie belongs to,
  and ownership is a condition of the statement that revokes it. Signing out on your laptop must not
  sign you out on your phone, so the user's other families are untouched.
- **`DELETE /auth/sessions/all` ends every session.** The unit is the _user_. It presents no token
  and matches on nothing but the subject of the validated access token, so it crosses every family
  the account owns — including sessions on devices the caller no longer has. This is the control for
  "I think my account is compromised".

Because it takes no request input at all, there is no value an attacker could supply to widen its
scope past themselves. The cost of that design is that it cannot be narrowed either: there's no
"end this _other_ session" endpoint, which would require exposing session identifiers to the client.

The catalog is members-only: **every Genre and Title endpoint requires a bearer token**, reads and
writes alike. Those endpoints simply omit `AllowAnonymous()` — FastEndpoints requires an
authenticated user by default — so the anonymous surface is exactly the five auth endpoints above
(and the two health endpoints). Anonymous catalog requests receive `401 Unauthorized`.

Continue to [Token lifecycle](./token-lifecycle.md) for how the tokens above actually behave.
