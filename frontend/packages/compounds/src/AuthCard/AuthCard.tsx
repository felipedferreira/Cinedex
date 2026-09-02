import type { FC, PropsWithChildren, ReactNode } from 'react';
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
  /**
   * Right-aligned label in the brand row, e.g. "Step 2 of 2". Optional: a screen
   * with nothing to say there leaves the row to the brand alone.
   */
  eyebrow?: string;
  /**
   * Small uppercase mono label above the heading, e.g. "Catalog · Screens".
   * Optional for the same reason, and worth omitting rather than filling: on a
   * screen whose title already says it, it is a second copy of the heading.
   */
  kicker?: string;
  kickerTone?: AuthCardKickerTone;
  title: string;
  description?: ReactNode;
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
export const AuthCard: FC<PropsWithChildren<AuthCardProps>> = ({
  brand,
  eyebrow,
  kicker,
  kickerTone = 'neutral',
  title,
  description,
  children,
  footnote,
}) => {
  return (
    <div className="flex flex-col gap-4 text-text">
      <div className="flex items-center gap-2">
        {brand}
        {eyebrow ? (
          <span className="ml-auto font-mono text-[10px] tracking-label text-text uppercase">
            {eyebrow}
          </span>
        ) : null}
      </div>

      <Card className="flex flex-col gap-4 px-[22px] py-5">
        <div className="border-b-2 border-text-h pb-3">
          {kicker ? (
            <p
              className={cn(
                'm-0 mb-2 font-mono text-label font-semibold tracking-eyebrow uppercase',
                kickerToneClass[kickerTone],
              )}
            >
              {kicker}
            </p>
          ) : null}
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
};
