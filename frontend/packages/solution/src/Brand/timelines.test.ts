import { describe, expect, it } from 'vitest';
import { buildApertureTimeline, buildFocusRingsTimeline } from './timelines';
import { INNER_ARC_PATH, OUTER_ARC_PATH, PIVOT_RADIUS } from './mark';

/**
 * These scrub the timelines directly instead of rendering a component, which is
 * what the previous `requestAnimationFrame` implementation could not be tested
 * for at all: jsdom has no rAF, so the suite could only ever assert the settled
 * frame and every intermediate frame was verified by hand in a browser.
 *
 * A GSAP timeline built `paused` can be driven to any instant with
 * `progress(p)` and flushes its writes synchronously, with no ticker involved —
 * so the whole 1.2s sequence is now inspectable here.
 *
 * The expectations are deliberately written as the ORIGINAL pre-GSAP maths
 * (`lerp`/`eOut`/`seg`, transcribed from the implementation this replaced)
 * rather than as recorded output. That makes these regression tests against the
 * sequence as it was designed and empirically verified, not a snapshot of
 * whatever the port happens to produce.
 */

const clamp = (v: number, min: number, max: number) =>
  v < min ? min : v > max ? max : v;
const seg = (t: number, a: number, b: number) => clamp((t - a) / (b - a), 0, 1);
const eOut = (t: number) => 1 - (1 - t) ** 3;
const eIO = (t: number) =>
  t < 0.5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2;
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

/**
 * There are two write paths with different precision, and asserting against the
 * wrong one produces failures that have nothing to do with the animation.
 *
 * GSAP writes its own `attr` tweens at full precision (and numbers inside
 * transform *strings* at 4 decimals), so those compare with an ordinary
 * tolerance. The handful of values `timelines.ts` derives itself — the ring
 * group's opacity and the glint — go out through `toFixed(3)`/`toFixed(1)`, and
 * a value landing exactly on a rounding boundary (0.3675 -> "0.367") fails a
 * tolerance test for no good reason. Those compare against the same rounding.
 */
const asWritten = (value: number, decimals: number) =>
  Number(value.toFixed(decimals));

/**
 * Builds the mark's animatable skeleton. This mirrors `MarkBody`'s structure —
 * the nesting matters, since each blade hinges inside its own already-rotated
 * frame and the timelines address everything by `data-*` hook.
 */
function makeMark(): SVGSVGElement {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.innerHTML = `
    <g data-settle>
      <g data-assembly>
        <circle data-barrel cx="100" cy="100" r="${String(PIVOT_RADIUS)}"></circle>
        <g data-lens><circle cx="100" cy="100" r="22.5"></circle></g>
        <g data-irisclip>
          <g data-spin>
            ${[0, 60, 120, 180, 240, 300]
              .map(
                (a) =>
                  `<g transform="translate(100,100) rotate(${String(a)}) translate(${String(PIVOT_RADIUS)},0)">
                     <g data-blade transform="rotate(30)"><path d="M0 0L-150 0L-150 -150L0 -150Z"></path></g>
                   </g>`,
              )
              .join('')}
          </g>
        </g>
      </g>
      <g data-ghosts opacity="0">
        <path data-ghost d="${OUTER_ARC_PATH}"></path>
        <path data-ghost d="M144.35 47.14A69 69 0 1 0 144.35 152.86"></path>
        <path data-ghost d="${INNER_ARC_PATH}"></path>
      </g>
      <g data-rings>
        <path data-outer d="${OUTER_ARC_PATH}" pathLength="1000"></path>
        <path data-inner d="${INNER_ARC_PATH}" pathLength="1000"></path>
      </g>
      <g data-glintwrap opacity="0">
        <g transform="rotate(-18 100 100)">
          <rect data-glint x="-220" y="-70" width="66" height="340"></rect>
        </g>
      </g>
    </g>`;
  document.body.append(svg);
  return svg;
}

