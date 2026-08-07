import type { ComponentType, ReactNode } from 'react';

export interface SolutionLinkProps {
  /** Destination path, e.g. `/forgot-password`. */
  to: string;
  className?: string;
  children?: ReactNode;
}

/**
 * The shape every screen navigates through. A `to` prop rather than `href`,
 * because that is what router link components take; `AnchorLink` adapts it back
 * to an anchor for hosts without one.
 */
export type SolutionLinkComponent = ComponentType<SolutionLinkProps>;
