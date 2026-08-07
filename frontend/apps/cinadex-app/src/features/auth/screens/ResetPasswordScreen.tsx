import { useState } from 'react';
import {
  AuthButton,
  AuthCard,
  AuthLayout,
  Checkbox,
  PasswordField,
} from '../components';

const MIN_LENGTH = 12;

// A tiny illustrative sample, not a real breach-corpus lookup — that check is
// server-side (Identity's password validators). This only exists so the
// "rejected" state the design specifies is reachable by typing, not hardcoded.
const SAMPLE_BREACHED_PASSWORDS = new Set([
  'password12345',
  '123456789012',
  'qwertyuiop123',
]);

export function ResetPasswordScreen({ email }: { email?: string }) {
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
      <AuthCard
        eyebrow="Step 2 of 2"
        kicker="Recovery · New password"
        title="Set a new password"
        description={
          email
            ? `Link verified for ${email}.`
            : 'Link verified for this account.'
        }
        footnote="The reset link is single-use — it stops working once your password changes."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
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
            onChange={(event) => {
              setSignOutEverywhere(event.target.checked);
            }}
          />
          <AuthButton type="submit" disabled={!canSubmit}>
            Save and sign in
          </AuthButton>
        </form>
      </AuthCard>
    </AuthLayout>
  );
}
