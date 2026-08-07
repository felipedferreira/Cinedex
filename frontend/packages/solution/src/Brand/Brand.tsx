/**
 * The Cinedex mark and wordmark, for `AuthCard`'s `brand` slot.
 *
 * A fragment rather than a wrapper: `AuthCard`'s brand row is a flex container
 * whose eyebrow uses `ml-auto`, so these two need to be its direct children.
 *
 * This is the only place the product's name and mark are drawn — which is the
 * point of the `compounds`/`solution` split. Swap this component and every
 * screen rebrands.
 */
export function Brand() {
  return (
    <>
      <span className="grid size-5 place-items-center rounded-sm bg-text-h font-mono text-xs font-semibold text-bg">
        C
      </span>
      <span className="font-mono text-brand font-semibold tracking-eyebrow text-text-h uppercase">
        Cinedex
      </span>
    </>
  );
}
