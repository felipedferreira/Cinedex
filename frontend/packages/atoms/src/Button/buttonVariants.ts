import { cva, type VariantProps } from 'class-variance-authority';

/**
 * The four button looks in the Cinedex UI, and the three scales they come at.
 *
 * `variant` owns the surface, `size` owns the footprint, and they compose: the
 * auth screens' call-to-action is `solid` at `block`, the landing page's counter
 * is `primary` at `md`. `block` sets `display: block`, which `cn()` resolves
 * against the base `inline-flex` — last wins — so the two are not in conflict.
 *
 * Lives in its own file because `react-refresh/only-export-components` fires on
 * a module that exports both a component and something else.
 */
export const buttonVariants = cva(
  'inline-flex cursor-pointer items-center justify-center font-mono focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-2 disabled:cursor-not-allowed',
  {
    variants: {
      variant: {
        primary:
          'gap-2 rounded-md border-2 border-transparent bg-accent-bg text-accent transition-[border-color] duration-300 hover:not-disabled:border-accent-border disabled:opacity-50',
        ghost:
          'gap-2 rounded-md border-2 border-transparent bg-social-bg text-text-h transition-[border-color] duration-300 hover:not-disabled:shadow disabled:opacity-50',
        solid:
          'rounded-sm border border-text-h bg-text-h text-bg transition-colors hover:not-disabled:border-text hover:not-disabled:bg-text disabled:opacity-35',
        outline:
          'rounded-sm border border-border bg-transparent text-text transition-colors hover:not-disabled:border-text-h hover:not-disabled:text-text-h disabled:opacity-35',
      },
      size: {
        sm: 'px-2 py-[3px] text-sm',
        md: 'px-2.5 py-[5px] text-base',
        block:
          'block w-full px-3.5 py-3.5 text-center text-xs font-semibold tracking-label uppercase no-underline',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
);

export type ButtonVariantProps = VariantProps<typeof buttonVariants>;
export type ButtonVariant = NonNullable<ButtonVariantProps['variant']>;
export type ButtonSize = NonNullable<ButtonVariantProps['size']>;
