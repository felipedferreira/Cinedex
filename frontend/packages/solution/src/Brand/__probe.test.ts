import { describe, expect, it } from 'vitest';
import { gsap } from 'gsap';
import { INNER_ARC_PATH, OUTER_ARC_PATH, PIVOT_RADIUS } from './mark';

/* ------------------------------------------------------------------ */
/* ORIGINAL implementation, transcribed verbatim from git HEAD         */
/* ------------------------------------------------------------------ */

function clamp(v: number, min: number, max: number): number {
  return v < min ? min : v > max ? max : v;
}
function seg(t: number, a: number, b: number): number {
  return clamp((t - a) / (b - a), 0, 1);
}
function eOut(t: number): number {
  return 1 - (1 - t) ** 3;
}
function eIO(t: number): number {
  return t < 0.5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2;
}
function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}
function scaleAbout(s: number): string {
  return `translate(100,100) scale(${s.toFixed(4)}) translate(-100,-100)`;
}
function dash(path: Element, amount: number): void {
  path.setAttribute('stroke-dasharray', '1000');
  path.setAttribute('stroke-dashoffset', (1000 * (1 - amount)).toFixed(2));
}
function query(root: SVGSVGElement, s: string): Element {
  const f = root.querySelector(s);
  if (!f) throw new Error(s);
  return f;
}
function queryAll(root: SVGSVGElement, s: string): Element[] {
  return [...root.querySelectorAll(s)];
}
interface M {
  settle: Element;
  assembly: Element;
  lens: Element;
  spin: Element;
  blades: Element[];
  ghosts: Element;
  ghostArcs: Element[];
  rings: Element;
  outer: Element;
  inner: Element;
  glintWrap: Element;
  glint: Element;
}
function collect(root: SVGSVGElement): M {
  return {
    settle: query(root, '[data-settle]'),
    assembly: query(root, '[data-assembly]'),
    lens: query(root, '[data-lens]'),
    spin: query(root, '[data-spin]'),
    blades: queryAll(root, '[data-blade]'),
    ghosts: query(root, '[data-ghosts]'),
    ghostArcs: queryAll(root, '[data-ghost]'),
    rings: query(root, '[data-rings]'),
    outer: query(root, '[data-outer]'),
    inner: query(root, '[data-inner]'),
    glintWrap: query(root, '[data-glintwrap]'),
    glint: query(root, '[data-glint]'),
  };
}
function applyGlint(m: M, p: number): void {
  if (p <= 0 || p >= 1) {
    m.glintWrap.setAttribute('opacity', '0');
    return;
  }
  m.glintWrap.setAttribute('opacity', Math.sin(Math.PI * p).toFixed(3));
  m.glint.setAttribute('x', lerp(-230, 225, p).toFixed(1));
}

function renderApertureFrame(root: SVGSVGElement, t: number): void {
  const m = collect(root);
  const hingeDeg = lerp(0, 30, eOut(seg(t, 0.06, 0.46)));
  const spin = lerp(-46, 0, eOut(seg(t, 0.06, 0.56)));
  const lensP = eOut(seg(t, 0.18, 0.54));
  const ringP = eOut(seg(t, 0.3, 0.64));
  const glintP = eIO(seg(t, 0.5, 0.82));
  const settleP = eOut(seg(t, 0.72, 1));

  m.blades.forEach((b) => {
    b.setAttribute('transform', `rotate(${hingeDeg.toFixed(3)})`);
  });
  m.spin.setAttribute('transform', `rotate(${spin.toFixed(3)} 100 100)`);
  m.assembly.setAttribute('transform', scaleAbout(1));
  m.assembly.setAttribute('opacity', '1');
  m.lens.setAttribute('opacity', lensP.toFixed(3));
  m.lens.setAttribute('transform', scaleAbout(lerp(0.62, 1, lensP)));
  m.ghosts.setAttribute('opacity', '0');
  m.rings.setAttribute('opacity', clamp(ringP * 4, 0, 1).toFixed(3));
  dash(m.outer, ringP);
  dash(m.inner, ringP);
  m.outer.setAttribute('transform', 'rotate(0 100 100)');
  m.inner.setAttribute('transform', 'rotate(0 100 100)');
  applyGlint(m, glintP);
  m.settle.setAttribute('transform', scaleAbout(lerp(1.045, 1, settleP)));
}

