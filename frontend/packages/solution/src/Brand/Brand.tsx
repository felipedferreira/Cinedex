import { useId } from 'react';
import type { FC } from 'react';
import { resolveBrandSize, type BrandSize } from './brandSize';
import { MarkBody } from './MarkBody';
import { Wordmark } from './Wordmark';

export interface BrandProps {
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
 * The Cinedex mark and wordmark, for `AuthCard`'s `brand` slot.
 *
 * A fragment rather than a wrapper: `AuthCard`'s brand row is a flex container
 * whose eyebrow uses `ml-auto`, so these two need to be its direct children.
 *
 * This is the only place the product's name and mark are drawn — which is the
 * point of the `compounds`/`solution` split. Swap this component and every
 * screen rebrands.
 *
 * Renders `MarkBody` at rest with no `ref` and no timeline — the same settled
 * state `BrandApertureAnimation`/`BrandFocusRingsAnimation` end on, so there's
 * no visual seam between an animated intro and this static mark appearing
 * everywhere else. It pulls in no GSAP: the timelines live behind
 * `useMarkTimeline`, which only the two animated variants call.
 */
export const Brand: FC<BrandProps> = ({ size = 'M' }) => {
  const uid = useId();
  const resolvedSize = resolveBrandSize(size);

  return (
    <>
      <MarkBody uid={uid} size={resolvedSize} />
      <Wordmark size={resolvedSize} />
    </>
  );
};
