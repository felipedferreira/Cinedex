import { useId } from 'react';
import type { ComponentProps, ReactNode } from 'react';
import { Checkbox as CheckboxPrimitive } from 'radix-ui';
import { cn } from '../utils/cn';

export interface CheckboxProps extends Omit<
  ComponentProps<typeof CheckboxPrimitive.Root>,
  'id' | 'asChild'
> {
  label: ReactNode;
}

/**
 * A labelled checkbox.
 *
 * Radix renders a `<button role="checkbox">` rather than an `<input>`, which
 * replaces the `peer` + `sr-only` + `after:` CSS trick the app-local version
 * used to draw a custom box. The tick is a real `Indicator` child, so its
 * visibility follows `data-state` instead of a sibling selector.
 *
 * The name is wired with `aria-labelledby` rather than left to the `<label>`
 * alone: a `<label for>` does associate with a button (buttons are labelable),
 * but the accessible name of a button is computed from its contents first, and
 * this one has none.
 */
export const Checkbox = ({ label, className, ...rest }: CheckboxProps) => {
  const id = useId();
  const labelId = `${id}-label`;

  return (
    <span className="inline-flex items-start gap-2">
      <CheckboxPrimitive.Root
        id={id}
        aria-labelledby={labelId}
        className={cn(
          'mt-0.5 grid size-[15px] flex-none cursor-pointer place-items-center rounded-xs border border-border-strong bg-bg',
          'data-[state=checked]:border-text-h data-[state=checked]:bg-text-h',
          'focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-1',
          'disabled:cursor-not-allowed disabled:opacity-50',
          className,
        )}
        {...rest}
      >
        <CheckboxPrimitive.Indicator>
          <span
            aria-hidden="true"
            className="block h-[6px] w-[3px] translate-y-[-1px] rotate-45 border-r-[1.5px] border-b-[1.5px] border-bg"
          />
        </CheckboxPrimitive.Indicator>
      </CheckboxPrimitive.Root>
      <label
        id={labelId}
        htmlFor={id}
        className="cursor-pointer font-mono text-caption font-medium text-text select-none"
      >
        {label}
      </label>
    </span>
  );
};
