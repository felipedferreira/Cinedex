import { useId, useState } from 'react';
import type { ComponentProps, ReactNode } from 'react';

export interface PasswordFieldProps extends Omit<
  ComponentProps<'input'>,
  'id' | 'type'
> {
  label: string;
  /** Rendered at the right of the label row, e.g. a "Forgot?" link. */
  labelExtra?: ReactNode;
  /** Overrides the label's tone/text for a rejected/invalid state, e.g. "New password — rejected". */
  error?: string;
}

export function PasswordField({
  label,
  labelExtra,
  error,
  className,
  ...rest
}: PasswordFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;
  const [visible, setVisible] = useState(false);

  return (
    <div className="flex flex-col gap-1.5 text-left">
      <span className="flex items-baseline justify-between gap-2.5">
        <label
          htmlFor={id}
          className={`font-mono text-[10px] font-medium tracking-[0.1em] uppercase ${error ? 'text-warning' : 'text-text'}`}
        >
          {label}
        </label>
        {labelExtra}
      </span>
      <div className="relative">
        <input
          id={id}
          type={visible ? 'text' : 'password'}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={`w-full rounded-sm border py-[11px] pr-16 pl-3 text-sm text-text-h transition-colors hover:border-accent-border focus-visible:border-accent focus-visible:outline-2 focus-visible:outline-accent disabled:cursor-not-allowed disabled:opacity-50 ${
            error
              ? 'border-warning-border bg-warning-bg'
              : 'border-border bg-bg'
          } ${className ?? ''}`}
          {...rest}
        />
        <button
          type="button"
          onClick={() => {
            setVisible((current) => !current);
          }}
          className="absolute top-1/2 right-2.5 -translate-y-1/2 cursor-pointer border-0 bg-transparent p-1 font-mono text-[10px] font-medium tracking-[0.06em] text-text uppercase hover:text-text-h"
        >
          {visible ? 'Hide' : 'Show'}
        </button>
      </div>
      {error ? (
        <span id={errorId} className="font-mono text-[11.5px] text-warning">
          {error}
        </span>
      ) : null}
    </div>
  );
}
