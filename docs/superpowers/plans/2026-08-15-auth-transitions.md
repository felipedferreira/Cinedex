# Auth Rack-Focus Transitions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the Cinedex auth flow the "rack focus" transition — the outgoing screen defocuses and recedes while the incoming screen arrives out of focus and resolves — with five variants that encode direction and outcome.

**Architecture:** A pure GSAP timeline builder (`buildRackFocusTimeline`) drives two DOM nodes through CSS custom properties. A router-free `ScreenTransition` component clones the outgoing DOM into an `inert` overlay and plays the timeline over it. Navigation triggers capture the clone *before* React re-renders, via a context hook that the router link and `ForgotPasswordScreen` both call.

**Tech Stack:** GSAP 3.15 (already a dependency of `@cinedex/solution`), React 19, TanStack Router, Vitest + Testing Library, Storybook 10.

**Spec:** [`docs/superpowers/specs/2026-08-15-auth-transitions-design.md`](../specs/2026-08-15-auth-transitions-design.md)

## Global Constraints

- **All new source lives in `frontend/packages/solution/src/transitions/`** except the router wiring, which goes in `frontend/apps/cinedex-app/src/routes/__root.tsx`.
- **`@cinedex/solution` must not import a router.** No `@tanstack/react-router` import anywhere under `packages/`. This is the package's hard rule.
- **Run every command from `frontend/`**, the npm workspace root. Never from a package directory.
- **Warnings are not errors on the frontend**, but `npm run lint` and `npm run format:check` are required CI checks. Run `npm run format` before every commit.
- **jsdom has no `requestAnimationFrame`.** Every test drives timelines with `tl.progress(p)` on a **paused** timeline, never by waiting.
- **`src/test/setup.ts` stubs `matchMedia` with `matches = true`.** Every existing test therefore takes the *reduced-motion* path by default. Tests of full-motion behaviour must override the stub explicitly.
- **Animated values are written as CSS custom properties**, never as `style.filter` / `style.transform` directly. jsdom's `CSSStyleDeclaration` implements only a subset of real CSS properties and may silently drop `filter`; custom properties are plain strings and always round-trip. The panes consume them in static inline styles.
- **Easing uses a hand-rolled cubic-bezier solver, not GSAP's `powerN` eases.** `Brand/timelines.ts` documents that GSAP's `powerN` naming is off by one from the usual vocabulary and that reaching for `power3` when porting a "cubic ease-out" is a real, already-encountered bug. The design specifies exact beziers; solve them exactly.
- Exact design values, copied from the spec:
  - Outgoing easing `cubic-bezier(.4, 0, .6, 1)`, incoming easing `cubic-bezier(.16, .8, .24, 1)`
  - Outgoing blur `0 → 9px`, incoming blur `11px → 0`
  - Outgoing opacity `1 → 0`, incoming opacity `0 → 1`
- Commit messages follow Conventional Commits (`type(scope): summary`) and end with:
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  ```

---

## File Structure

| File | Responsibility |
| --- | --- |
| `packages/solution/src/transitions/cubicBezier.ts` | Solve a CSS `cubic-bezier(x1,y1,x2,y2)` into a `(t) => number` GSAP accepts as an ease. Pure, no GSAP import. |
| `packages/solution/src/transitions/cubicBezier.test.ts` | Verifies the solver against known bezier identities. |
| `packages/solution/src/transitions/rackFocus.ts` | The variant table and `buildRackFocusTimeline`. Pure, returns a paused timeline, no React. |
| `packages/solution/src/transitions/rackFocus.test.ts` | The variant table as a `describe.each` fixture. |
| `packages/solution/src/transitions/authEdges.ts` | `variantForEdge(from, to, wentBack)` — the design's `MAP · 2A` table. Pure. |
| `packages/solution/src/transitions/authEdges.test.ts` | One case per mapped edge. |
| `packages/solution/src/transitions/ScreenTransition.tsx` | The clone host + `useCaptureOutgoing` context. Router-free. |
| `packages/solution/src/transitions/ScreenTransition.test.tsx` | Structural and accessibility contract. |
| `packages/solution/src/index.ts` | Barrel — add the new public exports. |
| `packages/solution/src/screens/ForgotPasswordScreen.tsx` | Modify: capture before `setStep`, wrap both branches. |
| `apps/cinedex-app/src/routes/__root.tsx` | Modify: `RouterScreenTransition` around `<Outlet />`; `RouterLink` captures on click. |
| `apps/storybook/src/solution/AuthTransitions.stories.tsx` | The review rail. |

---

## Task 1: The cubic-bezier ease solver

**Files:**
- Create: `frontend/packages/solution/src/transitions/cubicBezier.ts`
- Test: `frontend/packages/solution/src/transitions/cubicBezier.test.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: `cubicBezierEase(x1: number, y1: number, x2: number, y2: number): (t: number) => number`

- [ ] **Step 1: Write the failing test**

Create `frontend/packages/solution/src/transitions/cubicBezier.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { cubicBezierEase } from './cubicBezier';

/**
 * A cubic-bezier ease is pinned at both ends and monotonic in between, and
 * `cubic-bezier(0, 0, 1, 1)` is the identity. Those three properties are enough
 * to catch a solver that has converged on the wrong root, which is the failure
 * mode that matters — a subtly wrong curve looks fine in review and is exactly
 * the bug `Brand/timelines.ts` warns about in its `powerN` comment.
 */
describe('cubicBezierEase', () => {
  it('is pinned at both ends', () => {
    const ease = cubicBezierEase(0.4, 0, 0.6, 1);

    expect(ease(0)).toBeCloseTo(0, 6);
    expect(ease(1)).toBeCloseTo(1, 6);
  });

  it('reproduces the identity curve', () => {
    const ease = cubicBezierEase(0, 0, 1, 1);

    for (const t of [0.1, 0.25, 0.5, 0.75, 0.9]) {
      expect(ease(t)).toBeCloseTo(t, 4);
    }
  });

  it('is symmetric about the midpoint for a symmetric curve', () => {
    const ease = cubicBezierEase(0.4, 0, 0.6, 1);

    expect(ease(0.5)).toBeCloseTo(0.5, 4);
    expect(ease(0.25) + ease(0.75)).toBeCloseTo(1, 3);
  });

  it('front-loads the incoming curve, which is a strong ease-out', () => {
    const ease = cubicBezierEase(0.16, 0.8, 0.24, 1);

    // More than three quarters of the distance is covered in the first half.
    expect(ease(0.5)).toBeGreaterThan(0.75);
    expect(ease(0.5)).toBeLessThan(1);
  });

  it('increases monotonically', () => {
    const ease = cubicBezierEase(0.16, 0.8, 0.24, 1);
    let previous = -1;

    for (let i = 0; i <= 100; i += 1) {
      const value = ease(i / 100);
      expect(value).toBeGreaterThanOrEqual(previous);
      previous = value;
    }
  });

  it('clamps input outside the unit interval', () => {
    const ease = cubicBezierEase(0.4, 0, 0.6, 1);

    expect(ease(-0.5)).toBe(0);
    expect(ease(1.5)).toBe(1);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w @cinedex/solution -- cubicBezier`

Expected: FAIL — `Failed to resolve import "./cubicBezier"`.

- [ ] **Step 3: Write minimal implementation**

Create `frontend/packages/solution/src/transitions/cubicBezier.ts`:

