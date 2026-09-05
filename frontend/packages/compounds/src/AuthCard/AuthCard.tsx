import type { FC, PropsWithChildren, ReactNode } from 'react';
import { Card } from '@cinedex/atoms';

export interface AuthCardProps {
  /**
   * The product mark and wordmark, rendered at the left of the row above the
   * card. Injected rather than hardcoded — that is the line between this package
   * and `@cinedex/solution`: this component knows *where* a brand goes,
   * `@cinedex/solution`'s `Brand` knows *which* brand.
   *
   * The one `ReactNode` on this component, and deliberately so: a brand is
   * markup, and the alternative is importing Cinedex into a brand-agnostic
   * package. Everything else here is typed by what callers actually pass.
   */
  brand?: ReactNode;
  /**
   * Right-aligned label in the brand row, e.g. "Step 2 of 2". Optional: a screen
   * with nothing to say there leaves the row to the brand alone.
   */
  eyebrow?: string;
  /**
   * Small uppercase mono label above the heading, e.g. "Catalog · Screens".
   * Optional, and most screens omit it: on a screen whose title already says it,
   * it is a second copy of the heading. `HomeScreen` is the case it exists for —
   * its title is the bare product name, so the kicker is the only thing saying
   * what the page lists.
   */
  kicker?: string;
  title: string;
  /**
   * The sentence under the heading. `string`, not `ReactNode`: it renders into a
   * `<p>` and every caller passes text.
   */
  description?: string;
  /** The line below the card. `string` for the same reason as `description`. */
  footnote?: string;
}

/**
 * The panel every auth screen is built on: a brand row, then a card whose header
 * carries a kicker, a title and an optional description, then the screen's own
 * content, then an optional footnote below.
 */
export const AuthCard: FC<PropsWithChildren<AuthCardProps>> = ({
  brand,
  eyebrow,
  kicker,
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
            <p className="m-0 mb-2 font-mono text-label font-semibold tracking-eyebrow text-text uppercase">
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
