import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAuthScreen } from '../../../test/renderAuthScreen';
import { ForgotPasswordScreen } from './ForgotPasswordScreen';

describe('ForgotPasswordScreen', () => {
  it('shows the confirmation screen with the submitted email after requesting a reset', async () => {
    const user = userEvent.setup();
    await renderAuthScreen(<ForgotPasswordScreen />, '/forgot-password');

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(
      screen.getByRole('heading', { name: 'Check your inbox' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/felipe@cinedex\.io/)).toBeInTheDocument();
  });

  it('returns to the request form when "Start over" is pressed', async () => {
    const user = userEvent.setup();
    await renderAuthScreen(<ForgotPasswordScreen />, '/forgot-password');

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));
    await user.click(screen.getByRole('button', { name: 'Start over' }));

    expect(
      screen.getByRole('heading', { name: 'Reset your password' }),
    ).toBeInTheDocument();
  });
});
