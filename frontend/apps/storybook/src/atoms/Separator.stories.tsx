import type { Meta, StoryObj } from '@storybook/react-vite';
import { Separator } from '@cinedex/atoms';

/**
 * A themed rule. Radix marks it `aria-hidden` when decorative (the default), so
 * it does not turn up in the accessibility tree as something to navigate past.
 */
const meta = {
  title: 'Atoms/Separator',
  component: Separator,
  tags: ['autodocs'],
  args: {
    orientation: 'horizontal',
    decorative: true,
  },
  argTypes: {
    orientation: {
      control: 'inline-radio',
      options: ['horizontal', 'vertical'],
    },
  },
} satisfies Meta<typeof Separator>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Horizontal: Story = {
  render: (args) => (
    <div className="flex max-w-[420px] flex-col gap-3">
      <span className="text-body text-text">Above</span>
      <Separator {...args} />
      <span className="text-body text-text">Below</span>
    </div>
  ),
};

/** `orientation="vertical"` swaps the axis — the parent supplies the height. */
export const Vertical: Story = {
  args: { orientation: 'vertical' },
  render: (args) => (
    <div className="flex h-6 items-center gap-3">
      <span className="text-body text-text">Left</span>
      <Separator {...args} />
      <span className="text-body text-text">Right</span>
    </div>
  ),
};

/**
 * `decorative={false}` makes it a real `role="separator"`, for a rule that
 * genuinely divides two regions rather than just drawing a line.
 */
export const Semantic: Story = {
  args: { decorative: false },
  render: (args) => (
    <div className="flex max-w-[420px] flex-col gap-3">
      <span className="text-body text-text">Sign in with a password</span>
      <Separator {...args} />
      <span className="text-body text-text">Sign in with a provider</span>
    </div>
  ),
};
