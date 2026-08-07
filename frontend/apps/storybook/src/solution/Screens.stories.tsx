import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  CreateAccountScreen,
  ForgotPasswordScreen,
  ResetPasswordScreen,
  SignInScreen,
  SignedOutScreen,
  TwoFactorScreen,
} from '@cinedex/solution';

/**
 * Cinedex's seven auth states, end to end.
 *
 * These render with **no router and no mock**. `@cinedex/solution` navigates
 * through whatever link component the host injects, and `preview.tsx` injects
 * nothing — so the links here are plain anchors. That is the whole reason the
 * screens live in a package instead of in the app.
 */
const meta = {
  title: 'Solution/Screens',
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const SignIn: Story = {
  render: () => <SignInScreen />,
};

/** Unreachable in the running app — nothing distinguishes a lockout yet. */
export const SignInLocked: Story = {
  render: () => <SignInScreen locked />,
};

export const TwoFactor: Story = {
  render: () => <TwoFactorScreen />,
};

export const CreateAccount: Story = {
  render: () => <CreateAccountScreen />,
};

export const ForgotPassword: Story = {
  render: () => <ForgotPasswordScreen />,
};

export const ResetPassword: Story = {
  render: () => <ResetPasswordScreen email="felipe@cinedex.io" />,
};

export const SignedOut: Story = {
  render: () => <SignedOutScreen />,
};
