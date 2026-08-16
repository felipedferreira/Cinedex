import { useId } from 'react';
import { resolveBrandSize, type BrandSize } from './brandSize';
import { MarkBody } from './MarkBody';
import { buildFocusRingsTimeline } from './timelines';
import { useMarkTimeline } from './useMarkTimeline';
import { Wordmark } from './Wordmark';

export interface BrandFocusRingsAnimationProps {
  /**
   * The lockup scale. Named values run from `XS` through `XL`; `M` is the
   * default and `XS` preserves the original size. A positive whole number is
   * also accepted: 1–5 alias `XS`–`XL`, while higher numbers continue the
   * scale. Zero, negative, fractional, `NaN`, and infinite numbers throw a
   * `RangeError`.
   */
  size?: BrandSize;
}

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
export const BrandFocusRingsAnimation = ({
  size = 'M',
}: BrandFocusRingsAnimationProps) => {
  const uid = useId();
  const resolvedSize = resolveBrandSize(size);
  const { rootRef, wordmarkRef } = useMarkTimeline(buildFocusRingsTimeline);

  return (
    <>
      <MarkBody uid={uid} size={resolvedSize} ref={rootRef} />
      <Wordmark size={resolvedSize} ref={wordmarkRef} />
    </>
  );
};
