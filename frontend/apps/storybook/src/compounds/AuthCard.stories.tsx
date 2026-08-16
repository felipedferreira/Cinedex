import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/atoms';
import { AuthCard, AuthLayout } from '@cinedex/compounds';

const meta = {
  title: 'Compounds/AuthCard',
  component: AuthCard,
  tags: ['autodocs'],
  args: {
    eyebrow: 'CIN · Auth',
    kicker: 'Session · Sign in',
    title: 'Sign in',
    description: 'Cinedex catalog — production.',
    children: (
      <Button variant="solid" size="block">
        Continue
      </Button>
    ),
  },
  argTypes: {
    kickerTone: {
      control: 'inline-radio',
      options: ['neutral', 'warning', 'success', 'accent'],
    },
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
 * package boundary working: `@cinedex/compounds` knows *where* a brand goes,
 * not which one — see `Solution/Screens` for the same card wearing Cinedex's.
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

export const WarningKicker: Story = {
  args: {
    kicker: 'Session · Locked',
    kickerTone: 'warning',
    title: 'Too many attempts',
    description:
      'Sign-in for this account is paused. Nothing is wrong with your password.',
  },
};
