import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Field } from '../Field/Field';
import { PasswordInput } from './PasswordInput';

describe('PasswordInput', () => {
  it('masks the value by default and reveals it on "Show"', async () => {
    const user = userEvent.setup();
    render(
      <Field label="Password">
        <PasswordInput value="hunter2" onChange={vi.fn()} />
      </Field>,
    );

    const input = screen.getByLabelText('Password');
    expect(input).toHaveAttribute('type', 'password');

    await user.click(screen.getByRole('button', { name: 'Show' }));

    expect(input).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Hide' })).toBeInTheDocument();
  });

  it('picks up the surrounding field id, so the label points at it', () => {
    render(
      <Field label="New password">
        <PasswordInput value="" onChange={vi.fn()} />
      </Field>,
    );

    expect(screen.getByLabelText('New password')).toHaveAttribute(
      'type',
      'password',
    );
  });

  it('renders standalone outside a Field', () => {
    render(<PasswordInput aria-label="Password" value="" onChange={vi.fn()} />);

    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });
});
