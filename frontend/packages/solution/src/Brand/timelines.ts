import { gsap } from 'gsap';
import { OPEN_HINGE_DEG } from './mark';

/**
 * The two logo sequences, as GSAP timelines built over a `MarkBody` root.
 *
 * Every tween writes SVG **attributes** (`attr:`) rather than GSAP's own
 * `rotation`/`scale`/`x` shorthands. That is a deliberate constraint, not an
 * oversight, and it buys three things at once:
 *
 *  1. **The verified geometry survives untouched.** GSAP's transform shorthands
 *     go through CSS `transform` with a `transform-origin` resolved off
 *     `getBBox()`, which is a different coordinate model from the SVG
 *     `rotate(deg cx cy)` and `translate/scale/translate` strings `mark.ts` was
 *     empirically validated against. `attr:` interpolates the numbers *inside*
 *     those exact strings — constants stay pinned, so the six blades keep
 *     hinging in their own rotated frames and the iris opening stays
 *     `PIVOT_RADIUS · sin(phi)`. Measured end-to-end against the previous
 *     `lerp(a, b, eOut(t))` implementation, the worst divergence is 5e-5, which
 *     is entirely GSAP's 4-decimal attribute rounding.
 *  2. **Opacity stays on the attribute.** GSAP's plain `opacity` writes
 *     `style.opacity` and leaves any `opacity` attribute stale underneath it —
 *     which would leave the mark rendering correctly while
 *     `getAttribute('opacity')` reported the old value, i.e. tests that pass
 *     while the screen is wrong. `attr: { opacity }` keeps the two in agreement.
 *  3. **It runs in jsdom.** jsdom implements neither `requestAnimationFrame`
 *     nor `SVGElement.getBBox`, so GSAP's transform shorthands throw there.
 *     `attr:` tweens need neither, which is what lets the suite scrub these
 *     timelines frame by frame (`timelines.test.ts`) instead of only asserting
 *     the settled state.
 *
 * Timings are written as the original normalized windows fed through `win()`,
 * so every beat is still traceable to the sequence it was designed as.
 */

/** Both sequences run for the same 1.2s. */
const SEQUENCE_SECONDS = 1.2;

/**
 * GSAP's `powerN` naming is off by one from the usual quadratic/cubic
 * vocabulary: `power1` is quadratic, so **`power2` is the cubic** this mark was
 * designed against. `power2.out` reproduces the old `eOut` (`1-(1-t)³`) and
 * `power2.inOut` the old `eIO`, both to 0.0 error across the curve.
 *
 * `power3` is QUARTIC. It is the natural thing to reach for when porting a
 * "cubic ease-out" and it is wrong — it overshoots the intended curve by up to
 * 0.11 in the first half of every beat, which reads as the whole mark snapping
 * open early. Do not "fix" these to `power3`.
 */
const OUT = 'power2.out';
const IN_OUT = 'power2.inOut';

/**
 * Converts one of the sequence's original normalized `[a, b]` windows into the
 * `(duration, position)` pair GSAP wants, both in seconds. Keeping the call
 * sites in normalized form means the choreography still reads against the
 * storyboard it was designed as, rather than as hand-multiplied decimals.
 */
function win(a: number, b: number): { at: number; dur: number } {
  return { at: SEQUENCE_SECONDS * a, dur: SEQUENCE_SECONDS * (b - a) };
}

function clamp(v: number, min: number, max: number): number {
  return v < min ? min : v > max ? max : v;
}

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

/** The scale-about-centre sandwich `MarkBody`'s groups are animated with. */
function scaleAbout(s: number): string {
  return `translate(100,100) scale(${s.toFixed(4)}) translate(-100,-100)`;
}

/** SVG's own rotate-about-a-point form. */
function rotateAbout(deg: number): string {
  return `rotate(${deg.toFixed(3)} 100 100)`;
}

