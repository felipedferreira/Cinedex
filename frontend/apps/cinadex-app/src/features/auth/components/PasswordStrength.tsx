import type { PasswordRequirement } from './strengthFromRequirements';

export function PasswordStrengthMeter({
  ratio,
  label,
}: {
  ratio: number;
  label: string;
}) {
  return (
    <div className="flex items-center gap-2.5">
      <span className="block h-[9px] flex-1 overflow-hidden rounded-full border border-border bg-border">
        <i
          className="block h-full rounded-full bg-accent transition-[width]"
          style={{ width: `${String(Math.round(ratio * 100))}%` }}
        />
      </span>
      <span className="font-mono text-[10px] font-semibold tracking-[0.1em] text-accent uppercase">
        {label}
      </span>
    </div>
  );
}

export function PasswordChecklist({
  requirements,
}: {
  requirements: PasswordRequirement[];
}) {
  return (
    <ul className="m-0 mt-0.5 grid list-none gap-1.5 p-0">
      {requirements.map((requirement) => (
        <li
          key={requirement.label}
          className="flex items-center gap-2 font-mono text-[11.5px] text-text"
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
