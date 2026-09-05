import type { Meta, StoryObj } from '@storybook/react-vite';
import { Alert } from '@cinedex/frames';

const meta = {
  title: 'Frames/Alert',
  component: Alert,
  tags: ['autodocs'],
  args: {
    children: 'Locked at 14:02 UTC · 5 failures / 15 min',
  },
  argTypes: {
    tone: {
      control: 'inline-radio',
      options: ['neutral', 'warning'],
    },
  },
} satisfies Meta<typeof Alert>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Neutral: Story = { args: { tone: 'neutral' } };

export const Warning: Story = { args: { tone: 'warning' } };
