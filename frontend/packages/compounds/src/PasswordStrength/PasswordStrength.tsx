import { ProgressBar } from '@cinedex/atoms';
import type { PasswordRequirement } from './strengthFromRequirements';

export interface PasswordStrengthMeterProps {
  /** Completion from 0 to 1, as returned by `strengthFromRequirements`. */
  ratio: number;
  /** The verdict shown beside the bar, e.g. "Weak". */
  label: string;
}

/** The strength bar and its verdict, side by side. */
export function PasswordStrengthMeter({
  ratio,
  label,
}: PasswordStrengthMeterProps) {
  return (
    <div className="flex items-center gap-2.5">
      <ProgressBar ratio={ratio} label="Password strength" className="flex-1" />
      <span className="font-mono text-label font-semibold tracking-label text-accent uppercase">
        {label}
      </span>
    </div>
  );
}

export interface PasswordChecklistProps {
  requirements: PasswordRequirement[];
}

/** The tick-list of password requirements, met and unmet. */
export function PasswordChecklist({ requirements }: PasswordChecklistProps) {
  return (
    <ul className="m-0 mt-0.5 grid list-none gap-1.5 p-0">
      {requirements.map((requirement) => (
        <li
          key={requirement.label}
          className="flex items-center gap-2 font-mono text-caption text-text"
        >
          <i
            aria-hidden="true"
            className={`relative grid size-[13px] flex-none place-items-center rounded-xs ${
              requirement.met ? 'bg-accent' : 'border border-border bg-bg'
            }`}
          >
            {requirement.met ? (
              <span className="block h-[6px] w-[3px] translate-y-[-1px] rotate-45 border-r-[1.5px] border-b-[1.5px] border-bg" />
            ) : null}
          </i>
          {requirement.label}
        </li>
      ))}
    </ul>
  );
}
