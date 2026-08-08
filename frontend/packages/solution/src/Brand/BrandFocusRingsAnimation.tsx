import { useId } from 'react';
import { renderFocusRingsFrame } from './animations';
import { MarkBody } from './MarkBody';
import { useDelayedReveal } from './useDelayedReveal';
import { useMarkAnimation } from './useMarkAnimation';

const DURATION_MS = 1200;
/** `renderFocusRingsFrame`'s wordP segment starts at t = 0.7 of the sequence. */
const WORDMARK_DELAY_MS = 840;

/**
 * `Brand`, plus the "focus rings" intro — the inverse order from
 * `BrandApertureAnimation`: ghost arcs surface out of darkness and converge,
 * the two rings rotate into register to form the "C", and only then does the
 * lens assembly grow in at the centre before the wordmark fades in. Runs once
 * on mount and ends on the exact same settled attributes `Brand` renders
 * statically.
 *
 * Not currently wired into any screen — `HomeScreen` uses
 * `BrandApertureAnimation`. This ships fully built and exported so it's a
 * one-line swap if that choice changes, and so it can be reviewed on its own
 * in Storybook.
 */
export function BrandFocusRingsAnimation() {
  const uid = useId();
  const rootRef = useMarkAnimation(renderFocusRingsFrame, DURATION_MS);
  const wordmarkRevealed = useDelayedReveal(WORDMARK_DELAY_MS);

  return (
    <>
      <MarkBody uid={uid} ref={rootRef} />
      <span
        className="font-mono text-brand font-semibold tracking-eyebrow text-text-h uppercase transition-opacity duration-300"
        style={{ opacity: wordmarkRevealed ? 1 : 0 }}
      >
        Cinedex
      </span>
    </>
  );
}