function renderFocusRingsFrame(root: SVGSVGElement, t: number): void {
  const m = collect(root);
  const asm = eOut(seg(t, 0.1, 0.52));
  const ringOp = clamp(
    eOut(seg(t, 0, 0.16)) * 0.42 + eOut(seg(t, 0.14, 0.54)) * 0.58,
    0,
    1,
  );
  const ringDraw = lerp(0.34, 1, eOut(seg(t, 0.02, 0.48)));
  const ghostFade = eOut(seg(t, 0, 0.1)) * (1 - eOut(seg(t, 0.16, 0.5)));
  const lensP = eOut(seg(t, 0.48, 0.82));
  const hingeDeg = lerp(21, 30, eOut(seg(t, 0.5, 0.9)));
  const glintP = eIO(seg(t, 0.68, 1));
  const settleP = eOut(seg(t, 0.82, 1));

  m.blades.forEach((b) => {
    b.setAttribute('transform', `rotate(${hingeDeg.toFixed(3)})`);
  });
  m.spin.setAttribute(
    'transform',
    `rotate(${lerp(-18, 0, asm).toFixed(3)} 100 100)`,
  );
  m.assembly.setAttribute('opacity', lensP.toFixed(3));
  m.assembly.setAttribute('transform', scaleAbout(lerp(0.5, 1, lensP)));
  m.lens.setAttribute('opacity', '1');
  m.lens.setAttribute('transform', scaleAbout(1));
  m.ghosts.setAttribute('opacity', ghostFade.toFixed(3));
  const g = [62, -78, 44];
  m.ghostArcs.forEach((arc, i) => {
    arc.setAttribute(
      'transform',
      `rotate(${lerp(g[i], 0, asm).toFixed(2)} 100 100)`,
    );
  });
  m.rings.setAttribute('opacity', ringOp.toFixed(3));
  dash(m.outer, ringDraw);
  dash(m.inner, ringDraw);
  m.outer.setAttribute(
    'transform',
    `rotate(${lerp(52, 0, asm).toFixed(3)} 100 100)`,
  );
  m.inner.setAttribute(
    'transform',
    `rotate(${lerp(-64, 0, asm).toFixed(3)} 100 100)`,
  );
  applyGlint(m, glintP);
  m.settle.setAttribute('transform', scaleAbout(lerp(1.03, 1, settleP)));
}

/* ------------------------------------------------------------------ */
/* PROPOSED: per-property proxies on a real GSAP timeline              */
/* ------------------------------------------------------------------ */

const SEQ = 1.2;
const OUT = 'power2.out';
const IN_OUT = 'power2.inOut';
function win(a: number, b: number) {
  return { at: SEQ * a, dur: SEQ * (b - a) };
}

interface ApertureBeats {
  hinge: number;
  spin: number;
  lensP: number;
  ringP: number;
  glintP: number;
  settleP: number;
}

function buildAperture(root: SVGSVGElement): gsap.core.Timeline {
  const m = collect(root);
  const b: ApertureBeats = {
    hinge: 0,
    spin: -46,
    lensP: 0,
    ringP: 0,
    glintP: 0,
    settleP: 0,
  };

  const write = () => {
    m.blades.forEach((bl) => {
      bl.setAttribute('transform', `rotate(${b.hinge.toFixed(3)})`);
    });
    m.spin.setAttribute('transform', `rotate(${b.spin.toFixed(3)} 100 100)`);
    m.assembly.setAttribute('transform', scaleAbout(1));
    m.assembly.setAttribute('opacity', '1');
    m.lens.setAttribute('opacity', b.lensP.toFixed(3));
    m.lens.setAttribute('transform', scaleAbout(lerp(0.62, 1, b.lensP)));
    m.ghosts.setAttribute('opacity', '0');
    m.rings.setAttribute('opacity', clamp(b.ringP * 4, 0, 1).toFixed(3));
    dash(m.outer, b.ringP);
    dash(m.inner, b.ringP);
    m.outer.setAttribute('transform', 'rotate(0 100 100)');
    m.inner.setAttribute('transform', 'rotate(0 100 100)');
    applyGlint(m, b.glintP);
    m.settle.setAttribute('transform', scaleAbout(lerp(1.045, 1, b.settleP)));
  };

  const tl = gsap.timeline({ paused: true, onUpdate: write });
  const hinge = win(0.06, 0.46);
  const spin = win(0.06, 0.56);
  const lens = win(0.18, 0.54);
  const rings = win(0.3, 0.64);
  const glint = win(0.5, 0.82);
  const settle = win(0.72, 1);

  tl.addLabel('iris', hinge.at)
    .fromTo(b, { hinge: 0 }, { hinge: 30, duration: hinge.dur, ease: OUT }, 'iris')
    .fromTo(b, { spin: -46 }, { spin: 0, duration: spin.dur, ease: OUT }, spin.at)
    .fromTo(b, { lensP: 0 }, { lensP: 1, duration: lens.dur, ease: OUT }, lens.at)
    .fromTo(b, { ringP: 0 }, { ringP: 1, duration: rings.dur, ease: OUT }, rings.at)
    .fromTo(
      b,
      { glintP: 0 },
      { glintP: 1, duration: glint.dur, ease: IN_OUT },
      glint.at,
    )
    .fromTo(
      b,
      { settleP: 0 },
      { settleP: 1, duration: settle.dur, ease: OUT },
      settle.at,
    )
    .set({}, {}, SEQ);

  write();
  return tl;
}

