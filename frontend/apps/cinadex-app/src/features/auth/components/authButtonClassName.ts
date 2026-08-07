export type AuthButtonVariant = 'solid' | 'outline';

const variantClass: Record<AuthButtonVariant, string> = {
  solid: 'border-text-h bg-text-h text-bg hover:bg-text hover:border-text',
  outline:
    'border-border bg-transparent text-text hover:border-text-h hover:text-text-h',
};

const baseClass =
  'block w-full cursor-pointer rounded-sm border px-3.5 py-3.5 text-center font-mono text-xs font-semibold tracking-[0.1em] uppercase no-underline transition-colors disabled:cursor-not-allowed disabled:opacity-35';

/** Shared styling for the block-width CTA, usable on a `<button>` or, via `AuthLink`, an `<a>`. */
export function authButtonClassName(
  variant: AuthButtonVariant = 'solid',
  className = '',
) {
  return `${baseClass} ${variantClass[variant]} ${className}`;
}
