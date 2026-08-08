import type { Meta, StoryObj } from '@storybook/react-vite';
import { Field, Input } from '@cinedex/atoms';

/**
 * The bare text-input surface. Inside a `Field` it picks up that field's id and
 * `aria-*` from context; outside one it is just a styled input.
 *
 * It takes no `id` — that is `Field`'s to generate — and no `invalid` prop: the
 * rejected surface follows the field's error, so a control cannot look valid
 * while the message below it says otherwise.
 */
const meta = {
  title: 'Atoms/Input',
  component: Input,
  tags: ['autodocs'],
  args: {
    placeholder: 'you@example.com',
    'aria-label': 'Email',
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Input>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithValue: Story = {
  args: { defaultValue: 'ada@example.com' },
};

export const Disabled: Story = {
  args: { disabled: true, defaultValue: 'ada@example.com' },
};

/** Unknown props reach the element, so `type` behaves as it does natively. */
export const SearchType: Story = {
  args: {
    type: 'search',
    placeholder: 'Blade Runner',
    'aria-label': 'Search titles',
  },
};

/**
 * The invalid surface comes from the surrounding `Field`, not from a prop on the
 * input — `useFieldControl()` hands it `aria-invalid` and the `aria-describedby`
 * of the message. Standalone, the input carries whatever ids the caller passes.
 */
export const InsideAField: Story = {
  args: { defaultValue: 'not-an-email', 'aria-label': undefined },
  render: (args) => (
    <Field label="Email" error="Enter a valid email address">
      <Input {...args} />
    </Field>
  ),
};
