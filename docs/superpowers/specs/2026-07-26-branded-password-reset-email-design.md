# Branded HTML Password-Reset Email — Design

**Date:** 2026-07-26
**Branch:** `smtp-enhancement`
**Status:** Approved

## Goal

The password-reset email should look like a product, not a debug message. Replace the current
single-sentence body with a branded HTML email that matches the crimson palette of the Cinedex API
landing page, carries an embedded logo, and states the (newly shortened) one-hour link expiry.

The shared layout is extracted so a second transactional email costs copy rather than markup, but no
machinery is built beyond what this one email uses.

## Current state

`ForgotPasswordHandler.BuildResetEmail` composes the message inline:

```csharp
Body = new HtmlEmailBody(
    $"<p>We received a request to reset your password. " +
    $"<a href=\"{resetLink}\">Reset it here</a>.</p>",
    PlainTextFallback: $"Reset your password: {resetLink}"),
```

The transport is already capable of everything this design needs. `SmtpEmailSender.BuildMimeMessage`
populates a MailKit `BodyBuilder` with both `HtmlBody` and `TextBody`, which already yields a proper
`multipart/alternative` message. **No SMTP or delivery-queue behaviour changes.** The composition
comment at `ForgotPasswordHandler.cs:36` states the governing rule: composition is an application
concern and the adapter stays a dumb pipe.

Constraints discovered while scoping:

- **There is no Cinedex logo raster in the repo.** The landing page's brand mark is the `◆` glyph
  styled with CSS. The only images tracked are favicons, Vite/React starter assets, and coverage
  artifacts. SVG does not render in most mail clients, so `public/favicon.svg` cannot be reused.
- No image tooling is installed (`convert` on this machine is the Windows filesystem utility, not
  ImageMagick; `python` is the Microsoft Store alias stub).

## Approach

Inline images become a property of the HTML body, because an inline image is meaningless without
one:

```csharp
public sealed record InlineImage(string ContentId, string MediaType, ReadOnlyMemory<byte> Content);

public sealed record HtmlEmailBody(
    string Content,
    string? PlainTextFallback = null,
    IReadOnlyList<InlineImage>? InlineImages = null) : EmailBody;
```

`InlineImage` must be `public`, not `internal`: `HtmlEmailBody` is public and would otherwise expose
an inaccessible type in its public API. The new parameter is appended last with a default so the
existing `new HtmlEmailBody(content, PlainTextFallback: ...)` call site compiles unchanged; `Render`
normalises `null` to an empty list.

The Application layer owns the asset bytes and hands them to the adapter, which maps them to
`BodyBuilder.LinkedResources`. The adapter learns nothing about Cinedex.

Alternatives considered and rejected:

- **`EmailMessage.Attachments`** — more general, and would cover real file attachments later. But
  nothing needs attachments today, and it permits a nonsensical state the chosen shape forbids: a
  `PlainTextEmailBody` carrying inline images.
- **Adapter owns the logo** — the Application emits `cid:cinedex-logo` and `SmtpEmailSender` always
  attaches its own copy. Smallest Application diff, but it couples the layers through a magic string
  and puts branding inside the transport, contradicting the dumb-pipe rule.
- **Templating engine (Razor/Scriban)** — designer-friendly `.cshtml` files and a render model, but a
  new dependency and rendering infrastructure for a single email.
- **Text-only `◆` mark, no image** — needs no asset and no model change at all, and survives image
  blocking. Rejected in favour of a real logo for portfolio polish; the trade-off is accepted
  knowingly (see Risks).

## Changes

### Application — `Cinedex.Application/Email/`

| File | Change |
|---|---|
| `InlineImage.cs` | New record: `ContentId`, `MediaType`, `Content` |
| `HtmlEmailBody.cs` | Add a trailing `IReadOnlyList<InlineImage>? InlineImages = null` so existing call sites and `PlainTextEmailBody` are untouched |
| `Assets/cinedex-logo.png` | New embedded resource (`<EmbeddedResource>` in the csproj) |
| `EmailAssets.cs` | Loads the embedded logo once through a cached `Lazy<ReadOnlyMemory<byte>>`, throwing a named exception if the resource is absent |
| `CinedexEmailLayout.cs` | The shared shell |

```csharp
internal sealed record EmailLayoutContent(
    string Heading, string IntroHtml, string ButtonLabel,
    string ButtonUrl, string? FootnoteHtml, string PlainTextBody);

internal static class CinedexEmailLayout
{
    public static HtmlEmailBody Render(EmailLayoutContent content);
}
```

