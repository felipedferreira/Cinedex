import { useId } from 'react';
import { MarkBody } from './MarkBody';

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
 * Renders `MarkBody` at rest with no `ref` and no animation — the same
 * settled state `BrandApertureAnimation`/`BrandFocusRingsAnimation` end on,
 * so there's no visual seam between an animated intro and this static mark
 * appearing everywhere else. The wordmark stays plain text rather than the
 * hand-drawn SVG paths the source artifact also produced: at this size, next
 * to an eyebrow label, text keeps the design system's type scale (`text-brand`)
 * and screen-reader semantics that a geometric wordmark would have to
 * reinvent, for no legibility gain the artifact's large marketing-style
 * lockup actually needed.
 */
export function Brand() {
  const uid = useId();

  return (
    <>
      <MarkBody uid={uid} />
      <span className="font-mono text-brand font-semibold tracking-eyebrow text-text-h uppercase">
        Cinedex
      </span>
    </>
  );
}
