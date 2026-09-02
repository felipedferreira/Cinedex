import { useState } from 'react';
import type { FC } from 'react';
import { Button, Checkbox } from '@cinedex/atoms';
import { AuthLayout, PasswordField } from '@cinedex/compounds';
import { CinedexAuthCard } from './CinedexAuthCard';

const MIN_LENGTH = 12;

// A tiny illustrative sample, not a real breach-corpus lookup — that check is
// server-side (Identity's password validators). This only exists so the
// "rejected" state the design specifies is reachable by typing, not hardcoded.
const SAMPLE_BREACHED_PASSWORDS = new Set([
  'password12345',
  '123456789012',
  'qwertyuiop123',
]);

export interface ResetPasswordScreenProps {
  /** Shown in the description when the reset link identifies an account. */
  email?: string;
  onSubmit?: (values: { password: string; signOutEverywhere: boolean }) => void;
}

export const ResetPasswordScreen: FC<ResetPasswordScreenProps> = ({
  email,
  onSubmit,
}) => {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [signOutEverywhere, setSignOutEverywhere] = useState(true);

  const tooShort = password.length > 0 && password.length < MIN_LENGTH;
  const breached = SAMPLE_BREACHED_PASSWORDS.has(password);
  const passwordError = breached
    ? 'Appears in a known breach corpus — pick another.'
    : tooShort
      ? `Must be at least ${String(MIN_LENGTH)} characters.`
      : undefined;

  const confirmError =
    confirmPassword.length > 0 && confirmPassword !== password
      ? "Doesn't match the new password."
      : undefined;

  const canSubmit =
    password.length >= MIN_LENGTH && !breached && confirmPassword === password;

  return (
    <AuthLayout>
      <CinedexAuthCard
        eyebrow="Step 2 of 2"
        title="Set a new password"
        description={
          email
            ? `Link verified for ${email}.`
            : 'Link verified for this account.'
        }
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit?.({ password, signOutEverywhere });
          }}
        >
          <PasswordField
            label={passwordError ? 'New password — rejected' : 'New password'}
            autoComplete="new-password"
            value={password}
            onChange={(event) => {
              setPassword(event.target.value);
            }}
            error={passwordError}
          />
          <PasswordField
            label="Confirm password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(event) => {
              setConfirmPassword(event.target.value);
            }}
            error={confirmError}
          />
          <Checkbox
            label="Sign out of all other sessions"
            checked={signOutEverywhere}
            onCheckedChange={(checked) => {
              setSignOutEverywhere(checked === true);
            }}
          />
          <Button
            type="submit"
            variant="solid"
            size="block"
            disabled={!canSubmit}
          >
            Save and sign in
          </Button>
        </form>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
