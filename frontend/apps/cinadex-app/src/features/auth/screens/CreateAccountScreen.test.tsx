import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAuthScreen } from '../../../test/renderAuthScreen';
import { CreateAccountScreen } from './CreateAccountScreen';

describe('CreateAccountScreen', () => {
  it('keeps Create account disabled until every requirement is met', async () => {
    const user = userEvent.setup();
    await renderAuthScreen(<CreateAccountScreen />, '/register');

    const submit = screen.getByRole('button', { name: 'Create account' });
    expect(submit).toBeDisabled();

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.type(screen.getByLabelText('Username'), 'fferreira');
    await user.type(screen.getByLabelText('Password'), 'correct-horse-battery');
    await user.click(
      screen.getByRole('checkbox', { name: /accept the catalog terms/i }),
    );

    expect(submit).toBeEnabled();
  });

  it('keeps Create account disabled when the password equals the username', async () => {
    const user = userEvent.setup();
    await renderAuthScreen(<CreateAccountScreen />, '/register');

    await user.type(screen.getByLabelText('Email'), 'felipe@cinedex.io');
    await user.type(screen.getByLabelText('Username'), 'fferreira12345');
    await user.type(screen.getByLabelText('Password'), 'fferreira12345');
    await user.click(
      screen.getByRole('checkbox', { name: /accept the catalog terms/i }),
    );

    expect(
      screen.getByRole('button', { name: 'Create account' }),
    ).toBeDisabled();
  });
});
