import type { Meta, StoryObj } from '@storybook/react-vite';
import { Field, Input, PasswordInput } from '@cinedex/frames';

/**
 * The accessibility half of a form field: a label, a slot for the control, and
 * an optional error message, wired together with a generated id.
 *
 * The control is told none of that — `Input`, `PasswordInput` and anything else
 * calling `useFieldControl()` picks the id and `aria-*` up from context, so the
 * three can never disagree.
 */
const meta = {
  title: 'Frames/Field',
  component: Field,
  tags: ['autodocs'],
  args: {
    label: 'Email',
    children: <Input placeholder="you@example.com" />,
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Field>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/**
 * `error` does three things at once: the label switches to the warning tone, the
 * message is rendered and given an id, and the control inside picks up
 * `aria-invalid` plus `aria-describedby` pointing at it. With no error the
 * description is dropped entirely rather than pointing at an empty node.
 */
export const WithError: Story = {
  args: {
    error: 'Enter a valid email address',
    children: <Input defaultValue="not-an-email" />,
  },
};

/** `labelExtra` fills the right of the label row. */
export const WithLabelExtra: Story = {
  args: {
    label: 'Password',
    labelExtra: <a href="#forgot">Forgot?</a>,
    children: <PasswordInput defaultValue="hunter2" />,
  },
};

/** Any control works — the wiring is the field's, not the input's. */
export const AroundAPasswordInput: Story = {
  args: {
    label: 'New password — rejected',
    error: 'Appears in a known breach corpus — pick another.',
    children: <PasswordInput defaultValue="password12345" />,
  },
};
