import type { FC, PropsWithChildren } from 'react';
import { cn } from '@cinedex/atoms';

export type AuthLayoutProps = PropsWithChildren<{
  className?: string;
}>

/** Centres a narrow column in the viewport — the page frame every auth screen sits in. */
export const AuthLayout: FC<AuthLayoutProps> = ({ children, className }) => (
  <div
    className={cn(
      'flex min-h-svh items-center justify-center bg-bg p-6',
      className,
    )}
  >
    <div className="flex w-full max-w-[420px] flex-col gap-4">{children}</div>
  </div>
);