/** A blade's hinge, in its own already-rotated local frame. */
function hingeAt(deg: number): string {
  return `rotate(${deg.toFixed(3)})`;
}

function query(root: SVGSVGElement, selector: string): Element {
  const found = root.querySelector(selector);
  if (!found) {
    throw new Error(
      `Cinedex mark animation: expected "${selector}" in MarkBody`,
    );
  }
  return found;
}

function queryAll(root: SVGSVGElement, selector: string): Element[] {
  return [...root.querySelectorAll(selector)];
}

interface MarkElements {
  settle: SVGGElement;
  assembly: SVGGElement;
  lens: SVGGElement;
  spin: SVGGElement;
  blades: SVGGElement[];
  ghosts: SVGGElement;
  ghostArcs: SVGPathElement[];
  rings: SVGGElement;
  outer: SVGPathElement;
  inner: SVGPathElement;
  glintWrap: SVGGElement;
  glint: SVGRectElement;
}

function collect(root: SVGSVGElement): MarkElements {
  return {
    settle: query(root, '[data-settle]') as SVGGElement,
    assembly: query(root, '[data-assembly]') as SVGGElement,
    lens: query(root, '[data-lens]') as SVGGElement,
    spin: query(root, '[data-spin]') as SVGGElement,
    blades: queryAll(root, '[data-blade]') as SVGGElement[],
    ghosts: query(root, '[data-ghosts]') as SVGGElement,
    ghostArcs: queryAll(root, '[data-ghost]') as SVGPathElement[],
    rings: query(root, '[data-rings]') as SVGGElement,
    outer: query(root, '[data-outer]') as SVGPathElement,
    inner: query(root, '[data-inner]') as SVGPathElement,
    glintWrap: query(root, '[data-glintwrap]') as SVGGElement,
    glint: query(root, '[data-glint]') as SVGRectElement,
  };
}

/**
 * `pathLength={1000}` on the ring paths normalises the dash maths, so one
 * dasharray works for both arcs regardless of their real lengths.
 */
const RING_PATH_LENGTH = 1000;

/**
 * The glint's opacity (`sin(pi·p)`) and the ring group's (`clamp(4·p)`) are
 * *derived* from a progress value rather than tweened directly, so they ride on
 * a proxy that a timeline-level `onUpdate` flushes. Everything else in both
 * sequences is a plain one-to-one `attr` tween.
 *
 * Note `onUpdate` does **not** fire for `progress(0)`, so each builder calls
 * `flush` once itself to establish the opening frame.
 */
interface DerivedState {
  /** Ring draw-on progress, 0…1 — aperture only. */
  ring: number;
  /** The two overlapping ring-opacity ramps — focus rings only. */
  ringA: number;
  ringB: number;
  /** Glint sweep progress, 0…1. */
  glint: number;
}

function writeGlint(m: MarkElements, p: number): void {
  if (p <= 0 || p >= 1) {
    m.glintWrap.setAttribute('opacity', '0');
    return;
  }
  m.glintWrap.setAttribute('opacity', Math.sin(Math.PI * p).toFixed(3));
  m.glint.setAttribute('x', lerp(-230, 225, p).toFixed(1));
}

function writeRingDash(m: MarkElements, amount: number): void {
  const offset = (RING_PATH_LENGTH * (1 - amount)).toFixed(2);
  m.outer.setAttribute('stroke-dashoffset', offset);
  m.inner.setAttribute('stroke-dashoffset', offset);
}

/** Both ring paths need a dasharray before any dashoffset means anything. */
function primeRingDash(m: MarkElements): void {
  m.outer.setAttribute('stroke-dasharray', String(RING_PATH_LENGTH));
  m.inner.setAttribute('stroke-dasharray', String(RING_PATH_LENGTH));
}

export type MarkTimelineBuilder = (
  root: SVGSVGElement,
  wordmark: HTMLElement | null,
) => gsap.core.Timeline;

