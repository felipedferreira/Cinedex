import { useEffect, useRef, type RefObject } from 'react';
import type { FrameResult } from './animations';

type FrameRenderer = (root: SVGSVGElement, t: number) => FrameResult;

/**
 * Drives a `MarkBody` through one `renderFrame` sequence via
 * `requestAnimationFrame`, once, on mount — attribute writes rather than
 * React state, so the 1.2s run doesn't cost a re-render per frame.
 *
 * Honors `prefers-reduced-motion`: a reduced-motion viewer gets `renderFrame`
 * called once at `t = 1` (the settled frame `Brand` renders statically)
 * rather than the sequence.
 */
export function useMarkAnimation(
  renderFrame: FrameRenderer,
  durationMs: number,
): RefObject<SVGSVGElement | null> {
  const rootRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      renderFrame(root, 1);
      return;
    }

    let frameId: number;
    let start: number | null = null;

    const tick = (now: number) => {
      start ??= now;
      const t = Math.min((now - start) / durationMs, 1);
      renderFrame(root, t);
      if (t < 1) {
        frameId = requestAnimationFrame(tick);
      }
    };
    frameId = requestAnimationFrame(tick);

    return () => {
      cancelAnimationFrame(frameId);
    };
  }, [renderFrame, durationMs]);

  return rootRef;
}
