import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Checkbox } from './Checkbox';

describe('Checkbox', () => {
  it('associates the label with the input via the accessible name', () => {
    render(
      <Checkbox label="Keep me signed in" checked={false} onChange={vi.fn()} />,
    );

    expect(
      screen.getByRole('checkbox', { name: 'Keep me signed in' }),
    ).toBeInTheDocument();
  });

  it('calls onChange when clicked', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Checkbox label="Accept terms" checked={false} onChange={onChange} />,
    );

    await user.click(screen.getByRole('checkbox'));

    expect(onChange).toHaveBeenCalledOnce();
  });

  it('reflects the checked prop', () => {
    render(<Checkbox label="Accept terms" checked readOnly />);

    expect(screen.getByRole('checkbox')).toBeChecked();
  });
});
