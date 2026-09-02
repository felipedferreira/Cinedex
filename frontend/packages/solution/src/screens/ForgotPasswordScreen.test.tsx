import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ForgotPasswordScreen } from './ForgotPasswordScreen';

describe('ForgotPasswordScreen', () => {
  it('shows the confirmation screen with the submitted email after requesting a reset', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(
      screen.getByRole('heading', { name: 'Check your inbox' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/felipe@cinedex\.io/)).toBeInTheDocument();
  });

  it('returns to the request form when "Start over" is pressed', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));
    await user.click(screen.getByRole('button', { name: 'Start over' }));

    expect(
      screen.getByRole('heading', { name: 'Reset your password' }),
    ).toBeInTheDocument();
  });

  // The step is state inside one component rather than two screens, which is
  // what keeps the address across the round trip. A version that swapped
  // components would lose it.
  it('keeps the typed email when the user starts over', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));
    await user.click(screen.getByRole('button', { name: 'Start over' }));

    expect(screen.getByLabelText('Email')).toHaveValue('felipe@cinedex.io');
  });
});
