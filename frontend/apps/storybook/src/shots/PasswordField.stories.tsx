import type { Meta, StoryObj } from '@storybook/react-vite';
import { PasswordField } from '@cinedex/shots';

/**
 * A password field: the label/error wiring of frames' `Field` around the masked
 * input and its reveal toggle. The counterpart to `TextField`, which pairs
 * `Field` with a plain `Input`.
 *
 * Everything else it takes is `PasswordInput`'s, so `defaultValue`, `value` /
 * `onChange`, `autoComplete` and `disabled` all behave as they do on the frame.
 */
const meta = {
  title: 'Shots/PasswordField',
  component: PasswordField,
  tags: ['autodocs'],
  args: {
    label: 'Password',
    defaultValue: 'hunter2',
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PasswordField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** `labelExtra` fills the right of the label row — where sign-in puts "Forgot?". */
export const WithLabelExtra: Story = {
  args: { labelExtra: <a href="#forgot">Forgot?</a> },
};

/**
 * `error` reaches `Field`, which tones the label, renders the message and hands
 * the input `aria-invalid` plus the message's id. The field states the reason;
 * the label states that there is one.
 */
export const Rejected: Story = {
  args: {
    label: 'New password — rejected',
    defaultValue: 'password12345',
    error: 'Appears in a known breach corpus — pick another.',
  },
};

export const Disabled: Story = {
  args: { disabled: true },
};

export const Empty: Story = {
  args: { defaultValue: '', placeholder: '••••••••' },
};
