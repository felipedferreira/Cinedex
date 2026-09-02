import type { FC, ComponentProps } from 'react';
import { cn } from '../utils/cn';

export type CardProps = ComponentProps<'div'>;

/**
 * The raised surface: hairline border, page background, the themed shadow.
 *
 * Surface only — it owns no padding and no internal layout, so a compound can
 * lay its own content out without first undoing a default.
 */
export const Card: FC<CardProps> = ({ className, ...rest }) => {
  return (
    <div
      className={cn('rounded-sm border border-border bg-bg shadow', className)}
      {...rest}
    />
  );
};
