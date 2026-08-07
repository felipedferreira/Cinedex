import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAuthScreen } from '../../../test/renderAuthScreen';
import { SignInScreen } from './SignInScreen';

describe('SignInScreen', () => {
  it('renders the sign-in form with the account checkbox checked by default', async () => {
    await renderAuthScreen(<SignInScreen />, '/login');

    expect(
      screen.getByRole('heading', { name: 'Sign in' }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /keep me signed in/i }),
    ).toBeChecked();
  });

  it('accepts typed credentials', async () => {
    const user = userEvent.setup();
    await renderAuthScreen(<SignInScreen />, '/login');

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');

    expect(screen.getByLabelText('Email')).toHaveValue('felipe@cinedex.io');
  });

  it('renders the locked-out variant with a disabled submit button', async () => {
    await renderAuthScreen(<SignInScreen locked />, '/login');

    expect(
      screen.getByRole('heading', { name: 'Too many attempts' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeDisabled();
  });
});
