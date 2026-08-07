import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { OtpInput } from '@cinedex/atoms';

const meta = {
  title: 'Atoms/OtpInput',
  component: OtpInput,
  tags: ['autodocs'],
  args: {
    label: 'Verification code',
    value: '',
    // Required by the component's props, but every story is driven by the
    // `render` below, which supplies the real setter.
    onChange: () => undefined,
  },
  render: function ControlledOtpInput(args) {
    const [value, setValue] = useState(args.value);
    return <OtpInput {...args} value={value} onChange={setValue} />;
  },
} satisfies Meta<typeof OtpInput>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Empty: Story = {};

export const PartiallyFilled: Story = { args: { value: '481' } };

export const Complete: Story = { args: { value: '481523' } };

/** The box count follows `length` — the grid is not fixed at six. */
export const FourDigits: Story = { args: { length: 4, value: '48' } };
