import type { FC } from 'react';
/* eslint-disable react-hooks/static-components --
 * The link component is not created by this render: it comes from context, and
 * both the value `SceneProvider` takes as a prop and the `AnchorLink` default
 * are declared at module level. Its identity is stable across renders, so React
 * never remounts the subtree. The rule flags any component-valued local and
 * cannot tell a constant context value from one built inline.
 *
 * This file exists to hold that exemption: it is the only place in the package
 * that reads `LinkContext`, so the disable covers one component rather than
 * being repeated at every call site.
 */
import { useLinkComponent } from './linkContext';
import type { SceneLinkProps } from './linkTypes';

/**
 * Renders whatever link component the host injected. Everything in this package
 * that navigates does so through here.
 */
export const SceneLink: FC<SceneLinkProps> = ({
  to,
  search,
  className,
  children,
}) => {
  const Link = useLinkComponent();

  return (
    <Link to={to} search={search} className={className}>
      {children}
    </Link>
  );
};
