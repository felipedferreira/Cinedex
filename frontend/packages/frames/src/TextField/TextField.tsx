import type { FC, ReactNode } from 'react';
import { Field } from '../Field/Field';
import { Input, type InputProps } from '../Input/Input';

export interface TextFieldProps extends InputProps {
  label: string;
  /** Rendered at the right of the label row, e.g. a "Forgot?" link. */
  labelExtra?: ReactNode;
  /** When set, the field renders as invalid and the message is announced. */
  error?: string;
}

/**
 * The everyday form field: `Field` + `Input`, which is the pairing almost every
 * caller wants. Reach for the two separately only when the control is not a
 * plain input — `PasswordField` in `@cinedex/shots` is the example.
 */
export const TextField: FC<TextFieldProps> = ({
  label,
  labelExtra,
  error,
  ...rest
}) => {
  return (
    <Field label={label} labelExtra={labelExtra} error={error}>
      <Input {...rest} />
    </Field>
  );
};
