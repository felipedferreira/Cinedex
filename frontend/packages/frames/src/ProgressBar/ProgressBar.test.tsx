import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ProgressBar } from './ProgressBar';

describe('ProgressBar', () => {
  it('exposes a named progressbar with the ratio as a percentage', () => {
    render(<ProgressBar ratio={0.5} label="Password strength" />);

    const bar = screen.getByRole('progressbar', { name: 'Password strength' });
    expect(bar).toHaveAttribute('aria-valuenow', '50');
    expect(bar).toHaveAttribute('aria-valuemax', '100');
  });

  it.each([
    [-1, '0'],
    [0, '0'],
    [1, '100'],
    [2, '100'],
  ])('clamps a ratio of %s to %s percent', (ratio, expected) => {
    render(<ProgressBar ratio={ratio} label="Strength" />);

    expect(screen.getByRole('progressbar')).toHaveAttribute(
      'aria-valuenow',
      expected,
    );
  });
});
