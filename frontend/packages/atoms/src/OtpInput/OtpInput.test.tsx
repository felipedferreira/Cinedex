import { useState } from 'react';
import type { FC } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { OtpInput } from './OtpInput';

const ControlledOtpInput: FC = () => {
  const [value, setValue] = useState('');
  return (
    <OtpInput label="Verification code" value={value} onChange={setValue} />
  );
};

describe('OtpInput', () => {
  it('renders one box per digit', () => {
    render(<OtpInput label="Verification code" value="" onChange={vi.fn()} />);

    expect(screen.getAllByRole('textbox')).toHaveLength(6);
  });

  it('advances focus to the next box as digits are typed', async () => {
    const user = userEvent.setup();
    render(<ControlledOtpInput />);

    const boxes = screen.getAllByRole('textbox');
    await user.click(boxes[0]);
    await user.keyboard('4');

    expect(boxes[1]).toHaveFocus();
  });

  it('combines the typed digits into the reported value', async () => {
    const user = userEvent.setup();
    render(<ControlledOtpInput />);

    const boxes = screen.getAllByRole('textbox');
    await user.click(boxes[0]);
    await user.keyboard('481523');

    expect(boxes.map((box) => (box as HTMLInputElement).value).join('')).toBe(
      '481523',
    );
  });

  it('moves focus back and clears the previous digit on backspace from an empty box', async () => {
    const user = userEvent.setup();
    render(<ControlledOtpInput />);

    const boxes = screen.getAllByRole('textbox');
    await user.click(boxes[0]);
    await user.keyboard('4');
    expect(boxes[1]).toHaveFocus();

    await user.keyboard('{Backspace}');

    expect(boxes[0]).toHaveFocus();
    expect((boxes[0] as HTMLInputElement).value).toBe('');
  });
});
