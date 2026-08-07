import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ResetPasswordScreen } from './ResetPasswordScreen';

describe('ResetPasswordScreen', () => {
  it('shows the verified email when one is provided', () => {
    render(<ResetPasswordScreen email="felipe@cinedex.io" />);

    expect(
      screen.getByText(/Link verified for felipe@cinedex\.io/),
    ).toBeInTheDocument();
  });

  it('rejects a password shorter than 12 characters and disables submit', async () => {
    const user = userEvent.setup();
    render(<ResetPasswordScreen />);

    await user.type(screen.getByLabelText(/new password/i), 'short1!');

    expect(
      screen.getByText('Must be at least 12 characters.'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Save and sign in' }),
    ).toBeDisabled();
  });

  it('flags a mismatched confirmation', async () => {
    const user = userEvent.setup();
    render(<ResetPasswordScreen />);

    await user.type(
      screen.getByLabelText(/new password/i),
      'correct-horse-battery',
    );
    await user.type(
      screen.getByLabelText('Confirm password'),
      'different-password',
    );

    expect(
      screen.getByText("Doesn't match the new password."),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Save and sign in' }),
    ).toBeDisabled();
  });

  it('enables submit once the password is strong and confirmed', async () => {
    const user = userEvent.setup();
    render(<ResetPasswordScreen />);

    await user.type(
      screen.getByLabelText(/new password/i),
      'correct-horse-battery',
    );
    await user.type(
      screen.getByLabelText('Confirm password'),
      'correct-horse-battery',
    );

    expect(
      screen.getByRole('button', { name: 'Save and sign in' }),
    ).toBeEnabled();
  });
});
