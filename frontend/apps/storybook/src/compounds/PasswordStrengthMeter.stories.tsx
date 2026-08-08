import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  PasswordChecklist,
  PasswordField,
  PasswordStrengthMeter,
  strengthFromRequirements,
} from '@cinedex/compounds';

/**
 * The strength bar and its verdict, side by side.
 *
 * The bar is atoms' `ProgressBar` — Radix `Progress` — so it carries
 * `role="progressbar"` and the `aria-value*` set that the hand-rolled bar it
 * replaced did not. Both props normally come from `strengthFromRequirements`
 * rather than being written out by hand.
 */
const meta = {
  title: 'Compounds/PasswordStrengthMeter',
  component: PasswordStrengthMeter,
  tags: ['autodocs'],
  args: {
    ratio: 0.5,
    label: 'Weak',
  },
  argTypes: {
    ratio: { control: { type: 'range', min: 0, max: 1, step: 0.05 } },
  },
  decorators: [
    (Story) => (
      <div className="max-w-[420px]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PasswordStrengthMeter>;

export default meta;
type Story = StoryObj<typeof meta>;

export const TooWeak: Story = { args: { ratio: 0, label: 'Too weak' } };

export const Weak: Story = { args: { ratio: 0.5, label: 'Weak' } };

export const Strong: Story = { args: { ratio: 1, label: 'Strong' } };

/**
 * `strengthFromRequirements` is the pure helper the two props come from. It
 * gives no partial credit beyond the bar's own fill — anything short of every
 * requirement reads "Weak".
 */
export const FromRequirements: Story = {
  render: () => {
    const strength = strengthFromRequirements([
      { label: '12 characters or more', met: true },
      { label: 'Not your email or username', met: false },
    ]);
    return <PasswordStrengthMeter {...strength} />;
  },
};

/**
 * The helper, the meter and the checklist reacting to a real value, exactly as
 * `CreateAccountScreen` drives them.
 */
export const LiveFromAPasswordField: Story = {
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
        <PasswordStrengthMeter {...strength} />
        <PasswordChecklist requirements={requirements} />
      </div>
    );
  },
};
