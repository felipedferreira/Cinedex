import type { FC, ComponentProps } from 'react';
import { cn } from '@cinedex/atoms';
import { SolutionLink } from './SolutionLink';
import type { SolutionLinkProps } from './linkTypes';

/**
 * `text-caption` is not decoration — it is the reason this link has a size at
 * all. Nothing else here sets a type step, so without it the link falls through
 * to `base.css`'s `:root` font (18px, 16px under 1024px). That is invisible
 * wherever a parent already sets one — "Create one" sits inside
 * `InlineActionRow`'s `text-caption` and looked correct — but "Forgot?" sits in
 * `Field`'s label row, which sets none, and rendered at 16px next to its own
 * 10px `PASSWORD` label. Stating the step here makes the link self-sufficient
 * rather than dependent on where it happens to be mounted.
 */
const inlineLinkClass =
  'border-b border-accent-border text-caption text-accent no-underline hover:border-accent';

/** An inline underlined link, navigating through whatever the host injected. */
export const AuthLink: FC<SolutionLinkProps> = ({
  to,
  className,
  children,
}) => {
  return (
    <SolutionLink to={to} className={cn(inlineLinkClass, className)}>
      {children}
    </SolutionLink>
  );
};

/**
 * The same look as `AuthLink`, but for an in-page action rather than navigation
 * — "Resend", "Start over". A real `<button>`, so it is keyboard-operable and
 * announced as a button rather than a link that goes nowhere.
 */
export const AuthActionLink: FC<ComponentProps<'button'>> = ({
  className,
  type = 'button',
  ...rest
}) => {
  return (
    <button
      type={type}
      className={cn(
        'cursor-pointer border-0 border-b bg-transparent p-0 font-mono disabled:cursor-not-allowed disabled:opacity-45',
        inlineLinkClass,
        className,
      )}
      {...rest}
    />
  );
};
