import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TwoFactorScreen } from './TwoFactorScreen';

describe('TwoFactorScreen', () => {
  it('disables Verify until all six digits are entered', async () => {
    const user = userEvent.setup();
    render(<TwoFactorScreen />);

    expect(screen.getByRole('button', { name: 'Verify' })).toBeDisabled();

    const boxes = screen.getAllByRole('textbox');
    await user.click(boxes[0]);
    await user.keyboard('481523');

    expect(screen.getByRole('button', { name: 'Verify' })).toBeEnabled();
  });

  it('resets the code and countdown when Resend is pressed', async () => {
    const user = userEvent.setup();
    render(<TwoFactorScreen />);

    const boxes = screen.getAllByRole('textbox');
    await user.click(boxes[0]);
    await user.keyboard('4815');

    await user.click(screen.getByRole('button', { name: 'Resend' }));

    expect((boxes[0] as HTMLInputElement).value).toBe('');
  });
});