`Render` returns a finished `HtmlEmailBody` with the logo already present in `InlineImages`, so
callers never touch `InlineImage` directly.

### Application — `ForgotPasswordHandler`

`BuildResetEmail` keeps building `resetLink`, then fills an `EmailLayoutContent` and calls
`CinedexEmailLayout.Render`. No HTML remains in the handler. `HandleAsync` is unchanged.

### Adapter — `SmtpEmailSender.BuildMimeMessage`

Inside the existing `case HtmlEmailBody`, after setting `HtmlBody`/`TextBody`, add each inline image
to `bodyBuilder.LinkedResources` and assign its `ContentId`. This is the only adapter change.

The resulting MIME structure is:

```
multipart/alternative
├── text/plain
└── multipart/related
    ├── text/html
    └── image/png  (Content-ID: cinedex-logo)
```

### Asset generation

The `◆` mark and "Cinedex" wordmark are rendered as an SVG, rasterised to PNG through the browser
canvas (`canvas.toDataURL`), and the base64 decoded to a file with PowerShell — no new dependency.
Committed at 320×64 px and displayed at 160×32 via explicit `width`/`height` attributes, so it stays
sharp on HiDPI screens. Replacing the file later requires no code change as long as that 5:1 aspect
ratio holds.

## Email design

### Chosen layout — "Marquee"

Selected from three rendered candidates on 2026-07-26. Structure, top to bottom, in a 560 px
centred table:

1. **Header band** — `--field-hi` background, the logo centred, `8px 8px 0 0` corner radius.
2. **Crimson rule** — a 3 px `--crimson` bar separating band from body.
3. **Body card** — `--field-mid` background: 26 px heading "Reset your password", one explanatory
   paragraph in `--ink-muted`, then the CTA.
4. **CTA** — table cell filled `--accent` with dark `--field-lo` label text, 6 px radius.
5. **Expiry line** — "This link expires in 1 hour." in `--accent`, directly under the button.
6. **Fallback block** — hairline rule, then the raw URL in monospace at 11 px.
7. **Footer** — centred, `#8f6b67`: "Didn't request this? Ignore this email — your password won't
   change."

Alternatives considered and rejected:

- **Ticket stub** — perforated cinema-ticket motif with `ADMIT ONE RESET` and a monospace serial.
  The most distinctive of the three and thematically apt for a movie catalogue, but rejected on two
  counts: the dashed perforation renders inconsistently under Outlook's Word engine, and a playful
  register is wrong for a security message someone reads while locked out of their account.
- **Minimal** — outlined button, no header band, single accent. Closest to how Stripe and GitHub
  send these and the most trustworthy of the three, but it surrenders the brand presence that
  motivated this work.

### Register

A password-reset email is a security email. Ornate branding, urgency framing, and a prominent styled
button are also the signature of phishing, so decoration is deliberately bounded: one accent colour,
one CTA, no countdown timers, no urgency language, and no imagery beyond the logo. Marquee is the
most branded of the three candidates that still holds that line.

### Palette

Taken from `Cinedex.WebService/src/style.css`:

| Token | Value | Use |
|---|---|---|
| `--field-lo` | `#23090b` | Page background |
| `--field-mid` | `#2f0e11` | Card background |
| `--field-hi` | `#4d181b` | Header band |
| `--ink` | `#f3ece6` | Body text |
| `--ink-muted` | `#c29d97` | Footnotes |
| `--accent` | `#e0776f` | Button fill, links |
| `--crimson` | `#c44b43` | Accent rule |
| _derived_ | `#8f6b67` | Footer text and fallback URL — a step below `--ink-muted` |
| _derived_ | `#3d1518` | Hairline rules, flattened from the source `--hairline` rgba |

Mail-client constraints the markup must respect:

- **Table-based layout, fully inline styles.** No flexbox, no grid, no `<style>` blocks relied upon.
- **Solid hex only** — the source `--hairline` and `--card` tokens are `rgba(...)`, which Outlook's
  Word rendering engine ignores. Flatten to hex equivalents.
- **`background-color` on `<td>`, not `<div>`** — Outlook drops backgrounds on block elements.
- **Table-based button.** Outlook ignores padding on `<a>`, so the CTA is a table cell with the
  anchor filling it.
