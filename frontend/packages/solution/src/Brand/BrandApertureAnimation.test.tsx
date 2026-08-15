import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrandApertureAnimation } from './BrandApertureAnimation';

describe('BrandApertureAnimation', () => {
  it('renders the mark as a labelled image', () => {
    render(<BrandApertureAnimation />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toBeInTheDocument();
  });

  it('renders the wordmark as text', () => {
    render(<BrandApertureAnimation />);

    expect(screen.getByText('Cinedex')).toBeInTheDocument();
  });

  it('forwards its size to the mark and wordmark', () => {
    render(<BrandApertureAnimation size="XL" />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toHaveClass('size-10');
    expect(screen.getByText('Cinedex')).toHaveClass('text-xl');
  });

  it('settles on the fully-open iris immediately for a reduced-motion viewer', () => {
    // The global `matchMedia` stub defaults to `matches: true` (see
    // test/setup.ts) specifically so this exercises the synchronous settle
    // branch rather than the requestAnimationFrame loop, which jsdom does
    // not implement — the multi-frame path is verified by hand in a browser.
    const { container } = render(<BrandApertureAnimation />);

    const blade = container.querySelector('[data-blade]');
    const rotation = Number(
      /rotate\(([-\d.]+)\)/.exec(blade?.getAttribute('transform') ?? '')?.[1],
    );
    expect(rotation).toBeCloseTo(30, 1);

    // The glint sweep and ghost arcs are both mid-sequence effects — settled
    // means neither is left visible.
    expect(
      Number(
        container.querySelector('[data-glintwrap]')?.getAttribute('opacity'),
      ),
    ).toBe(0);
  });
});
