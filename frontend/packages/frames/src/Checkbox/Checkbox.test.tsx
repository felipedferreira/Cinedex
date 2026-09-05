import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Checkbox } from './Checkbox';

describe('Checkbox', () => {
  it('takes its accessible name from the label', () => {
    render(
      <Checkbox
        label="Keep me signed in"
        checked={false}
        onCheckedChange={vi.fn()}
      />,
    );

    expect(
      screen.getByRole('checkbox', { name: 'Keep me signed in' }),
    ).toBeInTheDocument();
  });

  it('calls onCheckedChange when clicked', async () => {
    const onCheckedChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Checkbox
        label="Accept terms"
        checked={false}
        onCheckedChange={onCheckedChange}
      />,
    );

    await user.click(screen.getByRole('checkbox'));

    expect(onCheckedChange).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('toggles when the label is clicked', async () => {
    const onCheckedChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Checkbox
        label="Accept terms"
        checked={false}
        onCheckedChange={onCheckedChange}
      />,
    );

    await user.click(screen.getByText('Accept terms'));

    expect(onCheckedChange).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('reflects the checked prop', () => {
    render(<Checkbox label="Accept terms" checked />);

    expect(screen.getByRole('checkbox')).toBeChecked();
  });

  it('does not toggle when disabled', async () => {
    const onCheckedChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Checkbox
        label="Accept terms"
        checked={false}
        disabled
        onCheckedChange={onCheckedChange}
      />,
    );

    await user.click(screen.getByRole('checkbox'));

    expect(onCheckedChange).not.toHaveBeenCalled();
  });
});
