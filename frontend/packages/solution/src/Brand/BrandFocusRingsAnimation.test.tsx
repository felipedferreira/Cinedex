import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrandFocusRingsAnimation } from './BrandFocusRingsAnimation';

describe('BrandFocusRingsAnimation', () => {
  it('renders the mark as a labelled image', () => {
    render(<BrandFocusRingsAnimation />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toBeInTheDocument();
  });

  it('renders the wordmark as text', () => {
    render(<BrandFocusRingsAnimation />);

    expect(screen.getByText('Cinedex')).toBeInTheDocument();
  });

  it('settles on the same fully-open iris as BrandApertureAnimation', () => {
    // Same reasoning as BrandApertureAnimation.test.tsx: the global
    // `matchMedia` stub defaults to reduced motion so this stays on the
    // synchronous settle branch rather than needing a requestAnimationFrame
    // polyfill jsdom doesn't provide.
    const { container } = render(<BrandFocusRingsAnimation />);

    const blade = container.querySelector('[data-blade]');
    const rotation = Number(
      /rotate\(([-\d.]+)\)/.exec(blade?.getAttribute('transform') ?? '')?.[1],
    );
    expect(rotation).toBeCloseTo(30, 1);

    // Ghost arcs are this sequence's mid-flight-only effect — settled means
    // they've faded back out, same as the glint has in BrandApertureAnimation.
    expect(
      Number(container.querySelector('[data-ghosts]')?.getAttribute('opacity')),
    ).toBe(0);
  });
});
