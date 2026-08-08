import type { Meta, StoryObj } from '@storybook/react-vite';
import { inputVariants, Label } from '@cinedex/atoms';

/**
 * The uppercase mono field label. Radix's `Label` is the base so that clicking
 * the text focuses the control even when that control is not a native input.
 *
 * `Field` renders one of these itself, against its own generated id — reach for
 * `Label` directly only for a control `Field` does not wrap.
 */
const meta = {
  title: 'Atoms/Label',
  component: Label,
  tags: ['autodocs'],
  args: {
    children: 'Email',
    tone: 'default',
  },
  argTypes: {
    tone: { control: 'inline-radio', options: ['default', 'error'] },
  },
} satisfies Meta<typeof Label>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** `tone="error"` switches to the warning ramp — what `Field` sets on rejection. */
export const ErrorTone: Story = {
  args: { tone: 'error', children: 'New password — rejected' },
};

/**
 * `htmlFor` associates the label with a control: clicking the text focuses it.
 * The input here is a native one carrying its own id, styled with the exported
 * `inputVariants` — inside a `Field` neither the id nor the wiring is the
 * caller's to write.
 */
export const ForAControl: Story = {
  render: (args) => (
    <div className="flex max-w-[420px] flex-col gap-1.5">
      <Label {...args} htmlFor="label-story-email" />
      <input
        id="label-story-email"
        type="email"
        placeholder="you@example.com"
        className={inputVariants()}
      />
    </div>
  ),
};
