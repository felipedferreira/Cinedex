import type { ComponentProps } from 'react';
import { Slot } from 'radix-ui';
import { cn } from '../utils/cn';
import { buttonVariants, type ButtonVariantProps } from './buttonVariants';

export interface ButtonProps
  extends ComponentProps<'button'>, ButtonVariantProps {
  /**
   * Render the caller's child element instead of a `<button>`, keeping the
   * styling. This is how a router `<Link>` gets button treatment without an
   * `<a>` nested in a `<button>`:
   *
   * ```tsx
   * <Button asChild variant="outline" size="block">
   *   <Link to="/forgot-password">Reset password instead</Link>
   * </Button>
   * ```
   */
  asChild?: boolean;
}

/**
 * The action primitive. Defaults to `type="button"` so it never submits a form
 * by accident — pass `type="submit"` explicitly when that is what you want.
 */
export function Button({
  variant,
  size,
  asChild = false,
  type = 'button',
  className,
  ...rest
}: ButtonProps) {
  const classes = cn(buttonVariants({ variant, size }), className);

  if (asChild) {
    // `type` is meaningless on whatever element the caller slots in (usually an
    // anchor), so it is deliberately not forwarded here.
    return <Slot.Root className={classes} {...rest} />;
  }

  return <button type={type} className={classes} {...rest} />;
}