```ts
/**
 * Solves a CSS `cubic-bezier(x1, y1, x2, y2)` into the `(t) => number` function
 * GSAP accepts wherever it takes an `ease`.
 *
 * This exists rather than mapping the design's curves onto GSAP's named eases
 * because that mapping is where this repo has already been bitten once:
 * `Brand/timelines.ts` documents at length that GSAP's `powerN` naming is off by
 * one from the usual quadratic/cubic vocabulary, and that "fixing" a cubic
 * ease-out to `power3` overshoots the intended curve by up to 0.11. The design
 * hands us exact beziers; solving them exactly removes the whole class of
 * problem and costs about thirty lines.
 *
 * The method is the browsers' own: Newton-Raphson on x(t) for a few iterations,
 * falling back to bisection when the derivative is too flat for Newton to be
 * trusted. Both control-point x values are clamped to [0, 1] because a CSS
 * cubic-bezier outside that range is not a function of t.
 */

const NEWTON_ITERATIONS = 8;
const NEWTON_MIN_SLOPE = 0.001;
const SUBDIVISION_EPSILON = 1e-7;
const SUBDIVISION_MAX_ITERATIONS = 20;

function clamp01(value: number): number {
  return value < 0 ? 0 : value > 1 ? 1 : value;
}

/** The bezier's a/b/c coefficients for one axis, given its two control points. */
function coefficients(p1: number, p2: number): [number, number, number] {
  const c = 3 * p1;
  const b = 3 * (p2 - p1) - c;
  const a = 1 - c - b;
  return [a, b, c];
}

function evaluate(t: number, [a, b, c]: [number, number, number]): number {
  return ((a * t + b) * t + c) * t;
}

function slope(t: number, [a, b, c]: [number, number, number]): number {
  return (3 * a * t + 2 * b) * t + c;
}

/** Finds the parametric t whose x equals the given progress. */
function solveForX(
  x: number,
  xCoefficients: [number, number, number],
): number {
  let t = x;

  for (let i = 0; i < NEWTON_ITERATIONS; i += 1) {
    const currentSlope = slope(t, xCoefficients);
    if (Math.abs(currentSlope) < NEWTON_MIN_SLOPE) {
      break;
    }
    t -= (evaluate(t, xCoefficients) - x) / currentSlope;
  }

  // Newton can wander off a flat stretch of the curve; bisection cannot.
  if (t < 0 || t > 1) {
    let low = 0;
    let high = 1;
    t = x;

    for (let i = 0; i < SUBDIVISION_MAX_ITERATIONS; i += 1) {
      const current = evaluate(t, xCoefficients);
      if (Math.abs(current - x) < SUBDIVISION_EPSILON) {
        break;
      }
      if (current < x) {
        low = t;
      } else {
        high = t;
      }
      t = (low + high) / 2;
    }
  }

  return t;
}

export function cubicBezierEase(
  x1: number,
  y1: number,
  x2: number,
  y2: number,
): (t: number) => number {
  const xCoefficients = coefficients(clamp01(x1), clamp01(x2));
  const yCoefficients = coefficients(y1, y2);

  // A straight line needs no solving, and skipping it keeps the identity exact.
  if (x1 === y1 && x2 === y2) {
    return clamp01;
  }

  return (t: number): number => {
    const progress = clamp01(t);
    if (progress === 0 || progress === 1) {
      return progress;
    }
    return evaluate(solveForX(progress, xCoefficients), yCoefficients);
  };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w @cinedex/solution -- cubicBezier`

Expected: PASS — 6 tests.

- [ ] **Step 5: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/packages/solution/src/transitions/cubicBezier.ts frontend/packages/solution/src/transitions/cubicBezier.test.ts
git commit -m "$(cat <<'EOF'
feat(solution): add a cubic-bezier ease solver for GSAP

The auth transition design specifies exact CSS beziers. Mapping them onto
GSAP's named eases is where this repo has already been bitten once - see
the powerN comment in Brand/timelines.ts - so solve them exactly instead.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: The rack-focus timeline builder

**Files:**
- Create: `frontend/packages/solution/src/transitions/rackFocus.ts`
- Test: `frontend/packages/solution/src/transitions/rackFocus.test.ts`

**Interfaces:**
- Consumes: `cubicBezierEase` from Task 1.
- Produces:
  - `type TransitionVariant = 'forward' | 'back' | 'lockout' | 'accountReady' | 'coldLoad'`
  - `interface RackFocusOptions { variant: TransitionVariant; reducedMotion?: boolean }`
  - `buildRackFocusTimeline(outgoing: HTMLElement | null, incoming: HTMLElement, options: RackFocusOptions): gsap.core.Timeline`
  - `const PANE_STYLE: Record<string, string>` — the static inline style a pane must carry for the custom properties to have any effect.

- [ ] **Step 1: Write the failing test**

Create `frontend/packages/solution/src/transitions/rackFocus.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { buildRackFocusTimeline, type TransitionVariant } from './rackFocus';

/**
 * These scrub the timeline directly rather than rendering a component, the same
 * approach `Brand/timelines.test.ts` uses and for the same reason: a GSAP
 * timeline built `paused` can be driven to any instant with `progress(p)` and
 * flushes its writes synchronously, with no ticker and no rAF — neither of
 * which jsdom has.
 *
 * The expectations are the DESIGNED values from
 * docs/superpowers/specs/2026-08-15-auth-transitions-design.md, not recorded
 * output, so these are regression tests against the spec rather than a snapshot
 * of whatever the first implementation happened to produce.
 */

function makePane(): HTMLElement {
  const el = document.createElement('div');
  document.body.append(el);
  return el;
}

function opacityOf(el: HTMLElement): number {
  return Number(el.style.getPropertyValue('--cdx-pane-opacity'));
}

function blurOf(el: HTMLElement): number {
  return Number.parseFloat(el.style.getPropertyValue('--cdx-pane-blur'));
}

function scaleOf(el: HTMLElement): number {
  return Number(el.style.getPropertyValue('--cdx-pane-scale'));
}

interface VariantExpectation {
  duration: number;
  outScale: number | null;
  inScale: number;
  blurred: boolean;
}

/** The spec's variant table, verbatim. Durations in seconds. */
const VARIANTS: [TransitionVariant, VariantExpectation][] = [
  ['forward', { duration: 0.82, outScale: 0.94, inScale: 1.05, blurred: true }],
  ['back', { duration: 0.82, outScale: 1.05, inScale: 0.94, blurred: true }],
  ['lockout', { duration: 0.52, outScale: 1, inScale: 1, blurred: true }],
  [
    'accountReady',
    { duration: 1.02, outScale: 0.94, inScale: 1.05, blurred: true },
  ],
  ['coldLoad', { duration: 0.64, outScale: null, inScale: 1.05, blurred: true }],
];

describe.each(VARIANTS)('buildRackFocusTimeline — %s', (variant, expected) => {
  it('runs for the designed duration', () => {
    const tl = buildRackFocusTimeline(makePane(), makePane(), { variant });

    expect(tl.duration()).toBeCloseTo(expected.duration, 5);
    tl.kill();
  });

  it('starts the outgoing pane fully visible and undistorted', () => {
    const outgoing = makePane();
    const tl = buildRackFocusTimeline(outgoing, makePane(), { variant });

    tl.progress(0);
    expect(opacityOf(outgoing)).toBeCloseTo(1, 3);
    expect(blurOf(outgoing)).toBeCloseTo(0, 3);
    expect(scaleOf(outgoing)).toBeCloseTo(1, 4);
    tl.kill();
  });

  it('settles the incoming pane on exactly the resting state', () => {
    const incoming = makePane();
    const tl = buildRackFocusTimeline(makePane(), incoming, { variant });

    tl.progress(1);
    expect(opacityOf(incoming)).toBeCloseTo(1, 3);
    expect(blurOf(incoming)).toBeCloseTo(0, 3);
    expect(scaleOf(incoming)).toBeCloseTo(1, 4);
    tl.kill();
  });

  it('takes the incoming pane through the designed scale', () => {
    const incoming = makePane();
    const tl = buildRackFocusTimeline(makePane(), incoming, { variant });

    tl.progress(0);
    expect(scaleOf(incoming)).toBeCloseTo(expected.inScale, 4);
    tl.kill();
  });

  it('takes the outgoing pane through the designed scale', () => {
    const outgoing = makePane();
    const tl = buildRackFocusTimeline(outgoing, makePane(), { variant });

    tl.progress(1);
    if (expected.outScale === null) {
      // coldLoad has no outgoing half at all.
      expect(scaleOf(outgoing)).toBeCloseTo(1, 4);
    } else {
      expect(scaleOf(outgoing)).toBeCloseTo(expected.outScale, 4);
    }
    tl.kill();
  });
});

describe('buildRackFocusTimeline — direction', () => {
  it('inverts scale between forward and back, so retreating reads as pulling out', () => {
    const forwardOut = makePane();
    const forwardIn = makePane();
    const backOut = makePane();
    const backIn = makePane();

    const forward = buildRackFocusTimeline(forwardOut, forwardIn, {
      variant: 'forward',
    });
    const back = buildRackFocusTimeline(backOut, backIn, { variant: 'back' });

    forward.progress(1);
    back.progress(1);
    expect(scaleOf(forwardOut)).toBeLessThan(1);
    expect(scaleOf(backOut)).toBeGreaterThan(1);

    forward.progress(0);
    back.progress(0);
    expect(scaleOf(forwardIn)).toBeGreaterThan(1);
    expect(scaleOf(backIn)).toBeLessThan(1);

    forward.kill();
    back.kill();
  });
});

describe('buildRackFocusTimeline — lockout', () => {
  it('holds scale at 1 throughout, so failure does not read as progress', () => {
    const outgoing = makePane();
    const incoming = makePane();
    const tl = buildRackFocusTimeline(outgoing, incoming, {
      variant: 'lockout',
    });

    for (const t of [0, 0.25, 0.5, 0.75, 1]) {
      tl.progress(t);
      expect(scaleOf(outgoing)).toBeCloseTo(1, 4);
      expect(scaleOf(incoming)).toBeCloseTo(1, 4);
    }
    tl.kill();
  });

  it('still blurs, so it reads as a move rather than a swap', () => {
    const outgoing = makePane();
    const tl = buildRackFocusTimeline(outgoing, makePane(), {
      variant: 'lockout',
    });

    tl.progress(1);
    expect(blurOf(outgoing)).toBeCloseTo(9, 2);
    tl.kill();
  });
});

describe('buildRackFocusTimeline — accountReady', () => {
  it('holds everything still for the first 200ms', () => {
    const outgoing = makePane();
    const incoming = makePane();
    const tl = buildRackFocusTimeline(outgoing, incoming, {
      variant: 'accountReady',
    });

    // 0.2s of 1.02s. Nothing has moved yet.
    tl.progress(0.2 / 1.02);
    expect(opacityOf(outgoing)).toBeCloseTo(1, 3);
    expect(blurOf(outgoing)).toBeCloseTo(0, 3);
    expect(opacityOf(incoming)).toBeCloseTo(0, 3);
    tl.kill();
  });
});

describe('buildRackFocusTimeline — coldLoad', () => {
  it('accepts a null outgoing pane', () => {
    const incoming = makePane();
    const tl = buildRackFocusTimeline(null, incoming, { variant: 'coldLoad' });

    tl.progress(1);
    expect(opacityOf(incoming)).toBeCloseTo(1, 3);
    tl.kill();
  });

  it('starts the incoming pane immediately, with no delay to wait out', () => {
    const incoming = makePane();
    const tl = buildRackFocusTimeline(null, incoming, { variant: 'coldLoad' });

    tl.progress(0.5);
    expect(opacityOf(incoming)).toBeGreaterThan(0.5);
    tl.kill();
  });
});

describe('buildRackFocusTimeline — reduced motion', () => {
  it.each(VARIANTS.map(([variant]) => variant))(
    'collapses %s to the same 200ms cross-fade',
    (variant) => {
      const outgoing = makePane();
      const incoming = makePane();
      const tl = buildRackFocusTimeline(outgoing, incoming, {
        variant,
        reducedMotion: true,
      });

      expect(tl.duration()).toBeCloseTo(0.2, 5);

      for (const t of [0, 0.5, 1]) {
        tl.progress(t);
        expect(blurOf(outgoing)).toBeCloseTo(0, 4);
        expect(blurOf(incoming)).toBeCloseTo(0, 4);
        expect(scaleOf(outgoing)).toBeCloseTo(1, 4);
        expect(scaleOf(incoming)).toBeCloseTo(1, 4);
      }

      tl.progress(1);
      expect(opacityOf(outgoing)).toBeCloseTo(0, 3);
      expect(opacityOf(incoming)).toBeCloseTo(1, 3);
      tl.kill();
    },
  );
});

describe('buildRackFocusTimeline — overlap', () => {
  it('leaves both panes defocused and unreadable in the middle', () => {
    const outgoing = makePane();
    const incoming = makePane();
    const tl = buildRackFocusTimeline(outgoing, incoming, {
      variant: 'forward',
    });

    // 0.35s of 0.82s — inside the 340ms overlap window.
    tl.progress(0.35 / 0.82);
    expect(opacityOf(outgoing)).toBeLessThan(1);
    expect(opacityOf(incoming)).toBeLessThan(1);
    expect(blurOf(outgoing)).toBeGreaterThan(0);
    expect(blurOf(incoming)).toBeGreaterThan(0);
    tl.kill();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w @cinedex/solution -- rackFocus`

