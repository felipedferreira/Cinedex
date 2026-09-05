import type { Meta, StoryObj } from '@storybook/react-vite';
import { PasswordChecklist } from '@cinedex/shots';

/**
 * The tick-list of password requirements, met and unmet.
 *
 * It takes the same `PasswordRequirement[]` that `strengthFromRequirements`
 * collapses into a ratio, so the list and the meter beside it cannot disagree
 * about what has been satisfied. The boxes are `aria-hidden` — "met" is carried
 * by the list text, not by a glyph a screen reader would have to interpret.
 */
const meta = {
  title: 'Shots/PasswordChecklist',
  component: PasswordChecklist,
  tags: ['autodocs'],
  args: {
    requirements: [
      { label: '12 characters or more', met: false },
      { label: 'Not your email or username', met: false },
    ],
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PasswordChecklist>;

export default meta;
type Story = StoryObj<typeof meta>;

export const NoneMet: Story = {};

export const PartiallyMet: Story = {
  args: {
    requirements: [
      { label: '12 characters or more', met: true },
      { label: 'Not your email or username', met: false },
    ],
  },
};

export const AllMet: Story = {
  args: {
    requirements: [
      { label: '12 characters or more', met: true },
      { label: 'Not your email or username', met: true },
    ],
  },
};

/** The list is not fixed at two — it renders whatever it is given. */
export const LongerList: Story = {
  args: {
    requirements: [
      { label: '12 characters or more', met: true },
      { label: 'Not your email or username', met: true },
      { label: 'Not in a known breach corpus', met: false },
      { label: 'Not one of your last five passwords', met: false },
    ],
  },
};
