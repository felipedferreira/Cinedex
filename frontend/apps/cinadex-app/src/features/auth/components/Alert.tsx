import type { ReactNode } from 'react';

export interface AlertProps {
  tone?: 'neutral' | 'warning' | 'success';
  children: ReactNode;
}

const toneClass: Record<NonNullable<AlertProps['tone']>, string> = {
  neutral: 'border-border border-l-text-h bg-bg text-text',
  warning: 'border-warning-border border-l-warning bg-warning-bg text-warning',
  success: 'border-success-border border-l-success bg-success-bg text-success',
};

export function Alert({ tone = 'neutral', children }: AlertProps) {
  return (
    <div
      role="status"
      className={`rounded-xs border border-l-[3px] px-3.5 py-3 text-[12.5px] leading-[1.4] ${toneClass[tone]}`}
    >
      {children}
    </div>
  );
}
