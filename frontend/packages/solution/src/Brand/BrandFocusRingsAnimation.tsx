import { useId } from 'react';
import { MarkBody } from './MarkBody';
import { buildFocusRingsTimeline } from './timelines';
import { useMarkTimeline } from './useMarkTimeline';
import { Wordmark } from './Wordmark';

/**
 * `Brand`, plus the "focus rings" intro — the inverse order from
 * `BrandApertureAnimation`: ghost arcs surface out of darkness and converge,
 * the two rings rotate into register to form the "C", and only then does the
 * lens assembly grow in at the centre before the wordmark fades in. Runs once
 * on mount and ends on the exact same settled attributes `Brand` renders
 * statically.
 *
 * Not currently wired into any screen — `HomeScreen` and the docs site's
 * landing page both use `BrandApertureAnimation`. This ships fully built and
 * exported so it's a one-line swap if that choice changes, and so it can be
 * reviewed on its own in Storybook.
 */
export function BrandFocusRingsAnimation() {
  const uid = useId();
  const { rootRef, wordmarkRef } = useMarkTimeline(buildFocusRingsTimeline);

  return (
    <>
      <MarkBody uid={uid} ref={rootRef} />
      <Wordmark ref={wordmarkRef} />
    </>
  );
}
