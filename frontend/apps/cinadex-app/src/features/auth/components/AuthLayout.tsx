import type { ReactNode } from 'react';

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-svh items-center justify-center bg-bg p-6">
      <div className="flex w-full max-w-[420px] flex-col gap-4">{children}</div>
    </div>
  );
}
