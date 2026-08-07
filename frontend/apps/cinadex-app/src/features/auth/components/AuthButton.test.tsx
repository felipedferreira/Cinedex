import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthButton } from './AuthButton';

describe('AuthButton', () => {
  it('defaults to type="button"', () => {
    render(<AuthButton>Sign in</AuthButton>);

    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');
  });

  it('calls onClick when pressed', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(<AuthButton onClick={onClick}>Sign in</AuthButton>);

    await user.click(screen.getByRole('button'));

    expect(onClick).toHaveBeenCalledOnce();
  });

  it('does not call onClick when disabled', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(
      <AuthButton disabled onClick={onClick}>
        Sign in
      </AuthButton>,
    );

    await user.click(screen.getByRole('button'));

    expect(onClick).not.toHaveBeenCalled();
  });

  it('renders the solid and outline variants with different styling', () => {
    const { rerender } = render(<AuthButton variant="solid">Go</AuthButton>);
    const solidClass = screen.getByRole('button').className;

    rerender(<AuthButton variant="outline">Go</AuthButton>);
    const outlineClass = screen.getByRole('button').className;

    expect(solidClass).not.toBe(outlineClass);
  });
});
