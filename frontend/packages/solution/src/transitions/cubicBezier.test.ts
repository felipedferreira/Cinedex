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
