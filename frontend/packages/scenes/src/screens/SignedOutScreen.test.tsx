import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SignedOutScreen } from './SignedOutScreen';

describe('SignedOutScreen', () => {
  it('renders the heading and a link back to sign in', () => {
    render(<SignedOutScreen />);

    expect(
      screen.getByRole('heading', { name: "You're signed out" }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in again' })).toHaveAttribute(
      'href',
      '/login',
    );
  });

  it('zeroes out the other-sessions count and disables itself once pressed', async () => {
    const user = userEvent.setup();
    render(<SignedOutScreen />);

    await user.click(
      screen.getByRole('button', { name: 'Sign out everywhere (2)' }),
    );

    expect(
      screen.getByRole('button', { name: 'Signed out everywhere' }),
    ).toBeDisabled();
  });
});