Expected: FAIL — `Failed to resolve import "./rackFocus"`.

- [ ] **Step 3: Write minimal implementation**

Create `frontend/packages/solution/src/transitions/rackFocus.ts`:

```ts
import { gsap } from 'gsap';
import { cubicBezierEase } from './cubicBezier';

/**
 * The "rack focus" transition between two auth screens, as a GSAP timeline.
 *
 * Every animated value goes out as a **CSS custom property**, not as
 * `style.filter` / `style.transform`. Two reasons, both load-bearing:
 *
 *  1. **jsdom.** Its `CSSStyleDeclaration` implements a subset of real CSS
 *     properties and can silently drop `filter`, which would leave the screen
 *     correct and the tests asserting nothing. Custom properties are plain
 *     strings and always round-trip, which is what lets the whole 820ms
 *     sequence be scrubbed in `rackFocus.test.ts` rather than verified by eye.
 *  2. **One writer per property.** The panes carry `PANE_STYLE` as a static
 *     inline style that reads all three variables; the timeline only ever moves
 *     the variables. Nothing composes a transform string from two places.
 *
 * The timeline is returned **paused**, exactly like `Brand/timelines.ts`, so the
 * caller owns whether it plays or is scrubbed.
 */

export type TransitionVariant =
  | 'forward'
  | 'back'
  | 'lockout'
  | 'accountReady'
  | 'coldLoad';

export interface RackFocusOptions {
  variant: TransitionVariant;
  /** Collapses every variant to the same 200ms opacity cross-fade. */
  reducedMotion?: boolean;
}

/**
 * The static inline style a pane must carry for the custom properties to mean
 * anything. Exported so `ScreenTransition` and the Storybook rail cannot drift
 * from what the timeline writes.
 */
export const PANE_STYLE = {
  opacity: 'var(--cdx-pane-opacity, 1)',
  filter: 'blur(var(--cdx-pane-blur, 0px))',
  transform: 'scale(var(--cdx-pane-scale, 1))',
  transformOrigin: '50% 42%',
  willChange: 'opacity, filter, transform',
} as const;

/** The design's two curves. Solved exactly — see `cubicBezier.ts`. */
const EASE_OUT = cubicBezierEase(0.4, 0, 0.6, 1);
const EASE_IN = cubicBezierEase(0.16, 0.8, 0.24, 1);

const OUT_BLUR = 9;
const IN_BLUR = 11;

/** The base forward move, in seconds. Every other variant derives from it. */
const BASE = {
  out: { at: 0, dur: 0.52 },
  in: { at: 0.18, dur: 0.64 },
} as const;

/**
 * `lockout` is specified as 520ms total, but the base transition's outgoing half
 * alone is 520ms — so the number cannot mean "the standard move, shortened at
 * the end". It is the forward timing scaled to fit, which preserves the 41%
 * overlap ratio. See the spec's "Two ambiguities" section.
 */
const LOCKOUT_SCALE = 0.52 / 0.82;

/** One pane's start and end state. */
interface PaneState {
  opacity: number;
  blur: number;
  scale: number;
}

interface PaneSpec {
  at: number;
  dur: number;
  from: PaneState;
  to: PaneState;
  ease: (t: number) => number;
}

interface VariantSpec {
  /** `null` on `coldLoad`, which has no screen to leave. */
  out: PaneSpec | null;
  in: PaneSpec;
}

const RESTING: PaneState = { opacity: 1, blur: 0, scale: 1 };

function outSpec(at: number, dur: number, scale: number): PaneSpec {
  return {
    at,
    dur,
    from: RESTING,
    to: { opacity: 0, blur: OUT_BLUR, scale },
    ease: EASE_OUT,
  };
}

function inSpec(at: number, dur: number, scale: number): PaneSpec {
  return {
    at,
    dur,
    from: { opacity: 0, blur: IN_BLUR, scale },
    to: RESTING,
    ease: EASE_IN,
  };
}

const VARIANT_SPECS: Record<TransitionVariant, VariantSpec> = {
  forward: {
    out: outSpec(BASE.out.at, BASE.out.dur, 0.94),
    in: inSpec(BASE.in.at, BASE.in.dur, 1.05),
  },
  // Scale inverted: the outgoing screen grows as you pull back out of it.
  back: {
    out: outSpec(BASE.out.at, BASE.out.dur, 1.05),
    in: inSpec(BASE.in.at, BASE.in.dur, 0.94),
  },
  // No scale at all, so a lockout does not read as forward progress.
  lockout: {
    out: outSpec(0, BASE.out.dur * LOCKOUT_SCALE, 1),
    in: inSpec(BASE.in.at * LOCKOUT_SCALE, BASE.in.dur * LOCKOUT_SCALE, 1),
  },
  // A 200ms beat of stillness before the move, on the one screen that earns it.
  accountReady: {
    out: outSpec(0.2 + BASE.out.at, BASE.out.dur, 0.94),
    in: inSpec(0.2 + BASE.in.at, BASE.in.dur, 1.05),
  },
  coldLoad: {
    out: null,
    in: inSpec(0, BASE.in.dur, 1.05),
  },
};

/** One 200ms opacity cross-fade, shared by every variant. */
const REDUCED_SPEC: VariantSpec = {
  out: {
    at: 0,
    dur: 0.2,
    from: RESTING,
    to: { opacity: 0, blur: 0, scale: 1 },
    ease: EASE_OUT,
  },
  in: {
    at: 0,
    dur: 0.2,
    from: { opacity: 0, blur: 0, scale: 1 },
    to: RESTING,
    ease: EASE_IN,
  },
};

function write(element: HTMLElement, state: PaneState): void {
  element.style.setProperty('--cdx-pane-opacity', state.opacity.toFixed(3));
  element.style.setProperty('--cdx-pane-blur', `${state.blur.toFixed(3)}px`);
  element.style.setProperty('--cdx-pane-scale', state.scale.toFixed(4));
}

/**
 * Adds one pane's tween to the timeline. The tween runs over a proxy object and
 * a per-timeline `onUpdate` flushes it, rather than GSAP writing the DOM itself
 * — the same shape `Brand/timelines.ts` uses for its derived values, and what
 * keeps the written format stable enough to assert on.
 */
function addPane(
  timeline: gsap.core.Timeline,
  element: HTMLElement,
  spec: PaneSpec,
): void {
  const proxy: PaneState = { ...spec.from };
  write(element, proxy);

  timeline.to(
    proxy,
    {
      opacity: spec.to.opacity,
      blur: spec.to.blur,
      scale: spec.to.scale,
      duration: spec.dur,
      ease: spec.ease,
      onUpdate: () => {
        write(element, proxy);
      },
    },
    spec.at,
  );
}

export function buildRackFocusTimeline(
  outgoing: HTMLElement | null,
  incoming: HTMLElement,
  options: RackFocusOptions,
): gsap.core.Timeline {
  const spec = options.reducedMotion
    ? REDUCED_SPEC
    : VARIANT_SPECS[options.variant];

  const timeline = gsap.timeline({ paused: true });

  if (outgoing) {
    if (spec.out) {
      addPane(timeline, outgoing, spec.out);
    } else {
      // coldLoad still has to establish the resting state, or a stale custom
      // property from a previous transition would survive on a reused node.
      write(outgoing, RESTING);
    }
  }

  addPane(timeline, incoming, spec.in);

  return timeline;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w @cinedex/solution -- rackFocus`

