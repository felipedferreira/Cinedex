import type { ReactNode } from 'react';
import { Card, cn } from '@cinedex/atoms';

export type AuthCardKickerTone = 'neutral' | 'warning' | 'success' | 'accent';

export interface AuthCardProps {
  /**
   * The product mark and wordmark, rendered at the left of the row above the
   * card. Injected rather than hardcoded — that is the line between this package
   * and `@cinedex/solution`: this component knows *where* a brand goes,
   * `@cinedex/solution`'s `Brand` knows *which* brand.
   */
  brand?: ReactNode;
  /** Right-aligned label in the brand row, e.g. "CIN · Auth" or "Step 2 of 2". */
  eyebrow: string;
  /** Small uppercase mono label above the heading, e.g. "Session · Sign in". */
  kicker: string;
  kickerTone?: AuthCardKickerTone;
  title: string;
  description?: ReactNode;
  children: ReactNode;
  footnote?: ReactNode;
}

const kickerToneClass: Record<AuthCardKickerTone, string> = {
  neutral: 'text-text',
  warning: 'text-warning',
  success: 'text-success',
  accent: 'text-accent',
};

/**
 * The panel every auth screen is built on: a brand row, then a card whose header
 * carries a kicker, a title and an optional description, then the screen's own
 * content, then an optional footnote below.
 */
export function AuthCard({
  brand,
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
        {brand}
        <span className="ml-auto font-mono text-[10px] tracking-label text-text uppercase">
          {eyebrow}
        </span>
      </div>

      <Card className="flex flex-col gap-4 px-[22px] py-5">
        <div className="border-b-2 border-text-h pb-3">
          <p
            className={cn(
              'm-0 mb-2 font-mono text-label font-semibold tracking-eyebrow uppercase',
              kickerToneClass[kickerTone],
            )}
          >
            {kicker}
          </p>
          <h1 className="m-0 text-title leading-[1.1] font-bold tracking-tight text-text-h">
            {title}
          </h1>
          {description ? (
            <p className="mt-2 mb-0 text-body text-text">{description}</p>
          ) : null}
        </div>
        {children}
      </Card>

      {footnote ? (
        <p className="m-0 font-mono text-footnote leading-[1.5] text-text">
          {footnote}
        </p>
      ) : null}
    </div>
  );
}
