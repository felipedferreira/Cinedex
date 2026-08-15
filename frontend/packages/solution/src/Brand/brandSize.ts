/** The named display scales supported by every Cinedex brand lockup. */
export const BRAND_SIZES = ['XS', 'S', 'M', 'L', 'XL'] as const;

export type NamedBrandSize = (typeof BRAND_SIZES)[number];

/**
 * A brand scale may be a named size or a positive whole-number scale step.
 * Numeric steps 1–5 are aliases for `XS`–`XL`; higher steps continue the
 * scale beyond `XL`.
 */
export type BrandSize = NamedBrandSize | number;

interface NamedBrandSizeClasses {
  markClassName: string;
  wordmarkClassName: string;
}

export interface ResolvedBrandSize extends NamedBrandSizeClasses {
  markStyle?: { width: string; height: string };
  wordmarkStyle?: { fontSize: string };
}

/** `XS` preserves the lockup dimensions that existed before sizing. */
const NAMED_SIZE_CLASSES: Record<NamedBrandSize, NamedBrandSizeClasses> = {
  XS: { markClassName: 'size-5', wordmarkClassName: 'text-brand' },
  S: { markClassName: 'size-6', wordmarkClassName: 'text-xs' },
  M: { markClassName: 'size-7', wordmarkClassName: 'text-sm' },
  L: { markClassName: 'size-8', wordmarkClassName: 'text-base' },
  XL: { markClassName: 'size-10', wordmarkClassName: 'text-xl' },
};

/** One through five are exact aliases for the corresponding named sizes. */
const NUMERIC_NAMED_SIZES: Partial<Record<number, NamedBrandSize>> = {
  1: 'XS',
  2: 'S',
  3: 'M',
  4: 'L',
  5: 'XL',
};

function isNamedBrandSize(size: string): size is NamedBrandSize {
  return BRAND_SIZES.some((candidate) => candidate === size);
}

/**
 * Resolves a public size to static Tailwind classes or, above `XL`, explicit
 * pixel dimensions. This keeps 1–5 aligned with the named scale while letting
 * callers request any positive whole-number step without depending on a
 * dynamically generated Tailwind class name.
 */
export function resolveBrandSize(size: BrandSize): ResolvedBrandSize {
  if (typeof size === 'string') {
    if (isNamedBrandSize(size)) {
      return NAMED_SIZE_CLASSES[size];
    }
    throw new RangeError(
      `Cinedex brand size must be one of: ${BRAND_SIZES.join(', ')}.`,
    );
  }

  if (!Number.isInteger(size) || size <= 0) {
    throw new RangeError(
      'Cinedex brand numeric size must be a whole number greater than 0.',
    );
  }

  const namedSize = NUMERIC_NAMED_SIZES[size];
  if (namedSize) {
    return NAMED_SIZE_CLASSES[namedSize];
  }

  // `XL` is 40px/20px. Each subsequent numeric step grows both by 4px.
  const pixelsAboveXl = (size - 5) * 4;
  return {
    markClassName: '',
    wordmarkClassName: '',
    markStyle: {
      width: `${String(40 + pixelsAboveXl)}px`,
      height: `${String(40 + pixelsAboveXl)}px`,
    },
    wordmarkStyle: { fontSize: `${String(20 + pixelsAboveXl)}px` },
  };
}
