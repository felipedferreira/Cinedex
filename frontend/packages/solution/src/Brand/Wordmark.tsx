import type { Ref } from 'react';

export interface WordmarkProps {
  ref?: Ref<HTMLSpanElement>;
}

/**
 * The product's name, set in the brand lockup's type. `Brand` renders it with
 * no `ref` and it simply sits there; the two animated variants pass one so
 * their GSAP timeline can fade it in as the last beat of the sequence.
 *
 * Rendered at full opacity — the settled state, matching `MarkBody`'s own
 * convention — so a pre-hydration or no-JS render shows a complete lockup. The
 * animated variants rewind it to 0 in a layout effect, before first paint.
 *
 * Kept as plain text rather than the hand-drawn SVG paths the source artifact
 * also produced: at this size, next to an eyebrow label, text keeps the design
 * system's type scale (`text-brand`) and the screen-reader semantics a
 * geometric wordmark would have to reinvent, for no legibility gain.
 */
export function Wordmark({ ref }: WordmarkProps) {
  return (
    <span
      ref={ref}
      className="font-mono text-brand font-semibold tracking-eyebrow text-text-h uppercase"
    >
      Cinedex
    </span>
  );
}
