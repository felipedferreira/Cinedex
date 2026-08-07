import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SignInScreen } from './SignInScreen';

describe('SignInScreen', () => {
  it('renders the sign-in form with the account checkbox checked by default', () => {
    render(<SignInScreen />);

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
    render(<SignInScreen />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');

    expect(screen.getByLabelText('Email')).toHaveValue('felipe@cinedex.io');
  });

  it('reports the entered credentials on submit', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<SignInScreen onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.type(screen.getByLabelText('Password'), 'hunter2');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(onSubmit).toHaveBeenCalledExactlyOnceWith({
      email: 'felipe@cinedex.io',
      password: 'hunter2',
      keepSignedIn: true,
    });
  });

  it('renders the locked-out variant with a disabled submit button', () => {
    render(<SignInScreen locked />);

    expect(
      screen.getByRole('heading', { name: 'Too many attempts' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeDisabled();
  });

  it('offers a password reset link out of the locked-out state', () => {
    render(<SignInScreen locked />);

    expect(
      screen.getByRole('link', { name: 'Reset password instead' }),
    ).toHaveAttribute('href', '/forgot-password');
  });
});