Expected: PASS — 5 variants × 5 shared cases, plus 9 targeted cases.

> If `coldLoad`'s duration comes out as `0.64` but `forward`'s comes out as `0.82`, the timeline is right. If any variant's duration is `0`, `addPane` was skipped — check that `spec.in` is always added.

- [ ] **Step 5: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/packages/solution/src/transitions/rackFocus.ts frontend/packages/solution/src/transitions/rackFocus.test.ts
git commit -m "$(cat <<'EOF'
feat(solution): add the rack-focus transition timeline

Five variants - forward, back, lockout, accountReady, coldLoad - plus a
reduced-motion collapse, built as a paused GSAP timeline so the whole
sequence is scrubbable in jsdom. The spec's variant table is the test
fixture.

Animated values go out as CSS custom properties rather than style.filter
and style.transform, because jsdom's CSSStyleDeclaration can silently
drop filter and leave the tests asserting nothing.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: The edge map

**Files:**
- Create: `frontend/packages/solution/src/transitions/authEdges.ts`
- Test: `frontend/packages/solution/src/transitions/authEdges.test.ts`

**Interfaces:**
- Consumes: `TransitionVariant` from Task 2.
- Produces: `variantForEdge(from: string | null, to: string, wentBack: boolean): TransitionVariant`

- [ ] **Step 1: Write the failing test**

Create `frontend/packages/solution/src/transitions/authEdges.test.ts`:

```ts
import { describe, expect, it } from 'vitest';
import { variantForEdge } from './authEdges';

describe('variantForEdge', () => {
  it('treats a first render as a cold load, whatever the destination', () => {
    expect(variantForEdge(null, '/login', false)).toBe('coldLoad');
    expect(variantForEdge(null, '/reset-password', false)).toBe('coldLoad');
    expect(variantForEdge(null, '/login?state=locked', false)).toBe('coldLoad');
  });

  it('advances forward between sibling auth screens', () => {
    expect(variantForEdge('/login', '/register', false)).toBe('forward');
    expect(variantForEdge('/login', '/forgot-password', false)).toBe('forward');
    expect(variantForEdge('/signed-out', '/login', false)).toBe('forward');
  });

  it('reads a lockout as its own variant, not as progress', () => {
    expect(variantForEdge('/', '/login?state=locked', false)).toBe('lockout');
    expect(variantForEdge('/login', '/login?state=locked', false)).toBe(
      'lockout',
    );
  });

  it('recedes into the signed-out screen, because the user is leaving', () => {
    expect(variantForEdge('/', '/signed-out', false)).toBe('back');
    expect(variantForEdge('/login', '/signed-out', false)).toBe('back');
  });

  it('runs backward whenever history went backward, whatever the map says', () => {
    expect(variantForEdge('/login', '/register', true)).toBe('back');
    expect(variantForEdge('/signed-out', '/login', true)).toBe('back');
  });

  // The history override must not swallow the two variants that carry meaning
  // the direction cannot: a lockout is still a lockout when reached via Back.
  it('does not let the history override outrank a lockout', () => {
    expect(variantForEdge('/login', '/login?state=locked', true)).toBe(
      'lockout',
    );
  });

  it('holds before the account-ready screen', () => {
    expect(variantForEdge('/register', '/account-ready', false)).toBe(
      'accountReady',
    );
  });

  it('ignores a change that is not a change', () => {
    expect(variantForEdge('/login', '/login', false)).toBe('forward');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w @cinedex/solution -- authEdges`

Expected: FAIL — `Failed to resolve import "./authEdges"`.

- [ ] **Step 3: Write minimal implementation**

Create `frontend/packages/solution/src/transitions/authEdges.ts`:

```ts
import type { TransitionVariant } from './rackFocus';

/**
 * Resolves which rack-focus variant an edge in the auth flow runs, from the
 * design's `MAP · 2A` table.
 *
 * `from` and `to` are full locations — pathname plus search — because two of the
 * design's edges are not pathname changes at all: the lockout is
 * `/login?state=locked`, and `ForgotPasswordScreen`'s request/sent step never
 * leaves `/forgot-password`. Callers key on whatever string identifies the
 * screen, so an in-screen step passes something like
 * `/forgot-password#sent`.
 *
 * Precedence is deliberate and tested:
 *
 *  1. **Cold load wins outright** — there is no outgoing screen to animate, so
 *     no other variant can apply.
 *  2. **Lockout outranks the history override** — arriving at a lockout via the
 *     Back button is still a lockout. The direction is the less important fact.
 *  3. **History backward outranks the map** — the Back button must read as
 *     backward even on an edge the map calls forward.
 */

/** Destinations that always recede, whichever way you reached them. */
const RECEDING = ['/signed-out'];

/** Destinations that take a beat before moving. */
const HOLDING = ['/account-ready'];

function pathOf(location: string): string {
  const [path] = location.split('?');
  return path;
}

function isLockout(location: string): boolean {
  return location.includes('state=locked');
}

export function variantForEdge(
  from: string | null,
  to: string,
  wentBack: boolean,
): TransitionVariant {
  if (from === null) {
    return 'coldLoad';
  }

  if (isLockout(to)) {
    return 'lockout';
  }

  if (wentBack || RECEDING.includes(pathOf(to))) {
    return 'back';
  }

  if (HOLDING.includes(pathOf(to))) {
    return 'accountReady';
  }

  return 'forward';
}
```

> `/account-ready` has no route in the app yet — it is in the map because the
> design specifies it and the Storybook rail exercises it. Adding the route
> later needs no change here.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w @cinedex/solution -- authEdges`

Expected: PASS — 8 tests.

- [ ] **Step 5: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/packages/solution/src/transitions/authEdges.ts frontend/packages/solution/src/transitions/authEdges.test.ts
git commit -m "$(cat <<'EOF'
feat(solution): map auth flow edges to transition variants

The design's MAP 2A table as a pure function. Keys on full locations
rather than pathnames, because the lockout is a search param and the
forgot-password step change never leaves its route.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: The `ScreenTransition` clone host

**Files:**
- Create: `frontend/packages/solution/src/transitions/ScreenTransition.tsx`
- Test: `frontend/packages/solution/src/transitions/ScreenTransition.test.tsx`
- Modify: `frontend/packages/solution/src/index.ts`

**Interfaces:**
- Consumes: `buildRackFocusTimeline`, `PANE_STYLE`, `TransitionVariant` from Task 2.
- Produces:
  - `interface ScreenTransitionProps { transitionKey: string; variant: TransitionVariant; children: ReactNode }`
  - `function ScreenTransition(props: ScreenTransitionProps): ReactElement`
  - `function useCaptureOutgoing(): () => void` — returns a no-op outside a `ScreenTransition`.

**Why an imperative capture rather than cloning in an effect:** React commits DOM changes *before* layout effects run, so by the time any effect fires, the outgoing screen is already gone. Reading a ref during render would work but React Compiler is enabled in this workspace and may skip re-running the component body. The reliable moment is the event that *causes* the change — a link click, a `popstate`, a `setState` in a submit handler — all of which run before React re-renders. So the host exposes `useCaptureOutgoing()` and the triggers call it. If nobody captures, the transition degrades to `coldLoad` rather than breaking.

