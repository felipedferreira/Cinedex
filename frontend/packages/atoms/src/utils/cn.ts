import { clsx, type ClassValue } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/**
 * `tailwind-merge` classifies an unrecognised `text-*` class as a colour, so
 * without this extension `text-label text-accent` would be treated as two
 * conflicting colours and one of them silently dropped. Registering the theme's
 * type and tracking steps in their real class groups keeps a size and a colour
 * on the same element.
 *
 * These lists mirror the `--text-*` and `--tracking-*` entries in
 * `@cinedex/theme`'s `tailwind.css`; a new step needs a line in both.
 */
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      'font-size': [
        {
          text: [
            'label',
            'footnote',
            'brand',
            'caption',
            'note',
            'body',
            'title',
          ],
        },
      ],
      tracking: [{ tracking: ['label', 'eyebrow'] }],
    },
  },
});

/**
 * Joins class names and resolves Tailwind conflicts, last one winning.
 *
 * This is what makes a caller's `className` reliably beat a component's own —
 * `cn('px-3', 'px-6')` is `px-6`, where plain concatenation would leave both and
 * let stylesheet order decide.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
