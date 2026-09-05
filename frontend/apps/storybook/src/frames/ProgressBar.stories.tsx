import type { Meta, StoryObj } from '@storybook/react-vite';
import { ProgressBar } from '@cinedex/frames';

/**
 * A determinate progress track — the password strength meter's bar.
 *
 * Radix's `Progress` supplies `role="progressbar"` and the `aria-value*` set,
 * which the hand-rolled `<span><i/></span>` this replaced did not have: the
 * meter previously announced nothing at all. `label` is its accessible name.
 */
const meta = {
  title: 'Frames/ProgressBar',
  component: ProgressBar,
  tags: ['autodocs'],
  args: {
    ratio: 0.5,
    label: 'Password strength',
  },
  argTypes: {
    ratio: { control: { type: 'range', min: 0, max: 1, step: 0.05 } },
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ProgressBar>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Empty: Story = { args: { ratio: 0 } };

export const Half: Story = { args: { ratio: 0.5 } };

export const Full: Story = { args: { ratio: 1 } };

/** The indicator transitions on `width`, so a changing `ratio` animates. */
export const Weak: Story = {
  args: { ratio: 0.25, label: 'Password strength' },
};

/** Values outside 0–1 are clamped rather than overflowing the track. */
export const OutOfRangeIsClamped: Story = {
  args: { ratio: 1.6 },
};