/** Pulls the first number out of a `rotate(...)`/`scale(...)` transform. */
function numberIn(el: Element | null, attribute: string): number {
  const raw = el?.getAttribute(attribute) ?? '';
  const match = /(-?[\d.]+)/.exec(raw.replace(/^[a-z]+\(/i, ''));
  return Number(match?.[1]);
}

function hingeOf(root: SVGSVGElement): number {
  const blade = root.querySelector('[data-blade]');
  return Number(/rotate\(([-\d.]+)\)/.exec(blade?.getAttribute('transform') ?? '')?.[1]);
}

function opacityOf(root: SVGSVGElement, hook: string): number {
  return Number(root.querySelector(hook)?.getAttribute('opacity'));
}

function scaleOf(root: SVGSVGElement, hook: string): number {
  const raw = root.querySelector(hook)?.getAttribute('transform') ?? '';
  return Number(/scale\(([\d.]+)\)/.exec(raw)?.[1]);
}

describe('buildApertureTimeline', () => {
  it('runs for 1.2 seconds', () => {
    const tl = buildApertureTimeline(makeMark(), null);
    expect(tl.duration()).toBeCloseTo(1.2, 5);
    tl.kill();
  });

  it('opens the iris along the designed cubic ease-out curve', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    for (const t of [0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.75, 1]) {
      tl.progress(t);
      expect(hingeOf(root)).toBeCloseTo(lerp(0, 30, eOut(seg(t, 0.06, 0.46))), 3);
    }
    tl.kill();
  });

  it('starts fully closed and ends fully open', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    tl.progress(0);
    expect(hingeOf(root)).toBeCloseTo(0, 3);

    tl.progress(1);
    expect(hingeOf(root)).toBeCloseTo(30, 3);
    tl.kill();
  });

  it('draws the rings on, fading the group in over the first quarter', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    for (const t of [0.3, 0.35, 0.45, 0.64, 1]) {
      tl.progress(t);
      const ringP = eOut(seg(t, 0.3, 0.64));
      expect(opacityOf(root, '[data-rings]')).toBeCloseTo(
        asWritten(clamp(ringP * 4, 0, 1), 3),
        6,
      );
      expect(
        Number(root.querySelector('[data-outer]')?.getAttribute('stroke-dashoffset')),
      ).toBeCloseTo(1000 * (1 - ringP), 1);
    }
    tl.kill();
  });

  it('sweeps the glint across and leaves it invisible at both ends', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    tl.progress(0);
    expect(opacityOf(root, '[data-glintwrap]')).toBe(0);

    tl.progress(0.66);
    const glintP = eIO(seg(0.66, 0.5, 0.82));
    expect(opacityOf(root, '[data-glintwrap]')).toBeCloseTo(
      asWritten(Math.sin(Math.PI * glintP), 3),
      6,
    );
    expect(Number(root.querySelector('[data-glint]')?.getAttribute('x'))).toBeCloseTo(
      asWritten(lerp(-230, 225, glintP), 1),
      6,
    );

    tl.progress(1);
    expect(opacityOf(root, '[data-glintwrap]')).toBe(0);
    tl.kill();
  });

  it('never surfaces the ghost arcs, which belong to the other sequence', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    for (const t of [0, 0.25, 0.5, 0.75, 1]) {
      tl.progress(t);
      expect(opacityOf(root, '[data-ghosts]')).toBe(0);
    }
    tl.kill();
  });

  it('settles the overshoot back to exactly 1', () => {
    const root = makeMark();
    const tl = buildApertureTimeline(root, null);

    tl.progress(0.72);
    expect(scaleOf(root, '[data-settle]')).toBeCloseTo(1.045, 3);

    tl.progress(1);
    expect(scaleOf(root, '[data-settle]')).toBeCloseTo(1, 4);
    tl.kill();
  });

  it('fades the wordmark in only after the mark has resolved', () => {
    const root = makeMark();
    const wordmark = document.createElement('span');
    document.body.append(wordmark);
    const tl = buildApertureTimeline(root, wordmark);

    tl.progress(0);
    expect(Number(wordmark.style.opacity)).toBe(0);

    // 0.7s of 1.2s is where the fade starts.
    tl.progress(0.5);
    expect(Number(wordmark.style.opacity)).toBe(0);

    tl.progress(1);
    expect(Number(wordmark.style.opacity)).toBe(1);
    tl.kill();
  });
});

