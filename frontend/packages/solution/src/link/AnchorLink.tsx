import type { SolutionLinkProps } from './linkTypes';

/**
 * The default link: a plain anchor, mapping the `to` contract back to `href`.
 * What Storybook, tests and any host without a router get.
 */
export function AnchorLink({ to, ...rest }: SolutionLinkProps) {
  return <a href={to} {...rest} />;
}
