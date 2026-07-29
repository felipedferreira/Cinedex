# Cinedex Authentication & Authorization Execution Plan

**Goal:** Ship authentication, role-based authorization, and related security hardening across 65 Linear issues, organized into 7 waves of work and 6 parallel lanes.

**Interactive Board:** Open the [Cinedex Auth Execution Board](https://claude.ai/code/artifact/90aa2d67-a58c-47f3-b97c-e14cdced5bc9) to explore the swimlane visualization. Click any card to trace its dependencies or unlock its dependents; click the Linear icon to jump to the issue.

**Current State:** Plan finalized and validated. All 65 issues mapped, dependencies corrected, waves derived from graph depth, lanes assigned by theme. Ready to execute Wave 0 (21 unblocked issues, 41 points).

---

## What Are Waves and Lanes?

**Waves** (columns) represent the earliest point work can legally start:
- **Wave N = Longest prerequisite chain ending at that issue**
- Wave 0: no dependencies; unblocked today
- Wave 1: depends on Wave 0; start once that work lands
- Wave 2, 3, … deeper dependencies
- **Maximum depth: 7 waves**, so the schedule is **staffing-limited, not dependency-limited**

**Lanes** (rows) organize work by theme and staffing:
- **Lane A: Platform & Config** (11 issues) — Docker, secrets, CI/CD, build toolchain
- **Lane B: Credentials & Recovery** (11 issues) — registration, password reset, email delivery
- **Lane C: Sessions & Tokens** (8 issues) — JWT access tokens, rotating refresh cookies, token providers
- **Lane D: Authorization** (10 issues) — roles, claims, policy-based access control, Razor Page authz
- **Lane E: Frontend Runtime** (16 issues) — React auth state, login/logout UX, protected routes, profile
- **Lane F: Verification & Ops** (9 issues) — end-to-end tests, observability, performance, security scanning

The grid answers two questions:
1. **"When can I start this?"** → Look at the Wave (column)
2. **"Who should do it?"** → Look at the Lane (row)

---

## Key Metrics

| Metric | Value |
|--------|-------|
| Total issues | 65 |
| Total points | 126 |
| Release blockers | 42 |
| Graph depth (max wave) | 7 (0–6) |
| Parallel lanes | 6 |
| Wave 0 (unblocked) | 21 issues, 41 points |
| Wave 0 release blockers | 8 |

**Wave Distribution:**

| Wave | Issues | Points | Notes |
|------|--------|--------|-------|
| 0 | 21 | 41 | Startable today; staffing-limited capacity |
| 1 | 12 | 18 | Depends on Wave 0 |
| 2 | 11 | 20 | Depends on Waves 0–1 |
| 3 | 7 | 12 | Depends on Waves 0–2 |
| 4 | 6 | 11 | Depends on Waves 0–3 |
| 5 | 5 | 16 | Depends on Waves 0–4 |
| 6 | 3 | 8 | Deepest dependencies |

**Lane Breakdown:**

| Lane | Issues | Points | Role |
|------|--------|--------|------|
| A: Platform & Config | 11 | 21 | Infrastructure, secrets, build |
| B: Credentials & Recovery | 11 | 22 | Registration, password reset, email |
| C: Sessions & Tokens | 8 | 14 | Token providers, refresh mechanisms |
| D: Authorization | 10 | 19 | Roles, claims, policies, Razor auth |
| E: Frontend Runtime | 16 | 32 | React auth, login UX, protected routes |
| F: Verification & Ops | 9 | 18 | E2E tests, observability, security |

---

## Critical Corrections to the Original Plan

Three dependency cycles and inconsistencies were fixed:

### C1: Broke CIN-57 ↔ CIN-75 Cycle

**Issue:** CIN-57 and CIN-75 listed each other as prerequisites, creating an unbreakable cycle.

**Fix:** Removed CIN-57 → CIN-75; kept CIN-75 → CIN-57. Rationale: Workflow permissions (CIN-75) must be hardened before scanning jobs are added to that workflow (CIN-57).

```
Before: CIN-57 ← → CIN-75 (cycle)
After:  CIN-75 → CIN-57   (order preserved)
```

### C2: Data Protection Key Ring Wired Correctly

**Issue:** The plan had CIN-9 (Data Protection key ring) blocking CIN-19 (encrypted credentials token), but the code shows that Data Protection is only used for password-reset tokens, not refresh tokens.

**Fix:** Removed CIN-9 → CIN-19; added CIN-9 as a prerequisite to CIN-46, CIN-55, and CIN-77 (the issues that actually consume reset tokens).

**Rationale:** `RefreshToken.cs` uses only a TokenHash (no Data Protection). `DependencyInjection.cs` registers `AddDefaultTokenProviders` with `DataProtectionTokenProviderOptions` for password-reset tokens. An ephemeral key ring at startup invalidates outstanding reset links on reboot—a critical security property that must be established before password reset is usable.

### C3: Reversed CIN-77 / CIN-59 Edge

**Issue:** The plan had CIN-77 (Response security headers) enabling CIN-59 (HTTPS-only and referrer policy), but the timeline showed CIN-59 at position 19 and CIN-77 much later, contradicting the dependency.

**Fix:** Made CIN-59 a prerequisite of CIN-77. Rationale: The Referrer-Policy header (CIN-59) is part of the solution to CIN-77, not a consequence of it.

---

## Start Here: Wave 0 (Unblocked Work)

The board highlights Wave 0 issues ranked by how much work they release downstream. **These 21 issues are startable today** — pick the highest-value ones first:

| Priority | Issue | Title | Lane | Points | Unblocks |
|----------|-------|-------|------|--------|----------|
| 1 | CIN-62 | Auth system design | A | 3 | 15 downstream |
| 2 | CIN-63 | Credential model | B | 3 | 12 downstream |
| 3 | CIN-64 | Session + token design | C | 2 | 10 downstream |
| 4 | CIN-65 | Authorization model & RBAC | D | 3 | 14 downstream |
| 5 | CIN-66 | Frontend auth context + router | E | 5 | 8 downstream |
| … | … | … | … | … | … |

See the full board for all 21, ranked by leverage.

---

## How This Became the Ground Truth

1. **Started with Linear:** All 65 issues defined with titles, estimates, milestones, and a "Parallel lane" tag on each.
2. **Extracted prerequisites:** Built a dependency matrix from issue descriptions and relationships.
3. **Detected problems:**
   - 13 issues were scheduled before their own prerequisites (worst: CIN-60 at position 7 with four prerequisites at 8–11)
   - CIN-57 ↔ CIN-75 cycle
   - CIN-9 → CIN-19 contradiction
   - CIN-77 / CIN-59 direction wrong
   - 20 "Enables" edges had no matching prerequisite on the target
   - 31 prerequisites had no reciprocal entry
4. **Applied corrections:** Fixed the three dependency issues and re-derived the entire execution order.
5. **Validated:** The corrected plan is cycle-free and all 65 issues can be executed in topological order.

---

## Execution Order Algorithm

1. **Topological sort** by longest prerequisite chain (wave) to ensure all dependencies are satisfied
2. **Secondary sort** by lane to keep related work together
3. **Tertiary sort** by Linear issue ID to break ties consistently
4. **Result:** A total execution order where issue N is never scheduled before any of its prerequisites

---

## Execution Constraints

### Global Constraints

- **Warnings are errors** (StyleCop + .NET analyzers). A missing XML doc comment on a public member breaks the build.
- **Migrations are never applied automatically** — even on `dotnet run`. A fresh database needs `dotnet ef database update` for both `FilmDbContext` and `AuthDbContext`.
- **Edit only the root `CHANGELOG.md`** — `backend/CHANGELOG.md` is regenerated by the backend build.
- **Frontend naming:** The product is "Cinedex", the API path is `/movies-svc`, but the catalog entity is `Title`.
- **Docker must be running** for integration tests (Testcontainers Postgres, Mailpit).

### Auth-Specific Constraints

- **Identity schema isolation:** The `auth` schema is separate from `films`, with its own DbContext (`AuthDbContext`).
- **JWT lifespan:** Access tokens are short-lived (typically 1 hour). Refresh tokens are rotating cookies.
- **Data Protection key ring:** Ephemeral (in-memory), so every restart invalidates outstanding password-reset tokens.
- **Password reset emails:** Branded HTML via MailKit + Mailpit (dev). SMTP credentials in `.env`.
- **Roles and claims:** Sourced from the `aspnetroles`, `aspnetusers`, and `aspnetuserclaims` tables.

---

## The Wave 0 Strategy

With 21 unblocked issues and only 6 lanes, **Wave 0 is the staffing bottleneck**. Prioritize high-leverage design work first:

1. **Design the models** (CIN-62, CIN-63, CIN-64, CIN-65) — agree on schema, DTOs, and policies
2. **Implement infrastructure** (CIN-69, CIN-70, CIN-71) — secrets, email, logging
3. **Build registration & login** (CIN-8, CIN-9, CIN-10) — happy path first
4. **Connect the frontend** (CIN-66, CIN-67) — auth context and router
5. **Add password reset** (CIN-63, CIN-11) — completes credential management

Once Wave 0 is shipping, Wave 1 becomes unblocked automatically—no new choreography needed.

---

## Links

- **Interactive Board:** [Cinedex Auth Execution Board](https://claude.ai/code/artifact/90aa2d67-a58c-47f3-b97c-e14cdced5bc9)
- **Linear Project:** [Cinedex / CIN](https://linear.app/felipe-ferreira/project/CIN)
- **Auth & Security Model:** `docs/auth-security-model.md`
- **Verification:** The plan was validated against 11 rules in `verify_v2.py`:
  - 65 unique issues in coverage
  - No issue precedes its prerequisites
  - Dependency graph is cycle-free
  - "Enables" column is the exact inverse of "Prerequisites"
  - Waves match longest prerequisite depth
  - Criticality matches Linear (42 release blockers)
  - Every issue is in exactly one gate
  - All three specific corrections applied correctly

---

## Visual Convention

On the swimlane board:

- **Selected (purple/accent)** — click a card; related cards light up
- **Must finish first (orange/up)** — prerequisites; these block the selection
- **This unlocks (teal/down)** — dependents; releasing this unblocks them
- **Release blocker (bold left edge)** — ships with the release; delays this delay the whole product
- **Critical path (purple fill)** — part of the longest dependency chain in the whole programme

The dependency graph (bottom of the board) shows the transitive closure—what blocks what, layered by wave. Zoom and pan with scroll and drag; click any node to re-centre or jump to Linear.

---

## FAQ

**Q: Can we parallelize more?**  
A: Waves are limits, not targets. If you have the staffing for it, Lane A and Lane B can run side-by-side in Wave 0. The board shows what *can* start; what you *do* start depends on your team capacity.

**Q: What if a prerequisite changes?**  
A: Update the Linear issue description, re-run `verify_v2.py`, and re-generate the board. The interactive visualization is a snapshot; the source of truth is Linear.

**Q: Why 7 waves for 65 issues?**  
A: The dependency graph has a maximum depth of 7—some issues depend on a chain of 7 predecessors. With 21 issues in Wave 0 and only 6 lanes, you're staffing-limited long before you hit the dependency wall.

**Q: Should we track "waves" as sprint boundaries?**  
A: No. Waves describe *precedence*, not *duration*. A Wave 0 issue might take 1 point or 5. The board is a dependency DAG, not a calendar. Sprints are orthogonal—you can organize them however your team prefers.

**Q: What if we skip an issue?**  
A: Any issue you skip unblocks its dependents automatically. For example, if you defer CIN-57, CIN-77 can still start once CIN-59 lands.

