import type { FC } from 'react';
import { AnchorLink } from './AnchorLink';
import { LinkContext } from './linkContext';
import type { SolutionLinkComponent } from './linkTypes';

export interface SolutionProviderProps {
  /**
   * The component screens render their internal navigation with. Defaults to a
   * plain `<a>`; a host with a router passes its own `Link`.
   */
  linkComponent?: SolutionLinkComponent;
  children?: React.ReactNode;
}

/**
 * Wraps the app once, at the root, to tell `@cinedex/solution`'s screens how to
 * navigate. Without it they still render — links just become ordinary anchors,
 * which is what Storybook and the tests want anyway.
 */
export const SolutionProvider: FC<SolutionProviderProps> = ({
  linkComponent = AnchorLink,
  children,
}) => (
  <LinkContext value={linkComponent}>{children}</LinkContext>
);