- [ ] **Step 1: Write the failing test**

Create `frontend/packages/solution/src/transitions/ScreenTransition.test.tsx`:

```tsx
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { ScreenTransition, useCaptureOutgoing } from './ScreenTransition';

/**
 * `src/test/setup.ts` stubs `matchMedia` with `matches: true`, so the whole
 * suite takes the reduced-motion path unless a test says otherwise. That is the
 * right default everywhere else, and exactly wrong here — a test of the
 * full-motion host that forgets to override it passes for the wrong reason.
 */
function useFullMotion() {
  const original = globalThis.matchMedia;
  beforeEach(() => {
    globalThis.matchMedia = ((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    })) as typeof globalThis.matchMedia;
  });
  afterEach(() => {
    globalThis.matchMedia = original;
  });
}

function Harness({ initial = 'a' }: { initial?: string }) {
  const [key, setKey] = useState(initial);
  return (
    <ScreenTransition transitionKey={key} variant="forward">
      <Pane label={key} onGo={setKey} />
    </ScreenTransition>
  );
}

function Pane({
  label,
  onGo,
}: {
  label: string;
  onGo: (next: string) => void;
}) {
  const capture = useCaptureOutgoing();
  return (
    <div>
      <h1>Screen {label}</h1>
      <button
        type="button"
        onClick={() => {
          capture();
          onGo(label === 'a' ? 'b' : 'a');
        }}
      >
        Go
      </button>
    </div>
  );
}

describe('ScreenTransition', () => {
  useFullMotion();

  it('renders its children', () => {
    render(<Harness />);

    expect(
      screen.getByRole('heading', { name: 'Screen a' }),
    ).toBeInTheDocument();
  });

  it('keeps a snapshot of the outgoing screen on the page during the move', () => {
    const { container } = render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    // Two panes: the live one and the frozen clone.
    expect(container.querySelectorAll('[data-cdx-pane]')).toHaveLength(2);
    expect(container.querySelector('[data-cdx-pane="outgoing"]')?.textContent)
      .toContain('Screen a');
    expect(container.querySelector('[data-cdx-pane="incoming"]')?.textContent)
      .toContain('Screen b');
  });

  it('hides the outgoing snapshot from assistive technology and from hit-testing', () => {
    const { container } = render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    const clone = container.querySelector('[data-cdx-pane="outgoing"]');
    expect(clone).toHaveAttribute('aria-hidden', 'true');
    expect(clone).toHaveAttribute('inert');
  });

  it('exposes exactly one heading to assistive technology mid-flight', () => {
    render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    // The clone's heading is inside an aria-hidden subtree, so the
    // accessibility tree sees only the incoming one.
    expect(screen.getAllByRole('heading')).toHaveLength(1);
    expect(screen.getByRole('heading')).toHaveTextContent('Screen b');
  });

  it('leaves exactly one snapshot when interrupted mid-transition', () => {
    const { container } = render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });
    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    expect(
      container.querySelectorAll('[data-cdx-pane="outgoing"]'),
    ).toHaveLength(1);
  });

  it('removes the snapshot once the move completes', async () => {
    vi.useFakeTimers();
    const { container } = render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });
    expect(
      container.querySelector('[data-cdx-pane="outgoing"]'),
    ).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1_000);
    });

    expect(container.querySelector('[data-cdx-pane="outgoing"]')).toBeNull();
    vi.useRealTimers();
  });

  it('does not steal focus while the screens are still mid-flight', () => {
    render(<Harness />);
    const trigger = screen.getByRole('button', { name: 'Go' });

    act(() => {
      trigger.click();
    });

    // The incoming heading must not grab focus until it is readable.
    expect(screen.getByRole('heading')).not.toHaveFocus();
  });

  it('moves focus to the incoming heading once the move completes', async () => {
    vi.useFakeTimers();
    render(<Harness />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1_000);
    });

    expect(screen.getByRole('heading', { name: 'Screen b' })).toHaveFocus();
    vi.useRealTimers();
  });

  it('does not take focus on the very first render', () => {
    render(<Harness />);

    // A cold load must not hijack focus away from the document.
    expect(screen.getByRole('heading')).not.toHaveFocus();
  });

  it('runs with no capture at all, degrading to an incoming-only move', () => {
    function NoCapture() {
      const [key, setKey] = useState('a');
      return (
        <ScreenTransition transitionKey={key} variant="forward">
          <div>
            <h1>Screen {key}</h1>
            <button
              type="button"
              onClick={() => {
                setKey('b');
              }}
            >
              Go
            </button>
          </div>
        </ScreenTransition>
      );
    }

    const { container } = render(<NoCapture />);

    act(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    });

    expect(container.querySelector('[data-cdx-pane="outgoing"]')).toBeNull();
    expect(screen.getByRole('heading')).toHaveTextContent('Screen b');
  });
});

describe('useCaptureOutgoing', () => {
  it('is a no-op outside a ScreenTransition, so screens stay storyable', () => {
    function Bare() {
      const capture = useCaptureOutgoing();
      return (
        <button
          type="button"
          onClick={() => {
            capture();
          }}
        >
          Go
        </button>
      );
    }

    render(<Bare />);

    expect(() => {
      screen.getByRole('button', { name: 'Go' }).click();
    }).not.toThrow();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w @cinedex/solution -- ScreenTransition`

Expected: FAIL — `Failed to resolve import "./ScreenTransition"`.

- [ ] **Step 3: Write minimal implementation**

Create `frontend/packages/solution/src/transitions/ScreenTransition.tsx`:

```tsx
import {
  createContext,
  useCallback,
  useContext,
  useLayoutEffect,
  useRef,
  type ReactNode,
} from 'react';
import type { gsap } from 'gsap';
import {
  buildRackFocusTimeline,
  PANE_STYLE,
  type TransitionVariant,
} from './rackFocus';

/**
 * Plays the rack-focus transition when `transitionKey` changes.
 *
 * The outgoing screen is a **clone of its own DOM**, not a React subtree kept
 * alive. Keeping the subtree alive is the obvious approach and it does not work
 * here: whatever renders the screens (a router `Outlet`, a step-switching
 * screen) re-renders the "outgoing" copy against the new state, so you end up
 * cross-fading a screen with itself. A clone is frozen by construction.
 *
 * The clone has to be taken **before** React commits the swap. React runs layout
 * effects after the DOM is already updated, and reading a ref during render is
 * not safe with React Compiler enabled — so the capture is imperative, driven by
 * the event that causes the change. `useCaptureOutgoing` is the hook the
 * triggers call; with no capture, the move degrades to incoming-only rather
 * than breaking.
 */

const CaptureContext = createContext<(() => void) | null>(null);

/**
 * Returns a function that freezes the current screen for the transition that is
 * about to start. Call it immediately before whatever changes the key — a
 * router navigation, a `setState` in a submit handler.
 *
 * Outside a `ScreenTransition` this is a no-op, so screens using it still render
 * bare in Storybook and in tests with no provider — the same shape as
 * `useLinkComponent`.
 */
export function useCaptureOutgoing(): () => void {
  const capture = useContext(CaptureContext);
  return useCallback(() => {
    capture?.();
  }, [capture]);
}

export interface ScreenTransitionProps {
  /** Identifies the current screen. A change plays the transition. */
  transitionKey: string;
  variant: TransitionVariant;
  children: ReactNode;
}

const STAGE_STYLE = { position: 'relative' } as const;

const OVERLAY_STYLE = {
  position: 'absolute',
  inset: 0,
  pointerEvents: 'none',
} as const;

export function ScreenTransition({
  transitionKey,
  variant,
  children,
}: ScreenTransitionProps) {
  const stageRef = useRef<HTMLDivElement>(null);
  const liveRef = useRef<HTMLDivElement>(null);
  const renderedKey = useRef<string | null>(null);
  const cloneRef = useRef<HTMLElement | null>(null);
  const timelineRef = useRef<gsap.core.Timeline | null>(null);

  const dropClone = useCallback(() => {
    cloneRef.current?.remove();
    cloneRef.current = null;
  }, []);

  const capture = useCallback(() => {
    const live = liveRef.current;
    const stage = stageRef.current;
    if (!live || !stage) {
      return;
    }

    // An interrupted move drops its own clone first, so there is never more
    // than one on the page.
    timelineRef.current?.kill();
    timelineRef.current = null;
    dropClone();

    const clone = live.cloneNode(true) as HTMLElement;
    clone.setAttribute('data-cdx-pane', 'outgoing');
    clone.setAttribute('aria-hidden', 'true');
    clone.setAttribute('inert', '');
    Object.assign(clone.style, OVERLAY_STYLE, PANE_STYLE);
    stage.append(clone);
    cloneRef.current = clone;
  }, [dropClone]);

  useLayoutEffect(() => {
    if (renderedKey.current === transitionKey) {
      return;
    }
    renderedKey.current = transitionKey;

    const incoming = liveRef.current;
    if (!incoming) {
      return;
    }

    const outgoing = cloneRef.current;
    const timeline = buildRackFocusTimeline(outgoing, incoming, {
      // With nothing captured there is no screen to leave, which is exactly
      // what `coldLoad` describes.
      variant: outgoing ? variant : 'coldLoad',
      reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)')
        .matches,
    });
    timelineRef.current = timeline;

    timeline.eventCallback('onComplete', () => {
      dropClone();
      timelineRef.current = null;

      // Focus follows the move, but only on a real transition — taking focus on
      // the first render would hijack a cold page load. And only on completion:
      // moving it at the start points a screen reader at a heading that is
      // still blurred out and unreadable.
      if (!outgoing) {
        return;
      }
      const heading = incoming.querySelector('h1');
      if (heading instanceof HTMLElement) {
        heading.setAttribute('tabindex', '-1');
        heading.focus({ preventScroll: true });
      }
    });
    timeline.play();

    return () => {
      // `kill` rather than `revert`, for the same reason `useMarkTimeline` does:
      // rewinding every property on a node React is about to drop is pointless,
      // and on a StrictMode remount it would fight the fresh timeline.
      timeline.kill();
    };
  }, [transitionKey, variant, dropClone]);

  useLayoutEffect(() => dropClone, [dropClone]);

  return (
    <CaptureContext.Provider value={capture}>
      <div ref={stageRef} style={STAGE_STYLE}>
        <div
          ref={liveRef}
          data-cdx-pane="incoming"
          style={PANE_STYLE}
        >
          {children}
        </div>
      </div>
    </CaptureContext.Provider>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w @cinedex/solution -- ScreenTransition`

