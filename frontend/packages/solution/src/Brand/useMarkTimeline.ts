import { useLayoutEffect, useRef, type RefObject } from 'react';
import type { MarkTimelineBuilder } from './timelines';

export interface MarkTimelineRefs {
  rootRef: RefObject<SVGSVGElement | null>;
  wordmarkRef: RefObject<HTMLSpanElement | null>;
}

/**
 * Builds one of `timelines.ts`'s sequences over a `MarkBody` and plays it once
 * on mount. GSAP owns the ticker, the easing and the sequencing; this hook owns
 * only the React lifecycle around it.
 *
 * `useLayoutEffect` rather than `useEffect` is load-bearing. `MarkBody` and the
 * wordmark are both authored in their **settled** state — that is what keeps the
 * static `Brand` and these two pixel-identical, and what lets a no-JS or
 * pre-hydration render show a complete logo instead of an invisible one. The
 * cost is that the settled frame would be painted once before an ordinary
 * effect could rewind it to the opening frame, which reads as a flash of the
 * finished logo followed by the animation. A layout effect runs before that
 * paint, and building the timeline applies its `fromTo` start values
 * immediately, so the first thing on screen is frame zero.
 *
 * Honors `prefers-reduced-motion` by scrubbing straight to the end rather than
 * skipping the timeline: `progress(1)` writes exactly the same attributes the
 * full sequence finishes on, so the reduced-motion render and the settled
 * render are the same DOM by construction rather than by two code paths
 * agreeing.
 */
export function useMarkTimeline(build: MarkTimelineBuilder): MarkTimelineRefs {
  const rootRef = useRef<SVGSVGElement>(null);
  const wordmarkRef = useRef<HTMLSpanElement>(null);

  useLayoutEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }

    const timeline = build(root, wordmarkRef.current);

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      timeline.progress(1);
    } else {
      timeline.play();
    }

    return () => {
      // `kill` rather than `revert` — revert would rewind every attribute to
      // the value it held before the timeline touched it, which on an unmount
      // mid-sequence is pointless work on a node React is about to drop, and on
      // a StrictMode remount would fight the fresh timeline for the same nodes.
      timeline.kill();
    };
  }, [build]);

  return { rootRef, wordmarkRef };
}
