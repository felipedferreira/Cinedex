import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  Card,
  Field,
  Label,
  PasswordInput,
  ProgressBar,
  Separator,
  VisuallyHidden,
} from '@cinedex/atoms';

/**
 * The remaining primitives, gathered in one place — each is small enough that a
 * file of its own would be mostly frontmatter.
 */
const meta = {
  title: 'Atoms/Primitives',
  tags: ['autodocs'],
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const CardSurface: Story = {
  render: () => (
    <Card className="max-w-[420px] p-5">
      <p className="text-body text-text">
        The raised surface: hairline border, page background, themed shadow. It
        owns no padding of its own — this story adds `p-5`.
      </p>
    </Card>
  ),
};

export const FieldWithPasswordInput: Story = {
  render: () => (
    <div className="max-w-[420px]">
      <Field label="Password" labelExtra={<a href="#forgot">Forgot?</a>}>
        <PasswordInput defaultValue="hunter2" />
      </Field>
    </div>
  ),
};

export const FieldRejected: Story = {
  render: () => (
    <div className="max-w-[420px]">
      <Field
        label="New password — rejected"
        error="Appears in a known breach corpus — pick another."
      >
        <PasswordInput defaultValue="password12345" />
      </Field>
    </div>
  ),
};

export const Progress: Story = {
  render: () => (
    <div className="flex max-w-[420px] flex-col gap-4">
      <ProgressBar ratio={0} label="Empty" />
      <ProgressBar ratio={0.5} label="Half" />
      <ProgressBar ratio={1} label="Full" />
    </div>
  ),
};

export const LabelTones: Story = {
  render: () => (
    <div className="flex flex-col gap-2">
      <Label>Default label</Label>
      <Label tone="error">Rejected label</Label>
    </div>
  ),
};

export const SeparatorRule: Story = {
  render: () => (
    <div className="flex max-w-[420px] flex-col gap-3">
      <span className="text-body text-text">Above</span>
      <Separator />
      <span className="text-body text-text">Below</span>
    </div>
  ),
};

export const HiddenText: Story = {
  render: () => (
    <p className="text-body text-text">
      There is a visually hidden heading before this paragraph — inspect the
      accessibility tree to find it.
      <VisuallyHidden>Section: hidden but announced</VisuallyHidden>
    </p>
  ),
};
