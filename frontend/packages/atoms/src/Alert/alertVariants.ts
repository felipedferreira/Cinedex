import { cva, type VariantProps } from 'class-variance-authority';

/**
 * A status panel: hairline border all round, a thicker left rule in the tone's
 * foreground colour. Each tone draws its three colours from one ramp in
 * `@cinedex/theme` rather than from ad-hoc opacities.
 */
export const alertVariants = cva(
  'rounded-xs border border-l-[3px] px-3.5 py-3 text-note leading-[1.4]',
  {
    variants: {
      tone: {
        neutral: 'border-border border-l-text-h bg-bg text-text',
        warning:
          'border-warning-border border-l-warning bg-warning-bg text-warning',
      },
    },
    defaultVariants: {
      tone: 'neutral',
    },
  },
);

export type AlertVariantProps = VariantProps<typeof alertVariants>;
export type AlertTone = NonNullable<AlertVariantProps['tone']>;
