import { createContext, useContext } from 'react';
import { AnchorLink } from './AnchorLink';
import type { SolutionLinkComponent } from './linkTypes';

export const LinkContext = createContext<SolutionLinkComponent>(AnchorLink);

/**
 * The link component the host injected.
 *
 * This is the whole of `@cinedex/solution`'s coupling to navigation. The screens
 * know Cinedex's route *paths* — those are Cinedex facts and belong here — but
 * not how to navigate them, so the package never imports a router and its
 * stories need no router mock.
 */
export function useLinkComponent(): SolutionLinkComponent {
  return useContext(LinkContext);
}
