import { useState } from 'react';
import type { FC } from 'react';
import { Input, type InputProps } from '../Input/Input';
import { cn } from '../utils/cn';

export type PasswordInputProps = Omit<InputProps, 'type'>;

/**
 * A password input with an in-field reveal toggle.
 *
 * Hand-rolled rather than Radix's `unstable_PasswordToggleField` — that
 * primitive is still behind an `unstable_` prefix, and the whole of the
 * behaviour it would replace is the `useState` below.
 */
export const PasswordInput: FC<PasswordInputProps> = ({
  className,
  ...rest
}) => {
  const [visible, setVisible] = useState(false);

  return (
    <div className="relative">
      <Input
        type={visible ? 'text' : 'password'}
        className={cn('pr-16', className)}
        {...rest}
      />
      <button
        type="button"
        onClick={() => {
          setVisible((current) => !current);
        }}
        className="absolute top-1/2 right-2.5 -translate-y-1/2 cursor-pointer border-0 bg-transparent p-1 font-mono text-label font-medium tracking-[0.06em] text-text uppercase hover:text-text-h"
      >
        {visible ? 'Hide' : 'Show'}
      </button>
    </div>
  );
};
