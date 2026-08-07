import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PasswordChecklist, PasswordStrengthMeter } from './PasswordStrength';
import { strengthFromRequirements } from './strengthFromRequirements';

describe('strengthFromRequirements', () => {
  it('reports "Too weak" when nothing is met', () => {
    expect(
      strengthFromRequirements([
        { label: 'a', met: false },
        { label: 'b', met: false },
      ]),
    ).toEqual({ ratio: 0, label: 'Too weak' });
  });

  it('reports "Weak" when some requirements are met', () => {
    const { ratio, label } = strengthFromRequirements([
      { label: 'a', met: true },
      { label: 'b', met: false },
    ]);
    expect(label).toBe('Weak');
    expect(ratio).toBeCloseTo(0.5);
  });

  it('reports "Strong" when every requirement is met', () => {
    expect(
      strengthFromRequirements([
        { label: 'a', met: true },
        { label: 'b', met: true },
      ]),
    ).toEqual({ ratio: 1, label: 'Strong' });
  });

  it('reports "Too weak" for an empty list rather than dividing by zero', () => {
    expect(strengthFromRequirements([])).toEqual({
      ratio: 0,
      label: 'Too weak',
    });
  });
});

describe('PasswordStrengthMeter', () => {
  it('exposes the ratio on a named progressbar and shows the verdict', () => {
    render(<PasswordStrengthMeter ratio={0.5} label="Weak" />);

    expect(
      screen.getByRole('progressbar', { name: 'Password strength' }),
    ).toHaveAttribute('aria-valuenow', '50');
    expect(screen.getByText('Weak')).toBeInTheDocument();
  });
});

describe('PasswordChecklist', () => {
  it('renders every requirement label', () => {
    render(
      <PasswordChecklist
        requirements={[
          { label: '12 characters or more', met: true },
          { label: 'Not your email or username', met: false },
        ]}
      />,
    );

    expect(screen.getByText('12 characters or more')).toBeInTheDocument();
    expect(screen.getByText('Not your email or username')).toBeInTheDocument();
  });
});
