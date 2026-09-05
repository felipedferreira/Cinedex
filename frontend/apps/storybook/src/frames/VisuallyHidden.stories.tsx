import type { Meta, StoryObj } from '@storybook/react-vite';
import { buttonVariants, VisuallyHidden } from '@cinedex/frames';

/**
 * Content available to screen readers but not painted — for a heading or a
 * status message the visual design already carries some other way.
 *
 * Nothing renders visibly in these stories by design: open the accessibility
 * panel, or inspect the DOM, to see what is there.
 */
const meta = {
  title: 'Frames/VisuallyHidden',
  component: VisuallyHidden,
  tags: ['autodocs'],
  args: {
    children: 'Section: hidden but announced',
  },
} satisfies Meta<typeof VisuallyHidden>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <p className="max-w-[420px] text-body text-text">
      There is a visually hidden heading before this paragraph — inspect the
      accessibility tree to find it.
      <VisuallyHidden {...args} />
    </p>
  ),
};

/**
 * The common use: naming a control whose visible content is a glyph. The glyph
 * is `aria-hidden`, so the button's accessible name is the hidden text alone
 * rather than "times".
 */
export const NamingAnIconButton: Story = {
  args: { children: 'Dismiss notice' },
  render: (args) => (
    <button type="button" className={buttonVariants({ variant: 'primary' })}>
      <span aria-hidden="true">×</span>
      <VisuallyHidden {...args} />
    </button>
  ),
};

/**
 * A status message that the design conveys visually — here by the field turning
 * warning-toned — but that a screen reader would otherwise never receive.
 */
export const StatusOnly: Story = {
  args: { children: 'Password rejected: appears in a known breach corpus.' },
  render: (args) => (
    <div className="max-w-[420px]">
      <span className="text-body text-warning">Rejected</span>
      <VisuallyHidden {...args} />
    </div>
  ),
};