Expected: PASS — 11 tests.

> **If the two fake-timer tests hang or time out**, GSAP's ticker is not
> advancing under them. GSAP drives itself with `requestAnimationFrame` when it
> exists and falls back to `setTimeout` when it does not — and this suite's jsdom
> has no rAF, which is why fake timers should work. If they do not, switch those
> two tests to real timers and `await waitFor(..., { timeout: 1500 })` rather
> than deleting them: completion behaviour and focus handoff are exactly what
> this layer exists to cover.

- [ ] **Step 5: Export from the barrel**

In `frontend/packages/solution/src/index.ts`, add after the `SolutionProvider` exports:

```ts
export { ScreenTransition, useCaptureOutgoing } from './transitions/ScreenTransition';
export type { ScreenTransitionProps } from './transitions/ScreenTransition';
export { buildRackFocusTimeline, PANE_STYLE } from './transitions/rackFocus';
export type {
  RackFocusOptions,
  TransitionVariant,
} from './transitions/rackFocus';
export { variantForEdge } from './transitions/authEdges';
```

- [ ] **Step 6: Run the whole package suite**

Run: `npm run test:run -w @cinedex/solution`

Expected: PASS — every existing test still green.

- [ ] **Step 7: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/packages/solution/src/transitions/ScreenTransition.tsx frontend/packages/solution/src/transitions/ScreenTransition.test.tsx frontend/packages/solution/src/index.ts
git commit -m "$(cat <<'EOF'
feat(solution): add the ScreenTransition clone host

Freezes the outgoing screen as a DOM clone rather than keeping its React
subtree alive, which would re-render against the new state and cross-fade
a screen with itself.

The clone is captured imperatively via useCaptureOutgoing, because React
commits DOM before layout effects run and React Compiler makes a
render-phase ref read unsafe. The clone is inert and aria-hidden, so the
340ms overlap does not expose two headings.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Transition the in-screen forgot-password step

**Files:**
- Modify: `frontend/packages/solution/src/screens/ForgotPasswordScreen.tsx`
- Test: `frontend/packages/solution/src/screens/ForgotPasswordScreen.test.tsx`

**Interfaces:**
- Consumes: `ScreenTransition`, `useCaptureOutgoing` from Task 4.
- Produces: no new exports. `ForgotPasswordScreen`'s props are unchanged.

**Why this task exists:** `request → sent` and `sent → request` are the only edges reachable by clicking today that exercise both a forward and a backward move, and neither is a navigation — they are `useState` inside one component. Without this task the router wiring in Task 6 animates almost nothing.

- [ ] **Step 1: Write the failing test**

Append to `frontend/packages/solution/src/screens/ForgotPasswordScreen.test.tsx`:

```tsx
describe('ForgotPasswordScreen transitions', () => {
  it('freezes the request form when the reset link is sent', async () => {
    const user = userEvent.setup();
    const { container } = render(<ForgotPasswordScreen />);

    await user.type(
      screen.getByLabelText(/email/i),
      'felipe@cinedex.online',
    );
    await user.click(screen.getByRole('button', { name: /send reset link/i }));

    expect(
      screen.getByRole('heading', { name: 'Check your inbox' }),
    ).toBeInTheDocument();
    // The outgoing snapshot is present and hidden from assistive technology.
    const clone = container.querySelector('[data-cdx-pane="outgoing"]');
    expect(clone).toHaveAttribute('aria-hidden', 'true');
  });

  it('runs the backward variant when starting over', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(
      screen.getByLabelText(/email/i),
      'felipe@cinedex.online',
    );
    await user.click(screen.getByRole('button', { name: /send reset link/i }));
    await user.click(screen.getByRole('button', { name: /start over/i }));

    expect(
      screen.getByRole('heading', { name: 'Reset your password' }),
    ).toBeInTheDocument();
  });
});
```

> If the existing test file does not already import `userEvent`, add
> `import userEvent from '@testing-library/user-event';` at the top. Check the
> existing imports before adding a duplicate.
>
> The exact accessible names above (`Check your inbox`, `Reset your password`,
> `Send reset link`, `Start over`) must match the screen's real copy. Read
> `ForgotPasswordScreen.tsx` and correct them if they differ — do not change the
> screen's copy to match the test.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w @cinedex/solution -- ForgotPasswordScreen`

Expected: FAIL — the `[data-cdx-pane="outgoing"]` query returns `null`.

- [ ] **Step 3: Wrap the screen and capture before each step change**

In `frontend/packages/solution/src/screens/ForgotPasswordScreen.tsx`:

1. Add the imports:

```tsx
import { ScreenTransition, useCaptureOutgoing } from '../transitions/ScreenTransition';
```

2. Rename the existing component to `ForgotPasswordScreenBody` and keep its
   whole body unchanged except for the two step changes below. Add a new
   exported wrapper:

```tsx
export function ForgotPasswordScreen(props: ForgotPasswordScreenProps) {
  const [step, setStep] = useState<'request' | 'sent'>('request');

  return (
    <ScreenTransition
      transitionKey={`/forgot-password#${step}`}
      // Sending the link advances; starting over pulls back out.
      variant={step === 'sent' ? 'forward' : 'back'}
    >
      <ForgotPasswordScreenBody {...props} step={step} onStepChange={setStep} />
    </ScreenTransition>
  );
}
```

3. `ForgotPasswordScreenBody` takes `step` and `onStepChange` instead of owning
   the state. Inside it, capture before every step change:

```tsx
const capture = useCaptureOutgoing();
```

Replace the submit handler's `setStep('sent')` with:

```tsx
capture();
onStepChange('sent');
```

And the "Start over" handler's `setStep('request')` with:

```tsx
capture();
onStepChange('request');
```

> `resendIn` and its `useEffect` stay in the body component, keyed off the `step`
> prop exactly as they were keyed off the state. Do not move the countdown into
> the wrapper — the clone must capture the countdown as it actually read at the
> moment of the change, which is the whole reason capture is imperative.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w @cinedex/solution -- ForgotPasswordScreen`

Expected: PASS — the existing tests plus 2 new ones.

- [ ] **Step 5: Run the whole package suite**

Run: `npm run test:run -w @cinedex/solution`

Expected: PASS.

- [ ] **Step 6: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/packages/solution/src/screens/ForgotPasswordScreen.tsx frontend/packages/solution/src/screens/ForgotPasswordScreen.test.tsx
git commit -m "$(cat <<'EOF'
feat(solution): transition the forgot-password step change

request -> sent and sent -> request are the only forward and backward
edges reachable by clicking today, and neither is a navigation. Splitting
the screen into a wrapper that owns the step and a body that renders it
lets the transition host sit between them.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Router wiring

**Files:**
- Modify: `frontend/apps/cinedex-app/src/routes/__root.tsx`
- Test: `frontend/apps/cinedex-app/src/transitions-routing.test.tsx` (create)

