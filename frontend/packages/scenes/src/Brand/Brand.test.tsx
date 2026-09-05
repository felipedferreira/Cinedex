import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Brand } from './Brand';
import { resolveBrandSize } from './brandSize';

describe('Brand', () => {
  it('uses the medium lockup size by default', () => {
    render(<Brand />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toHaveClass('size-7');
    expect(screen.getByText('Cinedex')).toHaveClass('text-sm');
  });

  it.each([
    ['XS', 'size-5', 'text-brand'],
    ['S', 'size-6', 'text-xs'],
    ['M', 'size-7', 'text-sm'],
    ['L', 'size-8', 'text-base'],
    ['XL', 'size-10', 'text-xl'],
  ] as const)(
    'applies the %s lockup size',
    (size, markClass, wordmarkClass) => {
      render(<Brand size={size} />);

      expect(screen.getByRole('img', { name: 'Cinedex' })).toHaveClass(
        markClass,
      );
      expect(screen.getByText('Cinedex')).toHaveClass(wordmarkClass);
    },
  );

  it('treats numeric size 1 as the XS lockup', () => {
    render(<Brand size={1} />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toHaveClass('size-5');
    expect(screen.getByText('Cinedex')).toHaveClass('text-brand');
  });

  it('continues a numeric size beyond XL', () => {
    render(<Brand size={6} />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toHaveStyle({
      width: '44px',
      height: '44px',
    });
    expect(screen.getByText('Cinedex')).toHaveStyle({ fontSize: '24px' });
  });

  it.each([0, -1, 1.5, Number.NaN, Number.POSITIVE_INFINITY])(
    'rejects invalid numeric size %s',
    (size) => {
      expect(() => {
        resolveBrandSize(size);
      }).toThrow(RangeError);
    },
  );

  it('renders the mark as a labelled image', () => {
    render(<Brand />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toBeInTheDocument();
  });

  it('renders the wordmark as text', () => {
    render(<Brand />);

    expect(screen.getByText('Cinedex')).toBeInTheDocument();
  });
});
