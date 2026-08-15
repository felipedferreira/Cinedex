import { useId } from 'react';
import { MarkBody } from './MarkBody';
import { buildApertureTimeline } from './timelines';
import { useMarkTimeline } from './useMarkTimeline';
import { Wordmark } from './Wordmark';

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
export function BrandApertureAnimation() {
  const uid = useId();
  const { rootRef, wordmarkRef } = useMarkTimeline(buildApertureTimeline);

  return (
    <>
      <MarkBody uid={uid} ref={rootRef} />
      <Wordmark ref={wordmarkRef} />
    </>
  );
}