**Interfaces:**
- Consumes: `ScreenTransition`, `useCaptureOutgoing`, `variantForEdge` from Tasks 3–4.
- Produces: no exports. `__root.tsx` gains a local `RouterScreenTransition`.

**Direction on Back/Forward:** every `popstate` is treated as **backward**. That is right for the Back button and wrong for the Forward button, which is a deliberate simplification: reading a true history delta needs TanStack Router's internal history index, which is not public API, and forward-button use inside an auth flow is vanishingly rare. This resolves the corresponding risk row in the spec.

- [ ] **Step 1: Write the failing test**

Create `frontend/apps/cinedex-app/src/transitions-routing.test.tsx`:

```tsx
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {
  createMemoryHistory,
  createRouter,
  RouterProvider,
} from '@tanstack/react-router';
import { routeTree } from './routeTree.gen';

function renderAt(path: string) {
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [path] }),
  });

  return render(<RouterProvider router={router} />);
}

describe('auth transitions', () => {
  it('wraps the routed screen in a transition stage', async () => {
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    expect(
      container.querySelector('[data-cdx-pane="incoming"]'),
    ).toBeInTheDocument();
  });

  it('freezes the outgoing screen when a screen link navigates', async () => {
    const user = userEvent.setup();
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    await user.click(screen.getByRole('link', { name: /create one/i }));

    await screen.findByRole('heading', { name: 'Create your account' });
    expect(
      container.querySelector('[data-cdx-pane="outgoing"]'),
    ).toBeInTheDocument();
  });

  it('exposes one heading to assistive technology across the move', async () => {
    const user = userEvent.setup();
    renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    await user.click(screen.getByRole('link', { name: /create one/i }));

    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
  });
});
```

> The link text `create one` and the destination heading `Create your account`
> must match the real copy in `SignInScreen` and `CreateAccountScreen`. Read
> both files and correct the test if they differ — do not change the screens.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -w cinedex-app -- transitions-routing`

Expected: FAIL — `[data-cdx-pane="incoming"]` is `null`.

- [ ] **Step 3: Wire the router**

Replace `frontend/apps/cinedex-app/src/routes/__root.tsx` entirely:

```tsx
import { useEffect, useRef, useState } from 'react';
import {
  createRootRoute,
  Link,
  Outlet,
  useRouterState,
} from '@tanstack/react-router';
import {
  ScreenTransition,
  SolutionProvider,
  useCaptureOutgoing,
  variantForEdge,
  type SolutionLinkProps,
} from '@cinedex/solution';
import { Toaster } from 'sonner';

/**
 * Adapts `@cinedex/solution`'s router-agnostic link contract to TanStack Router.
 *
 * This function is the entire coupling between the screen library and the
 * router, and the cast is where their two type systems meet: `@cinedex/solution`
 * deals in plain path strings so it can stay router-free and storyable, while
 * TanStack narrows `to` to the union of generated route paths. The paths the
 * screens use are real routes — `login-routing.test.tsx` is what keeps that
 * honest.
 *
 * It is also where the auth transition captures the outgoing screen. Every
 * in-app navigation goes through this one component, and a click handler runs
 * before React re-renders — which is the only moment the outgoing DOM still
 * exists to be cloned.
 */
function RouterLink({ to, search, onClick, ...rest }: SolutionLinkProps) {
  const capture = useCaptureOutgoing();

  return (
    <Link
      to={to as never}
      search={search as never}
      onClick={(event) => {
        capture();
        onClick?.(event as never);
      }}
      {...rest}
    />
  );
}

/**
 * Plays the rack-focus transition on every routed screen change.
 *
 * The key is pathname **plus search**, because the lockout state is
 * `/login?state=locked` — a search-param change that renders a materially
 * different screen.
 *
 * Every `popstate` is treated as backward. That is right for the Back button and
 * wrong for Forward, which is deliberate: a true history delta needs TanStack's
 * internal history index, which is not public API, and forward-button use inside
 * an auth flow is vanishingly rare.
 */
function RouterScreenTransition() {
  const location = useRouterState({
    select: (state) => state.location.pathname + state.location.searchStr,
  });
  const previous = useRef<string | null>(null);
  const [wentBack, setWentBack] = useState(false);

  useEffect(() => {
    const onPopState = () => {
      setWentBack(true);
    };
    window.addEventListener('popstate', onPopState);
    return () => {
      window.removeEventListener('popstate', onPopState);
    };
  }, []);

  const variant = variantForEdge(previous.current, location, wentBack);
  previous.current = location;

  return (
    <ScreenTransition transitionKey={location} variant={variant}>
      <Outlet />
    </ScreenTransition>
  );
}

export const Route = createRootRoute({
  component: () => (
    <SolutionProvider linkComponent={RouterLink}>
      <RouterScreenTransition />
      <Toaster />
    </SolutionProvider>
  ),
});
```

> Two things to verify against the installed router version before assuming the
> code is wrong:
>
> - `state.location.searchStr` is the serialised query string. If it does not
>   exist, use `JSON.stringify(state.location.search)` instead — the key only has
>   to be stable and distinct, not pretty.
> - `SolutionLinkProps` may not declare `onClick`. If TypeScript rejects it, add
>   `onClick?: MouseEventHandler<HTMLAnchorElement>` to `SolutionLinkProps` in
>   `packages/solution/src/link/linkTypes.ts` and export the type — that is a
>   legitimate widening of the contract, not a workaround.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test:run -w cinedex-app -- transitions-routing`

Expected: PASS — 3 tests.

- [ ] **Step 5: Run the existing routing test, which must not regress**

Run: `npm run test:run -w cinedex-app`

Expected: PASS — `login-routing.test.tsx` still green. If `/login/verify` now
fails to find its heading, `ScreenTransition` is swallowing the `Outlet` —
check that `children` is rendered unconditionally.

- [ ] **Step 6: Verify in a real browser**

Start the dev server and step through the flow by hand. This is the only check
that catches jank, and it is what the brand animations get too.

```bash
cd frontend && npm run start
```

Confirm at `http://localhost:5173`:
- `/login` fades in on cold load with no outgoing screen
- "Create one" runs the full 820ms forward move
- "Forgot?" → "Send reset link" runs forward, "Start over" runs backward
- Browser Back runs backward
- With OS "reduce motion" on, every move is a fast cross-fade with no blur

- [ ] **Step 7: Format, lint, build, commit**

```bash
cd frontend && npm run format && npm run lint && npm run build
```

```bash
git add frontend/apps/cinedex-app/src/routes/__root.tsx frontend/apps/cinedex-app/src/transitions-routing.test.tsx
git commit -m "$(cat <<'EOF'
feat(app): play the rack-focus transition on auth navigation

RouterLink was already the single coupling point between the screen
library and the router, which makes its click handler the one moment the
outgoing DOM still exists to be cloned. Keys on pathname plus search, so
the lockout state transitions like the distinct screen it is.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: The Storybook review rail

**Files:**
- Create: `frontend/apps/storybook/src/solution/AuthTransitions.stories.tsx`

**Interfaces:**
- Consumes: `ScreenTransition`, `useCaptureOutgoing`, `variantForEdge`, and the screen components, all from `@cinedex/solution`.
- Produces: no exports beyond the CSF meta and stories.

**Why:** this is the design-review surface — the place a human sees the 820ms and can compare it against `Auth Transitions.dc.html`. It also covers the three variants no route reaches.

- [ ] **Step 1: Write the story**

Create `frontend/apps/storybook/src/solution/AuthTransitions.stories.tsx`:

```tsx
import { useCallback, useEffect, useRef, useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  ScreenTransition,
  SignInScreen,
  CreateAccountScreen,
  ForgotPasswordScreen,
  ResetPasswordScreen,
  SignedOutScreen,
  TwoFactorScreen,
  useCaptureOutgoing,
  type TransitionVariant,
} from '@cinedex/solution';

interface RailScreen {
  id: string;
  label: string;
  variant: TransitionVariant;
  render: () => React.ReactNode;
}

/**
 * The design's ten screens. Two of them — "Verify your email" and "Account
 * ready" — have no component in `@cinedex/solution`: the source design marks
 * their copy as an unreviewed draft, so they appear here as explicit
 * placeholders rather than as shipped wording.
 */
