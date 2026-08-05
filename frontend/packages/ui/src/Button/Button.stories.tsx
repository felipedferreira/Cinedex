import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';

const meta = {
  title: 'Primitives/Button',
  component: Button,
  tags: ['autodocs'],
  args: {
    children: 'Count is 0',
    variant: 'primary',
    size: 'md',
  },
  argTypes: {
    variant: { control: 'inline-radio', options: ['primary', 'ghost'] },
    size: { control: 'inline-radio', options: ['sm', 'md'] },
    onClick: { action: 'clicked' },
  },
} satisfies Meta<typeof Button>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Primary: Story = {};

export const Ghost: Story = {
  args: { variant: 'ghost', children: 'Learn more' },
};

export const Small: Story = {
  args: { size: 'sm', children: 'Small' },
};

export const Disabled: Story = {
  args: { disabled: true, children: 'Unavailable' },
};
