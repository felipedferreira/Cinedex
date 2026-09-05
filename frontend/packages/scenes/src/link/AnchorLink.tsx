import type { FC } from 'react';
import type { SceneLinkProps } from './linkTypes';

/**
 * The default link: a plain anchor, mapping the `to`/`search` contract back to a
 * single `href`. What Storybook, tests and any host without a router get.
 */
export const AnchorLink: FC<SceneLinkProps> = ({ to, search, ...rest }) => {
  const query = search ? new URLSearchParams(search).toString() : '';

  return <a href={query ? `${to}?${query}` : to} {...rest} />;
};
