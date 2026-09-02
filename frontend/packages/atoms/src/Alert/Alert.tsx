import type { FC, ComponentProps } from 'react';
import { cn } from '../utils/cn';
import { alertVariants, type AlertVariantProps } from './alertVariants';

export interface AlertProps extends ComponentProps<'div'>, AlertVariantProps {}

/**
 * An inline status message. `role="status"` announces changes politely, which
 * suits the lockout and rate-limit copy these carry — a live region, not an
 * interruption.
 */
export const Alert: FC<AlertProps> = ({ tone, className, ...rest }) => {
  return (
    <div
      role="status"
      className={cn(alertVariants({ tone }), className)}
      {...rest}
    />
  );
};
