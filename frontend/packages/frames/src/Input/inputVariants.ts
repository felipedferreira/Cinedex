import { cva, type VariantProps } from 'class-variance-authority';

/**
 * The single input surface in the Cinedex UI.
 *
 * Before the library split there were two — the CSS Modules `TextField` (6px
 * radius, 16px text) and the auth flow's Tailwind password input (4px radius,
 * 14px text) — and sign-in showed both, stacked. This is the auth design, which
 * the screens were drawn to; `TextField` now adopts it so Email and Password
 * finally match.
 */
export const inputVariants = cva(
  'w-full rounded-sm border px-3 py-[11px] text-sm text-text-h transition-colors hover:not-disabled:border-accent-border focus-visible:border-accent focus-visible:outline-2 focus-visible:outline-accent disabled:cursor-not-allowed disabled:opacity-50',
  {
    variants: {
      invalid: {
        true: 'border-danger-border bg-danger-bg',
        false: 'border-border-strong bg-bg',
      },
    },
    defaultVariants: {
      invalid: false,
    },
  },
);

export type InputVariantProps = VariantProps<typeof inputVariants>;
