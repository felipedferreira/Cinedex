import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatPair } from './StatPair';

describe('StatPair', () => {
  it('renders both stat values and labels', () => {
    render(
      <StatPair
        stats={[
          { value: '12:47', label: 'Unlocks in', tone: 'warning' },
          { value: '0', label: 'Attempts left' },
        ]}
      />,
    );

    expect(screen.getByText('12:47')).toBeInTheDocument();
    expect(screen.getByText('Unlocks in')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
    expect(screen.getByText('Attempts left')).toBeInTheDocument();
  });
});
