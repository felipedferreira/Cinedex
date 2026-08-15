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
function solveForX(x: number, xCoefficients: [number, number, number]): number {
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
