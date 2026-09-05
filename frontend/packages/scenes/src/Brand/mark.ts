/**
 * Geometry for the Cinedex mark: a camera iris forming a "C".
 *
 * The single source of truth `Brand`, `BrandApertureAnimation` and
 * `BrandFocusRingsAnimation` all build their `<svg>` from — so the three stay
 * pixel-identical at rest and a tweak here reaches all of them.
 *
 * The 200×200 coordinate space, the two arc paths and the blade path are
 * unchanged from the artifact this was verified in: rasterizing the mark
 * confirmed the "C" gap spans the predicted arc within rounding, and the iris
 * opening empirically matches `PIVOT_RADIUS · sin(phi)` to within a device
 * pixel across phi = 5°…30°.
 */

export const MARK_VIEW_BOX = '0 0 200 200';

/** Outer ring: r 80, stroke 15, gap facing right (0°). */
export const OUTER_ARC_PATH = 'M166.32 55.26A80 80 0 1 0 166.32 144.74';

/** Inner ring: r 59, stroke 9, same gap. */
export const INNER_ARC_PATH = 'M146.49 63.68A59 59 0 1 0 146.49 136.32';

/**
 * One iris blade, drawn as a half-plane. Each instance is hinged at its own
 * pivot on the `PIVOT_RADIUS` circle (via a `translate` + `rotate` ancestor)
 * and clipped to that circle, so the six leading edges always meet as a
 * hexagon of inradius `PIVOT_RADIUS · sin(phi)` — one hinge angle drives the
 * whole opening.
 */
export const BLADE_PATH = 'M0 0L-150 0L-150 -150L0 -150Z';

/** Where each of the six blades pivots, in degrees around the mark's centre. */
export const BLADE_PIVOT_ANGLES_DEG = [0, 60, 120, 180, 240, 300] as const;

/** Radius the blade pivots sit on, and the iris clip circle's radius. */
export const PIVOT_RADIUS = 47;

/** Hinge angle at which the iris reads as fully open. */
export const OPEN_HINGE_DEG = 30;

/** Lens dome radius, and its two specular highlight dots. */
export const DOME_RADIUS = 22.5;
export const DOME_SPECULAR_PRIMARY = { cx: 92.2, cy: 92.2, r: 3.6 };
export const DOME_SPECULAR_SECONDARY = {
  cx: 107,
  cy: 105.6,
  r: 1.8,
  opacity: 0.75,
};

/**
 * Ring stroke colour, as three gradient stops. Each stop is a `light-dark()`
 * pair rather than a fixed colour, so ONE gradient definition reads as the
 * artifact's validated "flat" treatment on a light surface (all three stops
 * collapse to the same near-black) and its validated "metal" treatment on a
 * dark surface (the three stay distinct, producing the sheen) — no theme
 * detection in JS, the same mechanism `tokens.css` uses everywhere else.
 */
export const RING_GRADIENT_STOPS = [
  { offset: '0', color: 'light-dark(#16171B, #8E949C)' },
  { offset: '.45', color: 'light-dark(#16171B, #5A5F66)' },
  { offset: '1', color: 'light-dark(#16171B, #2A2D32)' },
] as const;

/** Barrel interior fill — the same light/light-dark split as the rings. */
export const BARREL_FILL = 'light-dark(#16171B, #08090B)';

/**
 * Blade and dome gradients are NOT theme-reactive — the artifact never varied
 * them by ground, only the rings and barrel. `gradientUnits="userSpaceOnUse"`
 * is load-bearing: it resolves in each blade's own rotated frame (origin =
 * its pivot), so the six facets catch light differently. The default
 * `objectBoundingBox` makes every blade sample the same corner of its
 * 150×150 quad and the iris flattens into one uniform disc — confirmed by
 * rasterizing both variants in the verifying session.
 */
export const BLADE_GRADIENT_STOPS = [
  { offset: '0', color: '#565C65' },
  { offset: '.55', color: '#2F343A' },
  { offset: '1', color: '#14171B' },
] as const;
export const BLADE_GRADIENT_VECTOR = { x1: 0, y1: 0, x2: -72, y2: -72 };
export const BLADE_STROKE = '#6B7178';

export const DOME_GRADIENT_STOPS = [
  { offset: '0', color: '#3B4048' },
  { offset: '.42', color: '#15171C' },
  { offset: '1', color: '#040507' },
] as const;

/**
 * The light sweep used by both animated components. Masked to the rings'
 * silhouette plus the barrel disc (see `MarkDefs`), so the sweep only ever
 * lights up the mark itself rather than spilling across its bounding box.
 */
export const GLINT_GRADIENT_STOPS = [
  { offset: '0', color: '#fff', opacity: 0 },
  { offset: '.42', color: '#fff', opacity: 0.92 },
  { offset: '.58', color: '#fff', opacity: 0.92 },
  { offset: '1', color: '#fff', opacity: 0 },
] as const;
