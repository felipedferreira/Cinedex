import type { ComponentProps } from 'react';
import { cn } from '@cinedex/atoms';
import { SolutionLink } from './SolutionLink';
import type { SolutionLinkProps } from './linkTypes';

const inlineLinkClass =
  'border-b border-accent-border text-accent no-underline hover:border-accent';

/** An inline underlined link, navigating through whatever the host injected. */
export function AuthLink({ to, className, children }: SolutionLinkProps) {
  return (
    <SolutionLink to={to} className={cn(inlineLinkClass, className)}>
      {children}
    </SolutionLink>
  );
}

/**
 * The same look as `AuthLink`, but for an in-page action rather than navigation
 * — "Resend", "Start over". A real `<button>`, so it is keyboard-operable and
 * announced as a button rather than a link that goes nowhere.
 */
export function AuthActionLink({
  className,
  type = 'button',
  ...rest
}: ComponentProps<'button'>) {
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
}
