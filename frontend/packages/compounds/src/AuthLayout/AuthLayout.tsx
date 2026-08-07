import type { ReactNode } from 'react';
import { cn } from '@cinedex/atoms';

export interface AuthLayoutProps {
  children: ReactNode;
  className?: string;
}

/** Centres a narrow column in the viewport — the page frame every auth screen sits in. */
export function AuthLayout({ children, className }: AuthLayoutProps) {
  return (
    <div
      className={cn(
        'flex min-h-svh items-center justify-center bg-bg p-6',
        className,
      )}
    >
      <div className="flex w-full max-w-[420px] flex-col gap-4">{children}</div>
    </div>
  );
}
