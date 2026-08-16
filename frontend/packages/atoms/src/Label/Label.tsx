import type { ComponentProps } from 'react';
import { Label as LabelPrimitive } from 'radix-ui';
import { cn } from '../utils/cn';

export interface LabelProps extends ComponentProps<typeof LabelPrimitive.Root> {
  /** Switches to the warning ramp when the field it names is rejected. */
  tone?: 'default' | 'error';
}

/**
 * The uppercase mono field label. Radix's `Label` is the base so that clicking
 * the text focuses the control even when that control is not a native input —
 * `Checkbox` renders a button.
 */
export const Label = ({ tone = 'default', className, ...rest }: LabelProps) => (
    <LabelPrimitive.Root
      className={cn(
        'font-mono text-label font-medium tracking-label uppercase',
        tone === 'error' ? 'text-danger' : 'text-text',
        className,
      )}
      {...rest}
    />
  );
