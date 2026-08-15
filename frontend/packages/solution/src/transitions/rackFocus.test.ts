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
}

/** The spec's variant table, verbatim. Durations in seconds. */
const VARIANTS: [TransitionVariant, VariantExpectation][] = [
  ['forward', { duration: 0.82, outScale: 0.94, inScale: 1.05 }],
  ['back', { duration: 0.82, outScale: 1.05, inScale: 0.94 }],
  ['lockout', { duration: 0.52, outScale: 1, inScale: 1 }],
  ['accountReady', { duration: 1.02, outScale: 0.94, inScale: 1.05 }],
  ['coldLoad', { duration: 0.64, outScale: null, inScale: 1.05 }],
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
