import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  InlineActionRow,
  PasswordChecklist,
  PasswordField,
  PasswordStrengthMeter,
  StatPair,
  strengthFromRequirements,
} from '@cinedex/compounds';

const meta = {
  title: 'Compounds/Assemblies',
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const Password: Story = {
  render: () => (
    <PasswordField
      label="Password"
      labelExtra={<a href="#forgot">Forgot?</a>}
      defaultValue="hunter2"
    />
  ),
};

export const PasswordRejected: Story = {
  render: () => (
    <PasswordField
      label="New password — rejected"
      defaultValue="password12345"
      error="Appears in a known breach corpus — pick another."
    />
  ),
};

/** The meter and checklist reacting to a real value, as `CreateAccountScreen` drives them. */
export const StrengthLive: Story = {
  render: function Live() {
    const [password, setPassword] = useState('');
    const requirements = [
      { label: '12 characters or more', met: password.length >= 12 },
      { label: 'Not your email or username', met: password.length > 0 },
    ];
    const strength = strengthFromRequirements(requirements);

    return (
      <div className="flex flex-col gap-2.5">
        <PasswordField
          label="Password"
          value={password}
          onChange={(event) => {
            setPassword(event.target.value);
          }}
        />
        <PasswordStrengthMeter ratio={strength.ratio} label={strength.label} />
        <PasswordChecklist requirements={requirements} />
      </div>
    );
  },
};

export const Stats: Story = {
  render: () => (
    <StatPair
      stats={[
        { value: '12:47', label: 'Unlocks in', tone: 'warning' },
        { value: '0', label: 'Attempts left' },
      ]}
    />
  ),
};

export const StatsSuccess: Story = {
  render: () => (
    <StatPair
      stats={[
        { value: '1', label: 'Device signed out', tone: 'success' },
        { value: '2', label: 'Sessions still active' },
      ]}
    />
  ),
};

export const ActionRow: Story = {
  render: () => (
    <InlineActionRow action={<a href="#register">Create one</a>}>
      No account?
    </InlineActionRow>
  ),
};