interface FocusBeats {
  asm: number;
  rampA: number;
  rampB: number;
  ringDraw: number;
  ghostIn: number;
  ghostOut: number;
  lensP: number;
  hingeP: number;
  glintP: number;
  settleP: number;
}

function buildFocus(root: SVGSVGElement): gsap.core.Timeline {
  const m = collect(root);
  const b: FocusBeats = {
    asm: 0,
    rampA: 0,
    rampB: 0,
    ringDraw: 0,
    ghostIn: 0,
    ghostOut: 0,
    lensP: 0,
    hingeP: 0,
    glintP: 0,
    settleP: 0,
  };
  const GHOST = [62, -78, 44];

  const write = () => {
    const hingeDeg = lerp(21, 30, b.hingeP);
    m.blades.forEach((bl) => {
      bl.setAttribute('transform', `rotate(${hingeDeg.toFixed(3)})`);
    });
    m.spin.setAttribute(
      'transform',
      `rotate(${lerp(-18, 0, b.asm).toFixed(3)} 100 100)`,
    );
    m.assembly.setAttribute('opacity', b.lensP.toFixed(3));
    m.assembly.setAttribute('transform', scaleAbout(lerp(0.5, 1, b.lensP)));
    m.lens.setAttribute('opacity', '1');
    m.lens.setAttribute('transform', scaleAbout(1));
    m.ghosts.setAttribute(
      'opacity',
      (b.ghostIn * (1 - b.ghostOut)).toFixed(3),
    );
    m.ghostArcs.forEach((arc, i) => {
      arc.setAttribute(
        'transform',
        `rotate(${lerp(GHOST[i], 0, b.asm).toFixed(2)} 100 100)`,
      );
    });
    m.rings.setAttribute(
      'opacity',
      clamp(b.rampA * 0.42 + b.rampB * 0.58, 0, 1).toFixed(3),
    );
    const drawn = lerp(0.34, 1, b.ringDraw);
    dash(m.outer, drawn);
    dash(m.inner, drawn);
    m.outer.setAttribute(
      'transform',
      `rotate(${lerp(52, 0, b.asm).toFixed(3)} 100 100)`,
    );
    m.inner.setAttribute(
      'transform',
      `rotate(${lerp(-64, 0, b.asm).toFixed(3)} 100 100)`,
    );
    applyGlint(m, b.glintP);
    m.settle.setAttribute('transform', scaleAbout(lerp(1.03, 1, b.settleP)));
  };

  const tl = gsap.timeline({ paused: true, onUpdate: write });
  const assemble = win(0.1, 0.52);
  const rampA = win(0, 0.16);
  const rampB = win(0.14, 0.54);
  const draw = win(0.02, 0.48);
  const gIn = win(0, 0.1);
  const gOut = win(0.16, 0.5);
  const lens = win(0.48, 0.82);
  const hinge = win(0.5, 0.9);
  const glint = win(0.68, 1);
  const settle = win(0.82, 1);

  tl.addLabel('assemble', assemble.at)
    .fromTo(b, { asm: 0 }, { asm: 1, duration: assemble.dur, ease: OUT }, 'assemble')
    .fromTo(b, { rampA: 0 }, { rampA: 1, duration: rampA.dur, ease: OUT }, rampA.at)
    .fromTo(b, { rampB: 0 }, { rampB: 1, duration: rampB.dur, ease: OUT }, rampB.at)
    .fromTo(
      b,
      { ringDraw: 0 },
      { ringDraw: 1, duration: draw.dur, ease: OUT },
      draw.at,
    )
    .fromTo(b, { ghostIn: 0 }, { ghostIn: 1, duration: gIn.dur, ease: OUT }, gIn.at)
    .fromTo(
      b,
      { ghostOut: 0 },
      { ghostOut: 1, duration: gOut.dur, ease: OUT },
      gOut.at,
    )
    .fromTo(b, { lensP: 0 }, { lensP: 1, duration: lens.dur, ease: OUT }, lens.at)
    .fromTo(
      b,
      { hingeP: 0 },
      { hingeP: 1, duration: hinge.dur, ease: OUT },
      hinge.at,
    )
    .fromTo(
      b,
      { glintP: 0 },
      { glintP: 1, duration: glint.dur, ease: IN_OUT },
      glint.at,
    )
    .fromTo(
      b,
      { settleP: 0 },
      { settleP: 1, duration: settle.dur, ease: OUT },
      settle.at,
    )
    .set({}, {}, SEQ);

  write();
  return tl;
}

