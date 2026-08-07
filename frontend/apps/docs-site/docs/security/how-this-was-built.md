---
sidebar_position: 6
---

# How This Was Built

The auth and security work wasn't built ad hoc — it was planned as its own execution effort and
designed in written specs before implementation, the same way the rest of this repository's
significant changes are. This page summarizes that process; the raw planning documents live in the
repository under `docs/superpowers/` for anyone who wants the full detail.

> **A live issue list isn't here yet.** The planning below is tracked in a Linear project; once
> that connector is authorized for this docs build, this page will list the actual issues and their
> current status instead of only the plan's snapshot.

## Planning: waves and lanes

The authentication and authorization effort was scoped as 65 issues before any of them shipped,
organized two ways at once:

- **Waves** — the earliest point each issue could legally start, derived from the longest chain of
  prerequisites ending at it. Wave 0 is unblocked work; deeper waves depend on earlier ones. The
  dependency graph tops out at **7 waves**, with **21 issues (41 points) unblocked in Wave 0** —
  meaning the schedule was staffing-limited, not dependency-limited, from the start.
- **Lanes** — work organized by theme, so "who should do this" and "when can it start" are answered
  independently: Platform & Config, Credentials & Recovery, Sessions & Tokens, Authorization,
  Frontend Runtime, and Verification & Ops.

Before execution began, the dependency graph was validated end-to-end and three real problems were
found and fixed: two prerequisite edges pointed the wrong way (one of them a genuine cycle — two
issues each listed as a prerequisite of the other), and one dependency was based on a
misunderstanding of which token-generation path actually needed it. All three were corrected before
Wave 0 started, and the corrected plan was verified to be cycle-free with every one of the 65 issues
reachable in a valid execution order.

## Design decisions

Two pieces of this work were designed on paper before being built, with alternatives considered and
explicitly rejected — not because the first idea didn't work, but because a second look found a
better one.

### Making the catalog members-only

The decision to require a bearer token on every catalog endpoint could have been enforced several
ways. A global ASP.NET fallback policy was considered and rejected as redundant — the endpoint
framework already requires authentication by default unless an endpoint opts out, so the actual
change was smaller than it looked: removing the opt-out from ten endpoints, rather than adding a
new enforcement mechanism on top of one that already existed.

### The branded password-reset email

The reset email went through three visual directions before shipping: a distinctive
"cinema-ticket-stub" motif was rejected because a playful register is the wrong tone for a security
message someone reads while locked out of their account (and its dashed-perforation styling didn't
render reliably in Outlook); a stripped-down "Stripe/GitHub style" minimal layout was rejected for
surrendering too much brand presence. The shipped design sits between them — one accent colour, one
button, no urgency language — because heavy branding paired with urgency framing is also the visual
signature of phishing, and a security email is exactly the wrong place to look like one. See
[Password reset](./password-reset.md#the-email-itself) for how that plays out in the actual markup.

## Where to look next

- [Known gaps](./known-gaps.md) — the items this planning process identified but hasn't shipped yet.
- The full planning documents (`docs/superpowers/plans/` and `docs/superpowers/specs/` in the
  repository) for anyone who wants the unabridged version, including the task-by-task
  implementation checklists this page deliberately leaves out.
