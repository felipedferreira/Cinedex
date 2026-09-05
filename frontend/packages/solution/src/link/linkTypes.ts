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
/**
 * The one component-type port in the frontend, and it cannot be a `ReactNode`
 * slot: this package renders N links at positions only it knows, each with its
 * own `to`, so an already-built element cannot serve. `SolutionProvider`
 * supplies a working `AnchorLink` default, which is what lets these screens
 * render router-free in Storybook and in tests, and `SolutionProvider.test.tsx`
 * pins the boundary by injecting a button-based link and asserting no `<a>`
 * survives.
 *
 * `no-restricted-syntax` bans `ComponentType` on a *prop*; naming the contract
 * here is the deliberate way to declare one, not a way around the rule.
 */
export type SolutionLinkComponent = ComponentType<SolutionLinkProps>;