describe('buildFocusRingsTimeline', () => {
  it('runs for 1.2 seconds', () => {
    const tl = buildFocusRingsTimeline(makeMark(), null);
    expect(tl.duration()).toBeCloseTo(1.2, 5);
    tl.kill();
  });

  it('surfaces the ghost arcs then dissolves them before the end', () => {
    const root = makeMark();
    const tl = buildFocusRingsTimeline(root, null);

    tl.progress(0);
    expect(opacityOf(root, '[data-ghosts]')).toBe(0);

    for (const t of [0.05, 0.1, 0.14, 0.3, 0.5, 1]) {
      tl.progress(t);
      const expected = eOut(seg(t, 0, 0.1)) * (1 - eOut(seg(t, 0.16, 0.5)));
      expect(opacityOf(root, '[data-ghosts]')).toBeCloseTo(expected, 3);
    }

    tl.progress(1);
    expect(opacityOf(root, '[data-ghosts]')).toBe(0);
    tl.kill();
  });

  it('rotates both rings into register at the centre', () => {
    const root = makeMark();
    const tl = buildFocusRingsTimeline(root, null);

    for (const t of [0.1, 0.25, 0.4, 0.52, 1]) {
      tl.progress(t);
      const asm = eOut(seg(t, 0.1, 0.52));
      expect(numberIn(root.querySelector('[data-outer]'), 'transform')).toBeCloseTo(
        lerp(52, 0, asm),
        2,
      );
      expect(numberIn(root.querySelector('[data-inner]'), 'transform')).toBeCloseTo(
        lerp(-64, 0, asm),
        2,
      );
    }

    // Both must land exactly on the settled 0deg, about the mark's centre.
    tl.progress(1);
    expect(root.querySelector('[data-outer]')?.getAttribute('transform')).toBe(
      'rotate(0.000 100 100)',
    );
    expect(root.querySelector('[data-inner]')?.getAttribute('transform')).toBe(
      'rotate(0.000 100 100)',
    );
    tl.kill();
  });

  it('grows the lens assembly in last, after the rings have landed', () => {
    const root = makeMark();
    const tl = buildFocusRingsTimeline(root, null);

    // Rings are already substantially drawn while the assembly is still absent.
    tl.progress(0.4);
    expect(opacityOf(root, '[data-assembly]')).toBeCloseTo(0, 3);
    expect(opacityOf(root, '[data-rings]')).toBeGreaterThan(0.5);

    for (const t of [0.48, 0.6, 0.75, 0.82, 1]) {
      tl.progress(t);
      const lensP = eOut(seg(t, 0.48, 0.82));
      expect(opacityOf(root, '[data-assembly]')).toBeCloseTo(lensP, 3);
      expect(scaleOf(root, '[data-assembly]')).toBeCloseTo(lerp(0.5, 1, lensP), 3);
    }
    tl.kill();
  });

  it('only finishes the iris opening, starting from 21 degrees', () => {
    const root = makeMark();
    const tl = buildFocusRingsTimeline(root, null);

    tl.progress(0);
    expect(hingeOf(root)).toBeCloseTo(21, 3);

    for (const t of [0.5, 0.7, 0.9, 1]) {
      tl.progress(t);
      expect(hingeOf(root)).toBeCloseTo(lerp(21, 30, eOut(seg(t, 0.5, 0.9))), 3);
    }
    tl.kill();
  });

  it('sums the two ring-opacity ramps rather than replacing one with the other', () => {
    const root = makeMark();
    const tl = buildFocusRingsTimeline(root, null);

    for (const t of [0, 0.08, 0.15, 0.16, 0.3, 0.54, 1]) {
      tl.progress(t);
      const expected = clamp(
        eOut(seg(t, 0, 0.16)) * 0.42 + eOut(seg(t, 0.14, 0.54)) * 0.58,
        0,
        1,
      );
      expect(opacityOf(root, '[data-rings]')).toBeCloseTo(asWritten(expected, 3), 6);
    }
    tl.kill();
  });
});

describe('both sequences', () => {
  it('settle on an identical mark, which is what makes Brand swappable', () => {
    const apertureRoot = makeMark();
    const focusRoot = makeMark();
    const aperture = buildApertureTimeline(apertureRoot, null);
    const focus = buildFocusRingsTimeline(focusRoot, null);

    aperture.progress(1);
    focus.progress(1);

    for (const hook of ['[data-blade]', '[data-settle]', '[data-outer]', '[data-inner]']) {
      expect(apertureRoot.querySelector(hook)?.getAttribute('transform')).toBe(
        focusRoot.querySelector(hook)?.getAttribute('transform'),
      );
    }
    for (const hook of ['[data-ghosts]', '[data-glintwrap]', '[data-rings]']) {
      expect(opacityOf(apertureRoot, hook)).toBe(opacityOf(focusRoot, hook));
    }

    aperture.kill();
    focus.kill();
  });
});
