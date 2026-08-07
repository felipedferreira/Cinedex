import { useId } from 'react';
import type { ComponentProps, ReactNode } from 'react';

export interface CheckboxProps extends Omit<
  ComponentProps<'input'>,
  'type' | 'id'
> {
  label: ReactNode;
}

export function Checkbox({ label, className, ...rest }: CheckboxProps) {
  const id = useId();

  return (
    <label
      htmlFor={id}
      className={`inline-flex cursor-pointer items-start gap-2 font-mono text-[11.5px] font-medium text-text select-none ${className ?? ''}`}
    >
      <input id={id} type="checkbox" className="peer sr-only" {...rest} />
      <span
        aria-hidden="true"
        className="relative mt-0.5 grid size-[15px] flex-none place-items-center rounded-xs border border-border bg-bg
          after:absolute after:h-[6px] after:w-[3px] after:translate-y-[-1px] after:rotate-45 after:border-r-[1.5px] after:border-b-[1.5px] after:border-bg after:opacity-0 after:content-['']
          peer-checked:border-text-h peer-checked:bg-text-h peer-checked:after:opacity-100
          peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-accent peer-focus-visible:outline-offset-1"
      />
      {label}
    </label>
  );
}
