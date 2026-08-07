export interface Stat {
  value: string;
  label: string;
  tone?: 'neutral' | 'warning' | 'success';
}

const toneClass: Record<NonNullable<Stat['tone']>, string> = {
  neutral: 'bg-bg text-text-h',
  warning: 'bg-warning-bg text-warning',
  success: 'bg-success-bg text-success',
};

export function StatPair({ stats }: { stats: [Stat, Stat] }) {
  return (
    <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xs border border-border bg-border">
      {stats.map((stat) => (
        <div
          key={stat.label}
          className={`px-3.5 py-3 ${toneClass[stat.tone ?? 'neutral']}`}
        >
          <b className="block font-mono text-2xl leading-none font-semibold tracking-tight tabular-nums">
            {stat.value}
          </b>
          <span className="mt-1.5 block font-mono text-[10px] font-medium tracking-[0.1em] text-text uppercase">
            {stat.label}
          </span>
        </div>
      ))}
    </div>
  );
}