const SCREENS: RailScreen[] = [
  {
    id: '01',
    label: '01 Sign in',
    variant: 'forward',
    render: () => <SignInScreen />,
  },
  {
    id: '02',
    label: '02 Two-factor code',
    variant: 'forward',
    render: () => <TwoFactorScreen />,
  },
  {
    id: '03',
    label: '03 Too many attempts',
    variant: 'lockout',
    render: () => <SignInScreen locked />,
  },
  {
    id: '04',
    label: '04 Create account',
    variant: 'forward',
    render: () => <CreateAccountScreen />,
  },
  {
    id: '05',
    label: '05 Verify your email',
    variant: 'forward',
    render: () => <NotBuilt name="Verify your email" />,
  },
  {
    id: '06',
    label: '06 Account ready',
    variant: 'accountReady',
    render: () => <NotBuilt name="Account ready" />,
  },
  {
    id: '07',
    label: '07 Reset your password',
    variant: 'forward',
    render: () => <ForgotPasswordScreen />,
  },
  {
    id: '08',
    label: '08 Check your inbox',
    variant: 'forward',
    render: () => <ForgotPasswordScreen />,
  },
  {
    id: '09',
    label: '09 Set a new password',
    variant: 'coldLoad',
    render: () => <ResetPasswordScreen />,
  },
  {
    id: '10',
    label: '10 Signed out',
    variant: 'back',
    render: () => <SignedOutScreen />,
  },
];

function NotBuilt({ name }: { name: string }) {
  return (
    <div className="flex min-h-svh items-center justify-center bg-bg p-6">
      <div className="w-full max-w-[420px] rounded-md border border-accent-border bg-accent-bg p-6">
        <p className="m-0 font-mono text-label font-semibold tracking-eyebrow text-accent uppercase">
          Not built
        </p>
        <h1 className="mt-2 mb-0 text-title font-bold tracking-tight text-text-h">
          {name}
        </h1>
        <p className="mt-2 mb-0 text-body text-text">
          No component upstream yet. The source design marks this screen&rsquo;s
          copy as a draft for review, so it is deliberately not shipped in{' '}
          <code>@cinedex/solution</code>.
        </p>
      </div>
    </div>
  );
}

/**
 * Hands the rail's controls a capture function from inside the provider.
 *
 * `useCaptureOutgoing` only works inside a `ScreenTransition`, but the chips
 * have to render *outside* the animated pane or they would blur out along with
 * the screen. This renders nothing and exists purely to cross that boundary.
 */
function CaptureBridge({ onReady }: { onReady: (capture: () => void) => void }) {
  const capture = useCaptureOutgoing();

  useEffect(() => {
    onReady(capture);
  }, [capture, onReady]);

  return null;
}

function Rail() {
  const [index, setIndex] = useState(0);
  const captureRef = useRef<() => void>(() => undefined);
  const current = SCREENS[index];

  const onReady = useCallback((capture: () => void) => {
    captureRef.current = capture;
  }, []);

  const go = (next: number) => {
    captureRef.current();
    setIndex((next + SCREENS.length) % SCREENS.length);
  };

  return (
    <div className="flex flex-col gap-4 p-6">
      <Chips index={index} onGo={go} />
      <div className="rounded-md border border-border">
        <ScreenTransition transitionKey={current.id} variant={current.variant}>
          <CaptureBridge onReady={onReady} />
          {current.render()}
        </ScreenTransition>
      </div>
    </div>
  );
}

function Chips({
  index,
  onGo,
}: {
  index: number;
  onGo: (next: number) => void;
}) {
  const go = onGo;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-1.5">
        {SCREENS.map((screen, i) => (
          <button
            key={screen.id}
            type="button"
            onClick={() => {
              go(i);
            }}
            className={
              i === index
                ? 'rounded-sm border border-text-h bg-text-h px-3 py-1.5 font-mono text-label text-bg uppercase'
                : 'rounded-sm border border-border bg-bg px-3 py-1.5 font-mono text-label text-text uppercase'
            }
          >
            {screen.label}
          </button>
        ))}
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => {
            go(index - 1);
          }}
          className="rounded-sm border border-border px-4 py-2 font-mono text-label text-text uppercase"
        >
          &larr; Prev
        </button>
        <button
          type="button"
          onClick={() => {
            go(index + 1);
          }}
          className="rounded-sm border border-text-h bg-text-h px-4 py-2 font-mono text-label text-bg uppercase"
        >
          Next &rarr;
        </button>
        <span className="ml-2 font-mono text-footnote text-text">
          {SCREENS[index].label} &middot; {SCREENS[index].variant}
        </span>
      </div>
    </div>
  );
}

const meta = {
  title: 'Solution/AuthTransitions',
  component: Rail,
  parameters: { layout: 'fullscreen' },
  tags: ['autodocs'],
} satisfies Meta<typeof Rail>;

export default meta;

type Story = StoryObj<typeof meta>;

export const RackFocus: Story = {};
```

> The `CaptureBridge` indirection is load-bearing, not incidental. The chips have
> to sit outside the animated pane or they blur out with the screen, but
> `useCaptureOutgoing` only resolves inside the provider. If you flatten it away,
> the capture silently returns the no-op and every move degrades to a plain
> `coldLoad` fade-in — a green build with the feature switched off.
>
> The tell when reviewing: click a chip and watch the *outgoing* screen. If it
> blurs and shrinks away, capture is working. If it simply vanishes and the new
> one fades in, it is not.

- [ ] **Step 2: Run Storybook and review by eye**

```bash
cd frontend && npm run storybook
```

At `http://localhost:9001`, open **Solution → AuthTransitions → RackFocus** and confirm against the design:

- Stepping `01 → 02` runs the full 820ms move with visible blur on both screens
- `03 Too many attempts` moves without any scale change
- `06 Account ready` sits still for a beat before moving
- `10 Signed out` recedes rather than advancing
- Toggling the theme toolbar does not break the panes

- [ ] **Step 3: Build Storybook, which is part of CI**

Run: `npm run build -w @cinedex/storybook`

Expected: typecheck passes and static output is produced.

- [ ] **Step 4: Format, lint, commit**

```bash
cd frontend && npm run format && npm run lint
```

```bash
git add frontend/apps/storybook/src/solution/AuthTransitions.stories.tsx
git commit -m "$(cat <<'EOF'
docs(storybook): add the auth transitions review rail

Ports the source design's screen rail so the 820ms move can be reviewed
next to the components. Covers the three variants no route reaches, and
shows the two screens with no component as explicit placeholders rather
than shipping their draft copy.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Changelog and documentation

**Files:**
- Modify: `CHANGELOG.md` (repo root — **never** `backend/CHANGELOG.md`)
- Modify: `frontend/packages/solution/CLAUDE.md`

- [ ] **Step 1: Add the changelog entry**

Use the `changelog-entry` skill. It goes under `## [Unreleased]`, in the
`### Added` subsection, Keep a Changelog format.

> Editing `backend/CHANGELOG.md` breaks CI — it is a build-managed copy and CI
> fails if the two files differ.

- [ ] **Step 2: Document the transitions in the package's CLAUDE.md**

Add to `frontend/packages/solution/CLAUDE.md`, in the Layout tree and the Notes
section. The notes worth recording, because each is a trap someone would
otherwise rediscover:

- Animated values are CSS custom properties, not `style.filter`/`style.transform`, because jsdom can silently drop `filter`.
- The outgoing screen is a DOM clone, and the capture is imperative because React commits DOM before layout effects and React Compiler makes a render-phase ref read unsafe.
- `useCaptureOutgoing` is a no-op with no provider, the same shape as `useLinkComponent`, so screens stay storyable.
- Easing is a hand-rolled bezier solver, not GSAP `powerN` — cross-reference the existing `powerN` warning.
- The `matchMedia` stub means every test defaults to reduced motion; full-motion tests must override it.

- [ ] **Step 3: Run the full workspace check, exactly as CI does**

```bash
cd frontend && npm run lint && npm run format:check && npm run build && npm run coverage
```

Expected: all four pass.

- [ ] **Step 4: Run the diagram guard**

Run: `node scripts/check-diagrams.mjs`

Expected: `ok - N mermaid diagrams across M files, no ASCII box art.`

- [ ] **Step 5: Commit**

```bash
git add CHANGELOG.md frontend/packages/solution/CLAUDE.md
git commit -m "$(cat <<'EOF'
docs: record the auth transitions in the changelog and package notes

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
)"
```

---

## Verification checklist

Before calling this done, confirm each of these has actually been run and its
output seen — not assumed:

- [ ] `npm run test:run` from `frontend/` — every workspace green
- [ ] `npm run lint` and `npm run format:check` — clean
- [ ] `npm run build` — including the Storybook build
- [ ] `npm run coverage` — no drop in `@cinedex/solution`
- [ ] `node scripts/check-diagrams.mjs` — passes
- [ ] The browser walkthrough in Task 6 Step 6, including the reduced-motion pass
- [ ] The Storybook review in Task 7 Step 2

## Known limitations, recorded deliberately

- **Forward-button navigation reads as backward.** See Task 6. A true history
  delta needs TanStack Router internals.
- **The clone drops portals.** Auth screens have none today; a Radix popover
  anchored outside the card subtree would not appear in the outgoing snapshot.
- **`05 Verify email` and `06 Account ready` are placeholders.** Their copy is an
  unreviewed draft in the source design.
- **`accountReady` and the two-factor edges are unreachable in the app.** They
  are covered by unit tests and the Storybook rail until the backend lands.
