import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAuthScreen } from '../../../test/renderAuthScreen';
import { SignedOutScreen } from './SignedOutScreen';

describe('SignedOutScreen', () => {
  it('renders the heading and a link back to sign in', async () => {
    await renderAuthScreen(<SignedOutScreen />, '/signed-out');

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
    await renderAuthScreen(<SignedOutScreen />, '/signed-out');

    const signOutEverywhere = screen.getByRole('button', {
      name: 'Sign out everywhere (2)',
    });
    await user.click(signOutEverywhere);

    expect(
      screen.getByRole('button', { name: 'Signed out everywhere' }),
    ).toBeDisabled();
  });
});
