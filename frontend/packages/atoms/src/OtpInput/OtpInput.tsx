import { useRef, useState } from 'react';
import type { ClipboardEvent, KeyboardEvent } from 'react';
import { cn } from '../utils/cn';

export interface OtpInputProps {
  length?: number;
  value: string;
  onChange: (value: string) => void;
  label: string;
  className?: string;
}

/**
 * A one-time-code field: one box per digit, behaving like a single input.
 *
 * Deliberately hand-rolled. Radix ships `unstable_OneTimePasswordField`, which
 * would also bring password-manager autofill and auto-submit, but it is a
 * preview API behind an `unstable_` prefix with an open issue on value
 * persistence — worth adopting once it stabilises, not before.
 */
export function OtpInput({
  length = 6,
  value,
  onChange,
  label,
  className,
}: OtpInputProps) {
  const [focusedIndex, setFocusedIndex] = useState<number | null>(null);
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const digits = Array.from({ length }, (_, index) => value[index] ?? '');

  function setDigit(index: number, digit: string) {
    const next = digits.slice();
    next[index] = digit;
    onChange(next.join('').slice(0, length));
  }

  function handleChange(index: number, raw: string) {
    const digit = raw.replace(/\D/g, '').slice(-1);
    setDigit(index, digit);
    if (digit && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  }

  function handleKeyDown(
    index: number,
    event: KeyboardEvent<HTMLInputElement>,
  ) {
    if (event.key === 'Backspace' && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
      setDigit(index - 1, '');
    }
  }

  function handlePaste(event: ClipboardEvent<HTMLInputElement>) {
    const pasted = event.clipboardData
      .getData('text')
      .replace(/\D/g, '')
      .slice(0, length);
    if (!pasted) return;
    event.preventDefault();
    onChange(pasted);
    inputRefs.current[Math.min(pasted.length, length - 1)]?.focus();
  }

  return (
    <div
      role="group"
      aria-label={label}
      className={cn('grid gap-2', className)}
      // Driven by `length` rather than a fixed `grid-cols-6`, so a code of a
      // different length still lays out in one row.
      style={{
        gridTemplateColumns: `repeat(${String(length)}, minmax(0, 1fr))`,
      }}
    >
      {digits.map((digit, index) => {
        const isNextEmpty = digit === '' && focusedIndex === index;
        return (
          <div key={index} className="relative">
            <input
              ref={(el) => {
                inputRefs.current[index] = el;
              }}
              value={digit}
              onChange={(event) => {
                handleChange(index, event.target.value);
              }}
              onKeyDown={(event) => {
                handleKeyDown(index, event);
              }}
              onPaste={handlePaste}
              onFocus={() => {
                setFocusedIndex(index);
              }}
              onBlur={() => {
                setFocusedIndex((current) =>
                  current === index ? null : current,
                );
              }}
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={1}
              aria-label={`Digit ${String(index + 1)} of ${String(length)}`}
              className={cn(
                'h-14 w-full rounded-sm border text-center font-mono text-xl font-semibold text-text-h focus-visible:outline-2 focus-visible:outline-accent',
                isNextEmpty
                  ? 'border-accent bg-accent-bg'
                  : 'border-border bg-bg',
              )}
            />
            {isNextEmpty ? (
              <i
                aria-hidden="true"
                className="animate-caret pointer-events-none absolute top-1/2 left-1/2 block h-6 w-[1.5px] -translate-x-1/2 -translate-y-1/2 bg-accent"
              />
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
