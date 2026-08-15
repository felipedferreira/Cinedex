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
 * Every tween is a `fromTo`, which matters for more than symmetry: it hands GSAP
 * both endpoints, so nothing has to be read back out of `getComputedStyle` — and
 * jsdom returns an empty string for an unset custom property, which a plain
 * `to()` would parse as `0` and silently animate from the wrong place.
 *
 * The timeline is returned **paused**, exactly like `Brand/timelines.ts`, so the
 * caller owns whether it plays or is scrubbed.
 */

export type TransitionVariant =
  'forward' | 'back' | 'lockout' | 'accountReady' | 'coldLoad';

export interface RackFocusOptions {
  variant: TransitionVariant;
  /** Collapses every variant to the same 200ms opacity cross-fade. */
  reducedMotion?: boolean;
}

/**
 * The static inline style a pane must carry for the custom properties to mean
 * anything. Exported so `ScreenTransition` and the Storybook rail cannot drift
 * from what the timeline writes.
 *
 * `transformOrigin` is the design's own focal point — the iris closes and opens
 * about 50% 42%, slightly above centre, which is where an auth card's heading
 * sits rather than its geometric middle.
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

/** One pane's start or end state. */
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

/** The custom properties a pane state maps onto, in the units CSS needs. */
function vars(state: PaneState): Record<string, string> {
  return {
    '--cdx-pane-opacity': String(state.opacity),
    '--cdx-pane-blur': `${String(state.blur)}px`,
    '--cdx-pane-scale': String(state.scale),
  };
}

function addPane(
  timeline: gsap.core.Timeline,
  element: HTMLElement,
  spec: PaneSpec,
): void {
  timeline.fromTo(
    element,
    vars(spec.from),
    { ...vars(spec.to), duration: spec.dur, ease: spec.ease },
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
      gsap.set(outgoing, vars(RESTING));
    }
  }

  addPane(timeline, incoming, spec.in);

  return timeline;
}
