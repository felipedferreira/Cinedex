import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/frames';
import { AuthCard, AuthLayout, InlineActionRow } from '@cinedex/shots';

/**
 * Centres a narrow column in the viewport — the page frame every auth screen
 * sits in, and the decorator the `AuthCard` stories render inside.
 *
 * It owns the page background, the viewport centring and the 420px column, so a
 * screen supplies only the stack that goes in it. These stories run
 * `layout: 'fullscreen'` because a frame that fills the viewport cannot be shown
 * inside Storybook's default padding.
 */
const meta = {
  title: 'Shots/AuthLayout',
  component: AuthLayout,
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
  args: {
    children: (
      <AuthCard
        eyebrow="Step 1 of 2"
        kicker="Session · Sign in"
        title="Sign in"
        description="Cinedex catalog — production."
      >
        <Button variant="solid" size="block">
          Continue
        </Button>
      </AuthCard>
    ),
  },
} satisfies Meta<typeof AuthLayout>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** The column is a flex stack with its own gap — cards stack without margins. */
export const StackedChildren: Story = {
  args: {
    children: (
      <>
        <AuthCard
          eyebrow="Step 1 of 2"
          kicker="Session · Sign in"
          title="Sign in"
          description="Cinedex catalog — production."
        >
          <Button variant="solid" size="block">
            Continue
          </Button>
        </AuthCard>
        <InlineActionRow action={<a href="#register">Create one</a>}>
          No account?
        </InlineActionRow>
      </>
    ),
  },
};

/** Anything can go in the column — it is a frame, not an auth-specific shell. */
export const ArbitraryContent: Story = {
  args: {
    children: (
      <p className="text-body text-text">
        The frame supplies the background, the centring and the 420px column.
        Nothing here is a card.
      </p>
    ),
  },
};
