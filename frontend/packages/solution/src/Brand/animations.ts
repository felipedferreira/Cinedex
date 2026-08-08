/**
 * The two logo sequences, ported from the artifact they were designed and
 * verified in. Each is a pure function of progress `t` (0…1) that writes SVG
 * attributes directly onto the elements under a `MarkBody` root — the same
 * imperative, `requestAnimationFrame`-driven approach the artifact used,
 * rather than per-frame React state, so a 1.2s run doesn't cost 70+
 * re-renders. `useMarkAnimation` owns the rAF loop; these just render one
 * instant of it.
 *
 * Querying by `data-*` attribute (rather than threading ~15 individual refs
 * through each component) mirrors the artifact's own `buildMark()` structure
 * closely enough to keep this a near-transcription of already-verified code —
 * the iris opening was empirically confirmed to match
 * `PIVOT_RADIUS · sin(phi)` to within a device pixel, and that verification
 * only holds if the maths below stays unchanged from what produced it.
 */

function clamp(v: number, min: number, max: number): number {
  return v < min ? min : v > max ? max : v;
}

function seg(t: number, a: number, b: number): number {
  return clamp((t - a) / (b - a), 0, 1);
}

/** Cubic ease-out. */
function eOut(t: number): number {
  return 1 - (1 - t) ** 3;
}

/** Cubic ease-in-out — used for the glint, which needs to ramp up then back down. */
function eIO(t: number): number {
  return t < 0.5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2;
}

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

function scaleAbout(s: number): string {
  return `translate(100,100) scale(${s.toFixed(4)}) translate(-100,-100)`;
}

/** `pathLength={1000}` on the ring paths normalises the dash maths. */
function dash(path: SVGPathElement, amount: number): void {
  path.setAttribute('stroke-dasharray', '1000');
  path.setAttribute('stroke-dashoffset', (1000 * (1 - amount)).toFixed(2));
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

function applyGlint(m: MarkElements, p: number): void {
  if (p <= 0 || p >= 1) {
    m.glintWrap.setAttribute('opacity', '0');
    return;
  }
  m.glintWrap.setAttribute('opacity', Math.sin(Math.PI * p).toFixed(3));
  m.glint.setAttribute('x', lerp(-230, 225, p).toFixed(1));
}

export interface FrameResult {
  /** Current iris hinge angle in degrees — 0 closed, 30 fully open. */
  hingeDeg: number;
  /** Whether the iris is on screen yet (0…1) — animation 02 delays its entrance. */
  visible: number;
}

/**
 * "Lens aperture": opens from a closed iris. The blades hinge outward while
 * the assembly counter-rotates, the rings draw on around the gap, a glint
 * crosses the metal, then the wordmark's caller fades it in (see
 * `BrandApertureAnimation`).
 */
export function renderApertureFrame(
  root: SVGSVGElement,
  t: number,
): FrameResult {
  const m = collect(root);

  const hingeDeg = lerp(0, 30, eOut(seg(t, 0.06, 0.46)));
  const spin = lerp(-46, 0, eOut(seg(t, 0.06, 0.56)));
  const lensP = eOut(seg(t, 0.18, 0.54));
  const ringP = eOut(seg(t, 0.3, 0.64));
  const glintP = eIO(seg(t, 0.5, 0.82));
  const settleP = eOut(seg(t, 0.72, 1));

  m.blades.forEach((blade) => {
    blade.setAttribute('transform', `rotate(${hingeDeg.toFixed(3)})`);
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

  return { hingeDeg, visible: 1 };
}

/**
 * "Focus rings": the inverse order. Ghost arcs surface out of darkness and
 * converge, the two rings rotate into register to form the "C", and only
 * then does the lens assembly grow in at the centre.
 */
export function renderFocusRingsFrame(
  root: SVGSVGElement,
  t: number,
): FrameResult {
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

  m.blades.forEach((blade) => {
    blade.setAttribute('transform', `rotate(${hingeDeg.toFixed(3)})`);
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
  const ghostStartDeg = [62, -78, 44];
  m.ghostArcs.forEach((arc, i) => {
    arc.setAttribute(
      'transform',
      `rotate(${lerp(ghostStartDeg[i], 0, asm).toFixed(2)} 100 100)`,
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

  return { hingeDeg, visible: lensP };
}
