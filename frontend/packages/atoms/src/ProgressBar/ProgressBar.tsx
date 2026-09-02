import type { FC } from 'react';
import { Progress as ProgressPrimitive } from 'radix-ui';
import { cn } from '../utils/cn';

export interface ProgressBarProps {
  /** Completion from 0 to 1. Values outside the range are clamped. */
  ratio: number;
  /** Accessible name, e.g. "Password strength". */
  label: string;
  className?: string;
}

/**
 * A determinate progress track.
 *
 * Radix's `Progress` supplies `role="progressbar"` and the `aria-value*` set,
 * which the hand-rolled `<span><i/></span>` this replaces did not have — the
 * password strength meter previously announced nothing at all.
 */
export const ProgressBar: FC<ProgressBarProps> = ({
  ratio,
  label,
  className,
}) => {
  const percent = Math.round(Math.min(Math.max(ratio, 0), 1) * 100);

  return (
    <ProgressPrimitive.Root
      value={percent}
      max={100}
      aria-label={label}
      className={cn(
        'block h-[9px] overflow-hidden rounded-full border border-border bg-border',
        className,
      )}
    >
      <ProgressPrimitive.Indicator
        className="block h-full rounded-full bg-accent transition-[width]"
        style={{ width: `${String(percent)}%` }}
      />
    </ProgressPrimitive.Root>
  );
};
