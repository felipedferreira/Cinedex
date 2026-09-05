import { createContext, useContext } from 'react';
import { AnchorLink } from './AnchorLink';
import type { SceneLinkComponent } from './linkTypes';

export const LinkContext = createContext<SceneLinkComponent>(AnchorLink);

/**
 * The link component the host injected.
 *
 * This is the whole of `@cinedex/scenes`'s coupling to navigation. The screens
 * know Cinedex's route *paths* — those are Cinedex facts and belong here — but
 * not how to navigate them, so the package never imports a router and its
 * stories need no router mock.
 */
export function useLinkComponent(): SceneLinkComponent {
  return useContext(LinkContext);
}
