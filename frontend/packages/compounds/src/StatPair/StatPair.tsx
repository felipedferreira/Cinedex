import { cn } from '@cinedex/atoms';

export type StatTone = 'neutral' | 'warning' | 'success';

export interface Stat {
  value: string;
  label: string;
  tone?: StatTone;
}

const toneClass: Record<StatTone, string> = {
  neutral: 'bg-bg text-text-h',
  warning: 'bg-warning-bg text-warning',
  success: 'bg-success-bg text-success',
};

export interface StatPairProps {
  stats: [Stat, Stat];
}

/**
 * Two figures side by side in a hairline-divided grid — a countdown next to a
 * remaining-attempts count, say. The divider is the container's background
 * showing through a 1px gap, so it stays a single hairline at any zoom.
 */
export function StatPair({ stats }: StatPairProps) {
  return (
    <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xs border border-border bg-border">
      {stats.map((stat) => (
        <div
          key={stat.label}
          className={cn('px-3.5 py-3', toneClass[stat.tone ?? 'neutral'])}
        >
          <b className="block font-mono text-2xl leading-none font-semibold tracking-tight tabular-nums">
            {stat.value}
          </b>
          <span className="mt-1.5 block font-mono text-label font-medium tracking-label text-text uppercase">
            {stat.label}
          </span>
        </div>
      ))}
    </div>
  );
}
