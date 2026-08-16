import type { ComponentProps } from 'react';
import { Separator as SeparatorPrimitive } from 'radix-ui';
import { cn } from '../utils/cn';

export type SeparatorProps = ComponentProps<typeof SeparatorPrimitive.Root>;

/**
 * A themed rule. Radix marks it `aria-hidden` when decorative (the default), so
 * it does not turn up as a landmark in the accessibility tree.
 */
export const Separator = ({
  className,
  orientation = 'horizontal',
  ...rest
}: SeparatorProps) => (
    <SeparatorPrimitive.Root
      orientation={orientation}
      className={cn(
        'bg-border/60',
        orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px',
        className,
      )}
      {...rest}
    />
  );
