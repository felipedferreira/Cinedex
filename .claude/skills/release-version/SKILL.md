---
name: release-version
description: >-
  Decide and apply the next semantic version for the Cinedex repo. Use this
  whenever the user wants to cut/prepare/tag a release, bump the version, roll
  the changelog, or asks "what should the next version be?" — including vaguer
  phrasings like "ship this", "finalize the changelog", "update the version
  numbers", or "we're ready to release". Reads the `## [Unreleased]` changelog
  section and recent git history, decides MAJOR/MINOR/PATCH from the rules in
  CONTRIBUTING.md, then updates root CHANGELOG.md, backend/Directory.Build.props,
  and the frontend package.json in one consistent step. Reach for this skill even
  if the user doesn't say "semver" or "release" explicitly but clearly wants to
  stamp a new version.
---

# Release a new version of Cinedex

Cinedex keeps its version in three places that must stay in lockstep, and its
changelog in [Keep a Changelog](https://keepachangelog.com) format. Getting a
release right means two separable things: **deciding** the correct next version
(judgment — that's your job, using the project's own rules), and **applying** it
(mechanical file surgery — delegated to `scripts/apply-release.mjs` so the
changelog is rewritten identically every time instead of by error-prone hand
edits).

Always propose the version, title, date, and the diff, then **wait for the
user's explicit approval before writing files.** Cutting a release touches three
files and is annoying to unwind, so the checkpoint matters.

## Workflow

Create a todo per step so nothing is skipped.

### 1. Read the current state

- **Current version** — `<Version>` in [`backend/Directory.Build.props`](../../../backend/Directory.Build.props). This is the source of truth for "where we are now".
- **The rules** — read the "Versioning" section of [`CONTRIBUTING.md`](../../../CONTRIBUTING.md) fresh each time. The scheme below reflects it, but if it has changed, CONTRIBUTING wins.
- **The changes** — the `## [Unreleased]` section of the root [`CHANGELOG.md`](../../../CHANGELOG.md). This curated section is the primary evidence for the bump decision.

If `## [Unreleased]` is empty, stop and tell the user there's nothing to release.

### 2. Corroborate with git

The changelog is human-maintained, so it can lag the code. Cross-check that
nothing user-facing is missing from `## [Unreleased]`:

```bash
git log --oneline -30
# if the repo has release tags, prefer the precise range:
git describe --tags --abbrev=0 2>/dev/null && \
  git log --oneline "$(git describe --tags --abbrev=0)"..HEAD
```

Scan for `feat:`/`fix:`/breaking commits that aren't reflected in the Unreleased
entries. If you find gaps, surface them to the user and offer to add the entries
**before** cutting the release — the changelog should be complete first. Don't
silently invent entries.

### 3. Decide the bump

Apply the project's scheme. Cinedex is **pre-1.0**, which is deliberately
adapted from strict semver — while MAJOR is `0`, the `1.0.0` slot is reserved
for the production-ready release rather than being spent on the first breaking
change.

**While current version is `0.y.z` (pre-1.0):**

| Unreleased contains… | Bump | Example |
|----------------------|------|---------|
| New features / significant capability (a populated `### Added`, or a substantial `### Changed`) | **MINOR** — `0.Y.0`, patch resets to 0 | `0.5.0 → 0.6.0` |
| Only bug fixes, tooling, docs, small tweaks (`### Fixed` / minor `### Changed`, no real features) | **PATCH** — `0.x.Y` | `0.5.0 → 0.5.1` |
| A breaking change | Still **MINOR** pre-1.0 (no compatibility promise yet), but **call the breaking change out explicitly** and ask whether this should instead be declared `1.0.0` | — |

**Once current version is `1.0.0` or higher (strict semver applies):**

- Breaking change to the public API/contract → **MAJOR** (`X.0.0`).
- New backward-compatible functionality → **MINOR** (`x.Y.0`).
- Backward-compatible bug fix only → **PATCH** (`x.y.Z`).

Remember to reset lower parts to zero when you bump a higher one (`0.5.3` with a
new feature → `0.6.0`, not `0.6.3`).

Never jump straight to `1.0.0` on your own judgment — that's a product decision.
If the changes look 1.0-worthy, recommend it and let the user make the call.

### 4. Compose the release heading

Releases use the format `## [x.y.z] - Month D YYYY Title` (e.g.
`## [0.6.0] - July 18 2026 Auth & SMTP`).

- **Date** — pass the date to the script in ISO form (`--date YYYY-MM-DD`); the
  script renders it long-form (full English month, day, full year) in the
  heading. The script defaults to today, so pass `--date` only to override.
- **Title** — a short, descriptive phrase capturing the release's theme, in the
  spirit of existing entries (e.g. "Entity Framework Core with PostgreSQL"). Draft
  it from the dominant thread of the Unreleased content and let the user edit it.

### 5. Propose, then wait

Show the user, and pause for approval:

- Current → proposed version, and the one-line reason (which rule fired).
- The proposed heading (`## [x.y.z] - Month D YYYY Title`).
- The concrete plan. Run the script in preview mode to render it:

```bash
node .claude/skills/release-version/scripts/apply-release.mjs \
  --version <X.Y.Z> --title "<title>" --date <YYYY-MM-DD> --dry-run
```

If the user adjusts the version, title, or date, re-run `--dry-run` with the new
values before proceeding.

### 6. Apply

On approval, run the same command **without** `--dry-run`. It updates, in one
shot:

1. `CHANGELOG.md` — moves the Unreleased body into the new dated section and
   leaves `## [Unreleased]` empty for the next cycle.
2. `backend/Directory.Build.props` — `<Version>`, `<FileVersion>`, and
   `<InformationalVersion>` together.
3. Every `frontend/**/package.json` that isn't under `node_modules`. The
   frontend is an npm workspace, so today that is eight files, all bumped in
   lockstep with the product version: the workspace root
   `frontend/package.json`; the three apps (`apps/cinedex-app`,
   `apps/storybook`, `apps/docs-site`); and the three component-library
   packages plus the design system (`packages/atoms`, `packages/compounds`,
   `packages/solution`, `packages/theme`).

Run it from the repo root (the default `--repo-root` is the current directory).

### 7. Follow-ups

Tell the user about the two things the script deliberately does **not** do,
because they're better handled by their own tooling:

- **`backend/CHANGELOG.md` is build-generated — never hand-edit it.** The backend
  build's `BuildFrontend` target copies the root changelog into it, and CI fails
  if the two differ. After releasing, run a backend build (`dotnet build` from
  `backend/`) and commit the refreshed `backend/CHANGELOG.md` diff alongside the
  release.
- **Sync the frontend lockfile:** `npm install --package-lock-only` in
  `frontend/` (the workspace root, where the single lockfile lives) so the
  recorded versions match the bumped `package.json` files.

Then confirm the change with `git diff --stat` and summarize what moved.

## Guardrails

- **Only** edit the root `CHANGELOG.md`, never `backend/CHANGELOG.md` (see above).
- **Only** the frontend workspace's own `package.json` files — the script
  already excludes `node_modules`; don't touch dependency manifests.
- The script refuses to write if the new version isn't strictly greater than the
  current one, or if `## [Unreleased]` is empty. Treat those errors as signals to
  re-check the decision, not to force past them.
- Don't commit, tag, or push as part of this skill unless the user asks — leave
  the release staged so they can review the diff first.

## The script

`scripts/apply-release.mjs` (Node 18+, no dependencies) does all file writes.
Flags: `--version` (required), `--title` (required), `--date` (ISO, defaults to
today), `--repo-root` (defaults to cwd), `--dry-run` (preview only). It builds
all new file contents before writing any, so a validation failure aborts without
leaving a half-applied release.
