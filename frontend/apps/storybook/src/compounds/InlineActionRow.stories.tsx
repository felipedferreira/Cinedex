import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/atoms';
import { InlineActionRow } from '@cinedex/compounds';

/**
 * The prompt-and-action row that closes a card — "No account? · Create one".
 *
 * It exists because three screens inlined the same six utilities each time. The
 * top border is its own, so it needs no separator above it.
 */
const meta = {
  title: 'Compounds/InlineActionRow',
  component: InlineActionRow,
  tags: ['autodocs'],
  args: {
    children: 'No account?',
    action: <a href="#register">Create one</a>,
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof InlineActionRow>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const BackToSignIn: Story = {
  args: {
    children: 'Remembered it?',
    action: <a href="#login">Sign in</a>,
  },
};

/** `action` is a `ReactNode`, so it does not have to be a link. */
export const WithButtonAction: Story = {
  args: {
    children: 'Code not arrived?',
    action: (
      <Button variant="ghost" size="sm">
        Send again
      </Button>
    ),
  },
};

/** A long prompt wraps against the action rather than pushing it out of the row. */
export const LongPrompt: Story = {
  args: {
    children: 'Signing in from a device you do not recognise?',
    action: <a href="#help">Get help</a>,
  },
};
