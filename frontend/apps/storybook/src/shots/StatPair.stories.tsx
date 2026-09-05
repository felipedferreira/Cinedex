import type { Meta, StoryObj } from '@storybook/react-vite';
import { StatPair } from '@cinedex/shots';

/**
 * Two figures side by side in a hairline-divided grid — a countdown next to a
 * remaining-attempts count, say.
 *
 * The divider is the container's background showing through a 1px gap rather
 * than a border, so it stays a single hairline at any zoom. Exactly two stats:
 * the prop is a tuple, not an array.
 */
const meta = {
  title: 'Shots/StatPair',
  component: StatPair,
  tags: ['autodocs'],
  args: {
    stats: [
      { value: '12:47', label: 'Unlocks in', tone: 'warning' },
      { value: '0', label: 'Attempts left' },
    ],
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof StatPair>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The locked-out sign-in card: a warning figure against a neutral one. */
export const Lockout: Story = {};

/** The multi-session sign-out panel. */
export const SignedOut: Story = {
  args: {
    stats: [
      { value: '1', label: 'Device signed out', tone: 'success' },
      { value: '2', label: 'Sessions still active' },
    ],
  },
};

/** `tone` defaults to `neutral`, which is the page surface with no tint. */
export const Neutral: Story = {
  args: {
    stats: [
      { value: '1982', label: 'Released' },
      { value: '117', label: 'Minutes' },
    ],
  },
};

/** Both halves can carry a tone — they are independent. */
export const BothToned: Story = {
  args: {
    stats: [
      { value: '3', label: 'Failed attempts', tone: 'warning' },
      { value: '2', label: 'Trusted devices', tone: 'success' },
    ],
  },
};
