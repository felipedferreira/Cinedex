import type { ComponentType, PropsWithChildren } from 'react';

export interface SolutionLinkProps extends PropsWithChildren {
  /** Destination path, e.g. `/forgot-password`. Pathname only — see `search`. */
  to: string;
  /**
   * Query parameters, kept separate from `to` rather than written into it.
   * Router link components match `to` against their own route table, so a
   * `to="/login?state=locked"` would not resolve; they take search state as its
   * own prop, and `AnchorLink` serialises it back onto the href.
   */
  search?: Record<string, string>;
  className?: string;
}

/**
 * The shape every screen navigates through. A `to` prop rather than `href`,
 * because that is what router link components take; `AnchorLink` adapts it back
 * to an anchor for hosts without one.
 */
export type SolutionLinkComponent = ComponentType<SolutionLinkProps>;
