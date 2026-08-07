import type { ReactNode } from 'react';

export interface AuthCardProps {
  /** Right-aligned label in the brand row, e.g. "CIN · Auth" or "Step 2 of 2". */
  eyebrow: string;
  /** Small uppercase mono label above the heading, e.g. "Session · Sign in". */
  kicker: string;
  kickerTone?: 'neutral' | 'warning' | 'success' | 'accent';
  title: string;
  description?: ReactNode;
  children: ReactNode;
  footnote?: ReactNode;
}

const kickerToneClass: Record<
  NonNullable<AuthCardProps['kickerTone']>,
  string
> = {
  neutral: 'text-text',
  warning: 'text-warning',
  success: 'text-success',
  accent: 'text-accent',
};

export function AuthCard({
  eyebrow,
  kicker,
  kickerTone = 'neutral',
  title,
  description,
  children,
  footnote,
}: AuthCardProps) {
  return (
    <div className="flex flex-col gap-4 text-text">
      <div className="flex items-center gap-2">
        <span className="grid size-5 place-items-center rounded-sm bg-text-h font-mono text-xs font-semibold text-bg">
          C
        </span>
        <span className="font-mono text-[11px] font-semibold tracking-[0.14em] text-text-h uppercase">
          Cinedex
        </span>
        <span className="ml-auto font-mono text-[10px] tracking-[0.1em] text-text uppercase">
          {eyebrow}
        </span>
      </div>

      <div className="flex flex-col gap-4 rounded-sm border border-border bg-bg px-[22px] py-5 shadow">
        <div className="border-b-2 border-text-h pb-3">
          <p
            className={`m-0 mb-2 font-mono text-[10px] font-semibold tracking-[0.14em] uppercase ${kickerToneClass[kickerTone]}`}
          >
            {kicker}
          </p>
          <h1 className="m-0 text-[25px] leading-[1.1] font-bold tracking-tight text-text-h">
            {title}
          </h1>
          {description ? (
            <p className="mt-2 mb-0 text-[13.5px] text-text">{description}</p>
          ) : null}
        </div>
        {children}
      </div>

      {footnote ? (
        <p className="m-0 font-mono text-[10.5px] leading-[1.5] text-text">
          {footnote}
        </p>
      ) : null}
    </div>
  );
}
