import { useId } from 'react';
import type { ComponentProps } from 'react';
import { cx } from '../utils/cx';
import styles from './TextField.module.css';

export interface TextFieldProps extends Omit<ComponentProps<'input'>, 'id'> {
  label: string;
  /** When set, the field renders as invalid and the message is announced. */
  error?: string;
}

/**
 * The form-input primitive: a label, an input and an optional error message,
 * wired together with a generated id so the label, `aria-describedby` and
 * `aria-invalid` always agree.
 */
export function TextField({
  label,
  error,
  className,
  ...rest
}: TextFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className={cx(styles.input, error && styles.invalid, className)}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...rest}
      />
      {error ? (
        <p className={styles.error} id={errorId}>
          {error}
        </p>
      ) : null}
    </div>
  );
}
