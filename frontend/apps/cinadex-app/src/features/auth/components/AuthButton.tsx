import type { ComponentProps } from 'react';
import {
  authButtonClassName,
  type AuthButtonVariant,
} from './authButtonClassName';

export interface AuthButtonProps extends ComponentProps<'button'> {
  variant?: AuthButtonVariant;
}

/**
 * The block-width, uppercase-mono call-to-action button every auth screen
 * uses. Not the shared library's `Button` — that component's tinted
 * "primary" variant doesn't match this design's solid ink-fill CTA, and
 * `@cinedex/components` stays CSS-Modules-only by convention.
 */
export function AuthButton({
  variant = 'solid',
  type = 'button',
  className,
  ...rest
}: AuthButtonProps) {
  return (
    <button
      type={type}
      className={authButtonClassName(variant, className)}
      {...rest}
    />
  );
}
