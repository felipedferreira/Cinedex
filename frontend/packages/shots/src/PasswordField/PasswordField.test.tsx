import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PasswordField } from './PasswordField';

describe('PasswordField', () => {
  it('masks the value by default and reveals it on "Show"', async () => {
    const user = userEvent.setup();
    render(
      <PasswordField label="Password" value="hunter2" onChange={vi.fn()} />,
    );

    const input = screen.getByLabelText('Password');
    expect(input).toHaveAttribute('type', 'password');

    await user.click(screen.getByRole('button', { name: 'Show' }));

    expect(input).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Hide' })).toBeInTheDocument();
  });

  it('marks the field invalid and announces the error when one is set', () => {
    render(
      <PasswordField
        label="New password"
        value="123"
        onChange={vi.fn()}
        error="Appears in a known breach corpus — pick another."
      />,
    );

    const input = screen.getByLabelText('New password');
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription(
      'Appears in a known breach corpus — pick another.',
    );
  });

  it('renders labelExtra in the label row', () => {
    render(
      <PasswordField
        label="Password"
        value=""
        onChange={vi.fn()}
        labelExtra={<a href="/forgot-password">Forgot?</a>}
      />,
    );

    expect(screen.getByRole('link', { name: 'Forgot?' })).toBeInTheDocument();
  });
});