/**
 * "Lens aperture": opens from a closed iris. The blades hinge outward while the
 * assembly counter-rotates, the rings draw on around the gap, a glint crosses
 * the metal, and the wordmark fades in behind it.
 */
export const buildApertureTimeline: MarkTimelineBuilder = (root, wordmark) => {
  const m = collect(root);
  primeRingDash(m);

  const state: DerivedState = { ring: 0, ringA: 0, ringB: 0, glint: 0 };
  const flush = () => {
    writeRingDash(m, state.ring);
    m.rings.setAttribute('opacity', clamp(state.ring * 4, 0, 1).toFixed(3));
    writeGlint(m, state.glint);
  };
  flush();

  const hinge = win(0.06, 0.46);
  const spin = win(0.06, 0.56);
  const lens = win(0.18, 0.54);
  const rings = win(0.3, 0.64);
  const glint = win(0.5, 0.82);
  const settle = win(0.72, 1);

  const tl = gsap.timeline({ paused: true, onUpdate: flush });

  // The iris is the whole point of this sequence, so it leads.
  tl.fromTo(
    m.blades,
    { attr: { transform: hingeAt(0) } },
    { attr: { transform: hingeAt(OPEN_HINGE_DEG) }, duration: hinge.dur, ease: OUT },
    hinge.at,
  )
    .fromTo(
      m.spin,
      { attr: { transform: rotateAbout(-46) } },
      { attr: { transform: rotateAbout(0) }, duration: spin.dur, ease: OUT },
      spin.at,
    )
    // The assembly is already at rest here; only `data-lens` grows in.
    .set(m.assembly, { attr: { transform: scaleAbout(1), opacity: 1 } }, 0)
    .fromTo(
      m.lens,
      { attr: { opacity: 0, transform: scaleAbout(0.62) } },
      {
        attr: { opacity: 1, transform: scaleAbout(1) },
        duration: lens.dur,
        ease: OUT,
      },
      lens.at,
    )
    // Ghost arcs belong to the focus-rings sequence only.
    .set(m.ghosts, { attr: { opacity: 0 } }, 0)
    .set([m.outer, m.inner], { attr: { transform: rotateAbout(0) } }, 0)
    // Draw-on + group fade share one progress value — see `DerivedState`.
    .to(state, { ring: 1, duration: rings.dur, ease: OUT }, rings.at)
    .to(state, { glint: 1, duration: glint.dur, ease: IN_OUT }, glint.at)
    .fromTo(
      m.settle,
      { attr: { transform: scaleAbout(1.045) } },
      {
        attr: { transform: scaleAbout(1) },
        duration: settle.dur,
        ease: OUT,
      },
      settle.at,
    );

  if (wordmark) {
    tl.fromTo(
      wordmark,
      { opacity: 0 },
      { opacity: 1, duration: 0.3, ease: 'none' },
      0.7,
    );
  }

  return tl;
};

/**
 * "Focus rings": the inverse order. Ghost arcs surface out of darkness and
 * converge, the two rings rotate into register to form the "C", and only then
 * does the lens assembly grow in at the centre.
 */
