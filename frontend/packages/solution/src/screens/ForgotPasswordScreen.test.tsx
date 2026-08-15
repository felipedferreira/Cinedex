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
});

/**
 * The request/sent step is not a navigation, so it needs its own transition
 * host. These assert the structural half — that the outgoing step is frozen and
 * hidden — rather than the motion, which `rackFocus.test.ts` covers.
 */
describe('ForgotPasswordScreen transitions', () => {
  it('freezes the request form when the reset link is sent', async () => {
    const user = userEvent.setup();
    const { container } = render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(
      screen.getByRole('heading', { name: 'Check your inbox' }),
    ).toBeInTheDocument();

    const clone = container.querySelector('[data-cdx-pane="outgoing"]');
    expect(clone).toHaveAttribute('aria-hidden', 'true');
    expect(clone).toHaveAttribute('inert');
    expect(clone?.textContent).toContain('Reset your password');
  });

  it('exposes only the incoming step to assistive technology mid-flight', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    // Both steps are on the page for 340ms; only one may be reachable.
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
  });

  it('keeps the typed email across the move, which a remount would lose', async () => {
    const user = userEvent.setup();
    render(<ForgotPasswordScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));
    await user.click(screen.getByRole('button', { name: 'Start over' }));

    expect(screen.getByLabelText('Email')).toHaveValue('felipe@cinedex.io');
  });
});