/* ------------------------------------------------------------------ */

function makeMark(): SVGSVGElement {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.innerHTML = `
    <g data-settle>
      <g data-assembly>
        <g data-lens><circle cx="100" cy="100" r="22.5"></circle></g>
        <g data-irisclip><g data-spin>
          ${[0, 60, 120, 180, 240, 300]
            .map(
              (a) =>
                `<g transform="translate(100,100) rotate(${String(a)}) translate(${String(PIVOT_RADIUS)},0)"><g data-blade transform="rotate(30)"><path d="M0 0L-150 0L-150 -150L0 -150Z"></path></g></g>`,
            )
            .join('')}
        </g></g>
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

const HOOKS = [
  '[data-settle]',
  '[data-assembly]',
  '[data-lens]',
  '[data-spin]',
  '[data-blade]',
  '[data-ghosts]',
  '[data-rings]',
  '[data-outer]',
  '[data-inner]',
  '[data-glintwrap]',
  '[data-glint]',
];
const ATTRS = [
  'transform',
  'opacity',
  'stroke-dasharray',
  'stroke-dashoffset',
  'x',
];

function snapshot(root: SVGSVGElement): string {
  const out: string[] = [];
  for (const hook of HOOKS) {
    for (const el of root.querySelectorAll(hook)) {
      for (const a of ATTRS) {
        const v = el.getAttribute(a);
        if (v !== null) out.push(`${hook}|${a}=${v}`);
      }
    }
  }
  for (const el of root.querySelectorAll('[data-ghost]')) {
    out.push(`[data-ghost]|transform=${String(el.getAttribute('transform'))}`);
  }
  return out.join('\n');
}

const SAMPLES = Array.from({ length: 241 }, (_, i) => i / 240);

/**
 * GSAP and the hand-rolled renderer evaluate the same ease at slightly
 * different floating-point precisions. Comparing their serialized SVG
 * attributes byte-for-byte therefore turns a sub-pixel difference into a
 * failure at a `toFixed` boundary (for example, -24.597 vs -24.598 degrees).
 *
 * Keep this probe strict about the rendered shape and its attribute structure,
 * while comparing animated numbers at the precision each attribute is drawn.
 */
function expectVisuallyEquivalentSnapshots(original: string, gsapFrame: string): void {
  const expected = original.split('\n');
  const actual = gsapFrame.split('\n');
  expect(actual).toHaveLength(expected.length);

  for (const [index, expectedLine] of expected.entries()) {
    const actualLine = actual[index];
    const [expectedKey, expectedValue] = expectedLine.split('=', 2);
    const [actualKey, actualValue] = actualLine.split('=', 2);
    expect(actualKey).toBe(expectedKey);

    if (expectedValue === actualValue) {
      continue;
    }

    const expectedNumbers = expectedValue.match(/-?[\d.]+/g);
    const actualNumbers = actualValue.match(/-?[\d.]+/g);
    expect(actualNumbers).not.toBeNull();
    expect(actualNumbers).toHaveLength(expectedNumbers?.length ?? 0);

    // Allow one last displayed digit: the two implementations can land on
    // opposite sides of a `toFixed` boundary while remaining less than half a
    // unit apart before serialization.
    const tolerance = expectedKey.endsWith('|x')
      ? 0.11
      : expectedKey.endsWith('|stroke-dashoffset')
        ? 0.011
        : expectedKey.endsWith('|transform') && expectedValue.includes('scale')
          ? 0.00011
          : 0.0011;

    for (const [numberIndex, expectedNumber] of (expectedNumbers ?? []).entries()) {
      expect(
        Math.abs(Number(actualNumbers?.[numberIndex]) - Number(expectedNumber)),
      ).toBeLessThanOrEqual(tolerance);
    }
  }
}

describe('proxy-timeline vs original renderFrame', () => {
  it('aperture: visually equivalent attributes at every sampled t', () => {
    const a = makeMark();
    const b = makeMark();
    const tl = buildAperture(b);

    const diffs: string[] = [];
    for (const t of SAMPLES) {
      renderApertureFrame(a, t);
      tl.progress(t);
      try {
        expectVisuallyEquivalentSnapshots(snapshot(a), snapshot(b));
      } catch {
        diffs.push(`t=${String(t)}`);
      }
    }
    if (diffs.length) {
      renderApertureFrame(a, Number(diffs[0].slice(2)));
      tl.progress(Number(diffs[0].slice(2)));
      const la = snapshot(a).split('\n');
      const lb = snapshot(b).split('\n');
      const first = la.find((l, i) => l !== lb[i]);
      throw new Error(
        `${String(diffs.length)}/${String(SAMPLES.length)} mismatched. first=${diffs[0]} ` +
          `orig="${String(first)}" gsap="${String(lb[la.indexOf(first ?? '')])}"`,
      );
    }
    expect(diffs).toEqual([]);
    tl.kill();
  });

  it('focus rings: visually equivalent attributes at every sampled t', () => {
    const a = makeMark();
    const b = makeMark();
    const tl = buildFocus(b);

    const diffs: string[] = [];
    for (const t of SAMPLES) {
      renderFocusRingsFrame(a, t);
      tl.progress(t);
      try {
        expectVisuallyEquivalentSnapshots(snapshot(a), snapshot(b));
      } catch {
        diffs.push(`t=${String(t)}`);
      }
    }
    if (diffs.length) {
      const t0 = Number(diffs[0].slice(2));
      renderFocusRingsFrame(a, t0);
      tl.progress(t0);
      const la = snapshot(a).split('\n');
      const lb = snapshot(b).split('\n');
      const first = la.find((l, i) => l !== lb[i]);
      throw new Error(
        `${String(diffs.length)}/${String(SAMPLES.length)} mismatched. first=${diffs[0]} ` +
          `orig="${String(first)}" gsap="${String(lb[la.indexOf(first ?? '')])}"`,
      );
    }
    expect(diffs).toEqual([]);
    tl.kill();
  });

  it('power2 eases are bit-exact against eOut / eIO', () => {
    const out = gsap.parseEase(OUT);
    const io = gsap.parseEase(IN_OUT);
    let maxOut = 0;
    let maxIO = 0;
    for (let i = 0; i <= 2000; i++) {
      const p = i / 2000;
      maxOut = Math.max(maxOut, Math.abs(out(p) - eOut(p)));
      maxIO = Math.max(maxIO, Math.abs(io(p) - eIO(p)));
    }
    expect(maxOut).toBe(0);
    expect(maxIO).toBe(0);
  });

  it('timeline duration is exactly 1.2s and scrubbing is order-independent', () => {
    const root = makeMark();
    const tl = buildAperture(root);
    expect(tl.duration()).toBeCloseTo(1.2, 10);

    tl.progress(0.25);
    tl.progress(0.35);
    const fwd = snapshot(root);
    tl.progress(0.9);
    tl.progress(0.35);
    expect(snapshot(root)).toBe(fwd);
    tl.kill();
  });
});
