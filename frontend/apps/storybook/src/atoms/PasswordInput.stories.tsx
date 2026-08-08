import type { Meta, StoryObj } from '@storybook/react-vite';
import { Field, PasswordInput } from '@cinedex/atoms';

/**
 * A password input with an in-field reveal toggle.
 *
 * Hand-rolled rather than Radix's `unstable_PasswordToggleField` — that
 * primitive is still behind an `unstable_` prefix, and the whole of the
 * behaviour it would replace is one `useState`. It is an `Input` underneath, so
 * it picks up a surrounding `Field`'s id and `aria-*` the same way.
 */
const meta = {
  title: 'Atoms/PasswordInput',
  component: PasswordInput,
  tags: ['autodocs'],
  args: {
    placeholder: '••••••••',
    'aria-label': 'Password',
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PasswordInput>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** Show/Hide swaps the input's `type`; the value never leaves the field. */
export const WithValue: Story = {
  args: { defaultValue: 'hunter2' },
};

export const Disabled: Story = {
  args: { disabled: true, defaultValue: 'hunter2' },
};

/** The `Field` supplies the label row — `labelExtra` is where "Forgot?" goes. */
export const InAField: Story = {
  args: { defaultValue: 'hunter2', 'aria-label': undefined },
  render: (args) => (
    <Field label="Password" labelExtra={<a href="#forgot">Forgot?</a>}>
      <PasswordInput {...args} />
    </Field>
  ),
};

/** Rejected: the warning surface follows the field's `error`, not a prop here. */
export const Rejected: Story = {
  args: { defaultValue: 'password12345', 'aria-label': undefined },
  render: (args) => (
    <Field
      label="New password"
      error="Appears in a known breach corpus — pick another."
    >
      <PasswordInput {...args} />
    </Field>
  ),
};