export const buildFocusRingsTimeline: MarkTimelineBuilder = (
  root,
  wordmark,
) => {
  const m = collect(root);
  primeRingDash(m);

  const state: DerivedState = { ring: 0, ringA: 0, ringB: 0, glint: 0 };
  const flush = () => {
    m.rings.setAttribute(
      'opacity',
      clamp(state.ringA + state.ringB, 0, 1).toFixed(3),
    );
    writeGlint(m, state.glint);
  };
  flush();

  /** Everything that converges into register shares this one window. */
  const assemble = win(0.1, 0.52);
  const ringFadeA = win(0, 0.16);
  const ringFadeB = win(0.14, 0.54);
  const ringDraw = win(0.02, 0.48);
  const ghostIn = win(0, 0.1);
  const ghostOut = win(0.16, 0.5);
  const lens = win(0.48, 0.82);
  const hinge = win(0.5, 0.9);
  const glint = win(0.68, 1);
  const settle = win(0.82, 1);

  const tl = gsap.timeline({ paused: true, onUpdate: flush });
  tl.addLabel('assemble', assemble.at);

  // Ghost arcs rise out of black, hold, then dissolve as the real rings land.
  tl.fromTo(
    m.ghosts,
    { attr: { opacity: 0 } },
    { attr: { opacity: 1 }, duration: ghostIn.dur, ease: OUT },
    ghostIn.at,
  ).to(
    m.ghosts,
    { attr: { opacity: 0 }, duration: ghostOut.dur, ease: OUT },
    ghostOut.at,
  );

  const GHOST_START_DEG = [62, -78, 44];
  m.ghostArcs.forEach((arc, i) => {
    tl.fromTo(
      arc,
      { attr: { transform: rotateAbout(GHOST_START_DEG[i]) } },
      {
        attr: { transform: rotateAbout(0) },
        duration: assemble.dur,
        ease: OUT,
      },
      'assemble',
    );
  });

  // The two rings counter-rotate into register to close the "C".
  tl.fromTo(
    m.outer,
    { attr: { transform: rotateAbout(52) } },
    { attr: { transform: rotateAbout(0) }, duration: assemble.dur, ease: OUT },
    'assemble',
  )
    .fromTo(
      m.inner,
      { attr: { transform: rotateAbout(-64) } },
      {
        attr: { transform: rotateAbout(0) },
        duration: assemble.dur,
        ease: OUT,
      },
      'assemble',
    )
    .fromTo(
      m.spin,
      { attr: { transform: rotateAbout(-18) } },
      {
        attr: { transform: rotateAbout(0) },
        duration: assemble.dur,
        ease: OUT,
      },
      'assemble',
    )
    // The dash starts a third drawn rather than from nothing.
    .fromTo(
      [m.outer, m.inner],
      {
        attr: {
          'stroke-dashoffset': RING_PATH_LENGTH * (1 - 0.34),
        },
      },
      {
        attr: { 'stroke-dashoffset': 0 },
        duration: ringDraw.dur,
        ease: OUT,
      },
      ringDraw.at,
    )
    // Two overlapping ramps that sum to full opacity — see `DerivedState`.
    .to(state, { ringA: 0.42, duration: ringFadeA.dur, ease: OUT }, ringFadeA.at)
    .to(state, { ringB: 0.58, duration: ringFadeB.dur, ease: OUT }, ringFadeB.at)
    // Only now does the lens assembly grow in at the centre.
    .set(m.lens, { attr: { opacity: 1, transform: scaleAbout(1) } }, 0)
    .fromTo(
      m.assembly,
      { attr: { opacity: 0, transform: scaleAbout(0.5) } },
      {
        attr: { opacity: 1, transform: scaleAbout(1) },
        duration: lens.dur,
        ease: OUT,
      },
      lens.at,
    )
    // The iris is already most of the way open; it only finishes here.
    .fromTo(
      m.blades,
      { attr: { transform: hingeAt(21) } },
      {
        attr: { transform: hingeAt(OPEN_HINGE_DEG) },
        duration: hinge.dur,
        ease: OUT,
      },
      hinge.at,
    )
    .to(state, { glint: 1, duration: glint.dur, ease: IN_OUT }, glint.at)
    .fromTo(
      m.settle,
      { attr: { transform: scaleAbout(1.03) } },
      { attr: { transform: scaleAbout(1) }, duration: settle.dur, ease: OUT },
      settle.at,
    );

  if (wordmark) {
    tl.fromTo(
      wordmark,
      { opacity: 0 },
      { opacity: 1, duration: 0.3, ease: 'none' },
      0.84,
    );
  }

  return tl;
};
