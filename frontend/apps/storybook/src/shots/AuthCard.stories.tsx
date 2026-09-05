import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/frames';
import { AuthCard, AuthLayout } from '@cinedex/shots';

const meta = {
  title: 'Shots/AuthCard',
  component: AuthCard,
  tags: ['autodocs'],
  args: {
    eyebrow: 'Step 1 of 2',
    kicker: 'Session · Sign in',
    title: 'Sign in',
    description: 'Cinedex catalog — production.',
    children: (
      <Button variant="solid" size="block">
        Continue
      </Button>
    ),
  },
  decorators: [
    (Story) => (
      <AuthLayout>
        <Story />
      </AuthLayout>
    ),
  ],
} satisfies Meta<typeof AuthCard>;

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * With no `brand`, the row above the card holds only the eyebrow. That is the
 * package boundary working: `@cinedex/shots` knows *where* a brand goes,
 * not which one — see `Scenes/Auth Stories` for the same card wearing Cinedex's.
 */
export const Unbranded: Story = {};

export const WithBrand: Story = {
  args: {
    brand: (
      <>
        <span className="grid size-5 place-items-center rounded-sm bg-accent font-mono text-xs font-semibold text-bg">
          A
        </span>
        <span className="font-mono text-brand font-semibold tracking-eyebrow text-text-h uppercase">
          Acme
        </span>
      </>
    ),
    eyebrow: 'ACME · Auth',
  },
};

/**
 * The full header — kicker, title, description — with a `footnote` under the
 * card. This is `HomeScreen`'s shape, which is the only screen that passes a
 * kicker: its title is the bare product name, so the kicker is the only thing
 * saying what the page lists.
 */
export const WithFootnote: Story = {
  args: {
    kicker: 'Catalog · Screens',
    title: 'Cinedex',
    description: 'Every screen in the auth flow, in one place.',
    footnote:
      'Screens live in @cinedex/scenes and render without a router; this app supplies the navigation.',
  },
};