- **System font stack.** Web fonts do not load; the landing page's `--sans`/`--mono` families fall
  back to `-apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif`.
- **`color-scheme: dark` declared.** The palette is already dark, so the email reads as dark-mode by
  default. Gmail and Outlook.com apply their own inversion to dark emails; the defences above keep
  that from mangling it, but exact rendering will vary by client and that is accepted.

## Copy

Heading "Reset your password", one explanatory sentence, the button, then:

- "This link expires in 1 hour." — formatted from `PasswordResetTokenPolicy` in
  `Cinedex.Application`, which is also what `AddAuthenticationAdapter` sets
  `DataProtectionTokenProviderOptions.TokenLifespan` from. The copy and the configured expiry are
  coupled by the compiler, not by convention: change the policy and both move together. The sentence
  is built once in `ForgotPasswordHandler` and passed to both the HTML footnote and the plain-text
  body, so those two cannot drift either.
- "If you didn't request this, you can ignore this email — your password won't change."
- The raw URL in small muted text, because some clients strip styled buttons.

The plain-text fallback mirrors all of the above.

### Injection surface

No user-supplied data is interpolated into the HTML — deliberately including no "Hi &lt;email&gt;"
greeting, which adds nothing given the reader is the recipient. The template interpolates only
configuration (`Frontend:BaseUrl`) and a token the service generated, so the body is injection-free by
construction rather than by careful escaping.

The `&` separating the `email` and `token` query parameters is written as `&amp;` in the `href`. The
current code emits it raw, which is malformed HTML; most clients tolerate it, but it is fixed here.

## Error handling

A missing or renamed embedded resource throws at compose time — and compose time is the request
thread. `ForgotPasswordHandler.BuildResetEmail(...)` is evaluated as the *argument* to
`IEmailDispatcher.Enqueue(...)`, so a throw from `EmailAssets.Logo()` propagates out of
`HandleAsync`, through `ForgotPasswordEndpoint`, into `DefaultExceptionHandler`, and the caller gets
**`500`**. The message is never enqueued, so `EmailDeliveryWorker` never sees it: this is not the
logged-and-swallowed failure mode that *delivery* failures have.

Loud is better than silent here, but the shape of the loudness matters. The unknown-email path
returns before composing anything (the `resetToken is null` branch) and still answers
`202 Accepted`. A missing asset would therefore make `POST /auth/password/forgot` answer `202` for
addresses with no account and `500` for real ones — an account-enumeration oracle on the one
endpoint whose whole design is to answer identically either way. `Lazy<T>` defaults to
`ExecutionAndPublication` and caches the thrown exception, so the failure would be stable for the
life of the process rather than intermittent.

Nothing here says the shipped code is fragile: the `EmbeddedResource` carries an explicit
`LogicalName`, and the unit test asserting the resource resolves from the assembly fails the build
if it stops matching. Those two are the guard. `EmailAssets` throws `InvalidOperationException`
naming the expected resource path so the cause is obvious in the logs if it ever does happen.

Validating the asset at startup — resolving it once during composition-root wiring, so a bad build
fails to start rather than serving a 500 per real account — would close the gap properly. It is
**not implemented on this branch**; it is recorded here as the mitigation to reach for if the asset
set grows or the guard weakens.

## Testing

| Level | Assertion |
|---|---|
| Unit | Rendered HTML contains the reset link |
| Unit | The `cid:` reference in the markup matches the attached `InlineImage.ContentId` |
| Unit | The `&` between query parameters is `&amp;`-encoded in the `href` |
| Unit | Plain-text fallback contains the raw URL |
| Unit | The embedded logo resource resolves from the assembly and is non-empty |
| Integration | Extend `SmtpEmailSenderTests` (Mailpit Testcontainer) to assert the inline image arrives over real SMTP |
| Manual | Eyeball the result in the Mailpit UI at `localhost:8025` |

Existing web-service endpoint tests use `CapturingEmailSender` and are unaffected.

## Out of scope

- **The `/reset-password` SPA page.** The reset link still lands on the Vite starter page; the SPA has
  no router. Tracked separately — this design is backend-only.
- Making the token lifespan configurable.
- Any second transactional email. The layout is extracted to make one cheap, not to add one now.

## Changelog

One entry under `## [Unreleased]`, category `### Changed` (it alters how a shipped feature behaves).
Root `CHANGELOG.md` only; `backend/CHANGELOG.md` is refreshed by the build.
