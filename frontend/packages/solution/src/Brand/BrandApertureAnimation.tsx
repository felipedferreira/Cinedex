import { useId } from 'react';
import { resolveBrandSize, type BrandSize } from './brandSize';
import { MarkBody } from './MarkBody';
import { buildApertureTimeline } from './timelines';
import { useMarkTimeline } from './useMarkTimeline';
import { Wordmark } from './Wordmark';

export interface BrandApertureAnimationProps {
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
 * `Brand`, plus the "lens aperture" intro: the iris opens from closed while the
 * assembly counter-rotates, the rings draw on around the gap, a glint crosses
 * the metal, then the wordmark fades in. Runs once on mount and ends on the
 * exact same settled attributes `Brand` renders statically — swapping one for
 * the other mid-flow (e.g. after `HomeScreen` first paints) shows no pop.
 *
 * The choreography itself is `buildApertureTimeline`; this component is only
 * the two DOM nodes it drives. `BrandFocusRingsAnimation` is the alternate
 * sequence, built and exported the same way.
 */
export const BrandApertureAnimation = ({
  size = 'M',
}: BrandApertureAnimationProps) => {
  const uid = useId();
  const resolvedSize = resolveBrandSize(size);
  const { rootRef, wordmarkRef } = useMarkTimeline(buildApertureTimeline);

  return (
    <>
      <MarkBody uid={uid} size={resolvedSize} ref={rootRef} />
      <Wordmark size={resolvedSize} ref={wordmarkRef} />
    </>
  );
};
