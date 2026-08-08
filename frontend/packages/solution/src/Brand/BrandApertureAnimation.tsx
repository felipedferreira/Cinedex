import { useId } from 'react';
import { renderApertureFrame } from './animations';
import { MarkBody } from './MarkBody';
import { useDelayedReveal } from './useDelayedReveal';
import { useMarkAnimation } from './useMarkAnimation';

const DURATION_MS = 1200;
/** `renderApertureFrame`'s wordP segment starts at t = 0.58 of the sequence. */
const WORDMARK_DELAY_MS = 700;

/**
 * `Brand`, plus the "lens aperture" intro: the iris opens from closed while
 * the assembly counter-rotates, the rings draw on around the gap, a glint
 * crosses the metal, then the wordmark fades in. Runs once on mount and ends
 * on the exact same settled attributes `Brand` renders statically — swapping
 * one for the other mid-flow (e.g. after `HomeScreen` first paints) shows no
 * pop.
 *
 * The one place this plays today is `HomeScreen`, the app's index/landing
 * screen — everywhere else uses the static `Brand`. `BrandFocusRingsAnimation`
 * is the alternate sequence, built and exported the same way.
 */
export function BrandApertureAnimation() {
  const uid = useId();
  const rootRef = useMarkAnimation(renderApertureFrame, DURATION_MS);
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
