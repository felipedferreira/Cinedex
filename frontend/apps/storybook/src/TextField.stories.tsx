import type { Meta, StoryObj } from '@storybook/react-vite';
import { TextField } from '@cinedex/components';

const meta = {
  title: 'Primitives/TextField',
  component: TextField,
  tags: ['autodocs'],
  args: {
    label: 'Email',
    placeholder: 'you@example.com',
  },
} satisfies Meta<typeof TextField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithError: Story = {
  args: {
    error: 'Enter a valid email address',
    defaultValue: 'not-an-email',
  },
};

export const Disabled: Story = {
  args: { disabled: true, defaultValue: 'ada@example.com' },
};

export const Search: Story = {
  args: { label: 'Search titles', type: 'search', placeholder: 'Blade Runner' },
};
