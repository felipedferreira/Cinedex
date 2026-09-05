import type { FC, PropsWithChildren } from 'react';
import { AnchorLink } from './AnchorLink';
import { LinkContext } from './linkContext';
import type { SceneLinkComponent } from './linkTypes';

export interface SceneProviderProps {
  /**
   * The component screens render their internal navigation with. Defaults to a
   * plain `<a>`; a host with a router passes its own `Link`.
   */
  linkComponent?: SceneLinkComponent;
}

/**
 * Wraps the app once, at the root, to tell `@cinedex/scenes`'s screens how to
 * navigate. Without it they still render — links just become ordinary anchors,
 * which is what Storybook and the tests want anyway.
 */
export const SceneProvider: FC<PropsWithChildren<SceneProviderProps>> = ({
  linkComponent = AnchorLink,
  children,
}) => {
  return <LinkContext value={linkComponent}>{children}</LinkContext>;
};
