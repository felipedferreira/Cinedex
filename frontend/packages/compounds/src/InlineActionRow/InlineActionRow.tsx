import type { FC, PropsWithChildren, ReactNode } from 'react';
import { cn } from '@cinedex/atoms';

export type InlineActionRowProps = PropsWithChildren<{
  /** The action on the right, usually a link. */
  action: ReactNode;
  className?: string;
}>

/**
 * The prompt-and-action row that closes a card — "No account? · Create one".
 * Was inlined in three screens with the same six utilities each time.
 */
export const InlineActionRow: FC<InlineActionRowProps> = ({
  children,
  action,
  className,
}) => (
  <div
    className={cn(
      'flex items-baseline justify-between gap-2.5 border-t border-border/60 pt-3 font-mono text-caption text-text',
      className,
    )}
  >
    <span>{children}</span>
    {action}
  </div>
);
