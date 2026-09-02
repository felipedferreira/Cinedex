import type { FC, ReactNode } from 'react';
import { Field, PasswordInput, type PasswordInputProps } from '@cinedex/atoms';

export interface PasswordFieldProps extends PasswordInputProps {
  label: string;
  /** Rendered at the right of the label row, e.g. a "Forgot?" link. */
  labelExtra?: ReactNode;
  /** Overrides the label's tone and states the reason, e.g. "New password — rejected". */
  error?: string;
}

/**
 * A password field: the label/error wiring of `Field` around the masked input
 * and its reveal toggle. The counterpart to `TextField`, which pairs `Field`
 * with a plain `Input`.
 */
export const PasswordField: FC<PasswordFieldProps> = ({
  label,
  labelExtra,
  error,
  ...rest
}) => {
  return (
    <Field label={label} labelExtra={labelExtra} error={error}>
      <PasswordInput {...rest} />
    </Field>
  );
};
