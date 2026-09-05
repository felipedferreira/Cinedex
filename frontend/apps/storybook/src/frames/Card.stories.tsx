import type { Meta, StoryObj } from '@storybook/react-vite';
import { Card, Separator } from '@cinedex/frames';

/**
 * The raised surface: hairline border, page background, the themed shadow.
 *
 * Surface only — it owns no padding and no internal layout, so a shot can
 * lay its own content out without first undoing a default.
 */
const meta = {
  title: 'Frames/Card',
  component: Card,
  tags: ['autodocs'],
  args: {
    className: 'max-w-[420px] p-5',
    children: (
      <p className="text-body text-text">
        The raised surface: hairline border, page background, themed shadow.
      </p>
    ),
  },
} satisfies Meta<typeof Card>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/**
 * Every inset is the caller's. With no padding of its own the card can hold a
 * full-bleed band above content that is inset — a default would have to be
 * undone first.
 */
export const NoPaddingOfItsOwn: Story = {
  args: {
    className: 'max-w-[420px] overflow-hidden',
    children: (
      <>
        <div className="h-20 bg-accent-bg" />
        <p className="p-5 text-body text-text">
          The band above runs to the border; this paragraph is inset by `p-5`.
        </p>
      </>
    ),
  },
};

/** No internal layout either — sections are composed, here with a `Separator`. */
export const Sectioned: Story = {
  args: {
    className: 'max-w-[420px]',
    children: (
      <>
        <p className="p-5 text-body text-text">Blade Runner · 1982</p>
        <Separator />
        <p className="p-5 text-body text-text">Directed by Ridley Scott</p>
      </>
    ),
  },
};
