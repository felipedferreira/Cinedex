import type { ComponentProps } from 'react';
import { Link, type LinkComponentProps } from '@tanstack/react-router';

const linkClass =
  'border-b border-accent-border text-accent no-underline hover:border-accent';

export function AuthLink(props: LinkComponentProps) {
  return (
    <Link {...props} className={`${linkClass} ${props.className ?? ''}`} />
  );
}

export function AuthActionLink({
  className,
  type = 'button',
  ...rest
}: ComponentProps<'button'>) {
  return (
    <button
      type={type}
      className={`cursor-pointer border-0 border-b bg-transparent p-0 font-mono ${linkClass} disabled:cursor-not-allowed disabled:opacity-45 ${className ?? ''}`}
      {...rest}
    />
  );
}
