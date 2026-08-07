import { createContext, useContext } from 'react';

export interface FieldContextValue {
  /** The generated id the control must carry so the label's `htmlFor` matches. */
  id: string;
  /** Set only when the field has an error, so no control points at an empty node. */
  errorId?: string;
  invalid: boolean;
}

/**
 * Lets `Field` hand its generated id and error wiring to whatever control sits
 * inside it, so `htmlFor`, `aria-describedby` and `aria-invalid` cannot
 * disagree. `null` when a control is rendered outside a `Field`, which is
 * allowed — the control then carries whatever ids the caller passes.
 */
export const FieldContext = createContext<FieldContextValue | null>(null);

/**
 * The accessibility props a control should spread onto its element. Returns an
 * empty object outside a `Field`.
 */
export function useFieldControl() {
  const field = useContext(FieldContext);
  if (!field) return {};

  return {
    id: field.id,
    'aria-invalid': field.invalid ? (true as const) : undefined,
    'aria-describedby': field.errorId,
  };
}
