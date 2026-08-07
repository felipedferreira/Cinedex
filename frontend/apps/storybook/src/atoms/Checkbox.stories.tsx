import type { Meta, StoryObj } from '@storybook/react-vite';
import { Checkbox } from '@cinedex/atoms';

const meta = {
  title: 'Atoms/Checkbox',
  component: Checkbox,
  tags: ['autodocs'],
  args: {
    label: 'Keep me signed in on this device',
  },
  argTypes: {
    onCheckedChange: { action: 'checked changed' },
  },
} satisfies Meta<typeof Checkbox>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Unchecked: Story = { args: { checked: false } };

export const Checked: Story = { args: { checked: true } };

export const Disabled: Story = { args: { checked: true, disabled: true } };

/** Uncontrolled — Radix keeps the state when `checked` is not supplied. */
export const Uncontrolled: Story = {
  args: { label: 'I accept the catalog terms and moderation policy' },
};
