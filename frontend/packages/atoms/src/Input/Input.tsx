import type { ComponentProps } from 'react';
import { useFieldControl } from '../Field/fieldContext';
import { cn } from '../utils/cn';
import { inputVariants } from './inputVariants';

export type InputProps = Omit<ComponentProps<'input'>, 'id'>;

/**
 * The bare text-input surface. Inside a `Field` it picks up that field's id and
 * `aria-*` from context; outside one it is just a styled input.
 */
export const Input = ({ className, ...rest }: InputProps) => {
  const field = useFieldControl();

  return (
    <input
      className={cn(
        inputVariants({ invalid: field['aria-invalid'] ?? false }),
        className,
      )}
      {...field}
      {...rest}
    />
  );
};
