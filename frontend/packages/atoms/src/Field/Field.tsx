import { useId, useMemo } from 'react';
import type { ReactNode } from 'react';
import { Label } from '../Label/Label';
import { cn } from '../utils/cn';
import { FieldContext } from './fieldContext';

export interface FieldProps {
  label: string;
  /** Rendered at the right of the label row, e.g. a "Forgot?" link. */
  labelExtra?: ReactNode;
  /** When set, the field renders as invalid and the message is announced. */
  error?: string;
  className?: string;
  children: ReactNode;
}

/**
 * The accessibility half of a form field: a label, a slot for the control, and
 * an optional error message, wired together with a generated id.
 *
 * The control does not have to be told any of that — `Input`, `PasswordInput`
 * and anything else calling `useFieldControl()` picks the id and `aria-*` up
 * from context, so the three can never disagree. When there is no error the
 * description is dropped entirely rather than pointing at an empty node.
 */
export function Field({
  label,
  labelExtra,
  error,
  className,
  children,
}: FieldProps) {
  const id = useId();
  const value = useMemo(
    () => ({
      id,
      errorId: error ? `${id}-error` : undefined,
      invalid: Boolean(error),
    }),
    [id, error],
  );

  return (
    <FieldContext value={value}>
      <div className={cn('flex flex-col gap-1.5 text-left', className)}>
        <span className="flex items-baseline justify-between gap-2.5">
          <Label htmlFor={id} tone={error ? 'error' : 'default'}>
            {label}
          </Label>
          {labelExtra}
        </span>
        {children}
        {error ? (
          <span
            id={value.errorId}
            className="font-mono text-caption text-danger"
          >
            {error}
          </span>
        ) : null}
      </div>
    </FieldContext>
  );
}
