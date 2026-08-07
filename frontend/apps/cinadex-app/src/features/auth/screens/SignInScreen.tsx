import { useState } from 'react';
import { Link } from '@tanstack/react-router';
import { TextField } from '@cinedex/components';
import {
  Alert,
  AuthButton,
  AuthCard,
  AuthLayout,
  AuthLink,
  Checkbox,
  PasswordField,
  StatPair,
  authButtonClassName,
} from '../components';

export function SignInScreen({ locked = false }: { locked?: boolean }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [keepSignedIn, setKeepSignedIn] = useState(true);

  if (locked) {
    return (
      <AuthLayout>
        <AuthCard
          eyebrow="CIN · Auth"
          kicker="Session · Locked"
          kickerTone="warning"
          title="Too many attempts"
          description="Sign-in for this account is paused. Nothing is wrong with your password — the limiter does not say either way."
          footnote="Lockout applies per account and per IP. Both counters must cool down."
        >
          <StatPair
            stats={[
              { value: '12:47', label: 'Unlocks in', tone: 'warning' },
              { value: '0', label: 'Attempts left' },
            ]}
          />
          <Alert tone="warning">
            <p className="m-0 font-mono font-medium">
              Locked at 14:02 UTC · 5 failures / 15 min
            </p>
            <p className="mt-1 mb-0 text-[12.5px]">
              Support cannot lift a lockout early. A completed password reset
              clears it.
            </p>
          </Alert>
          <div className="pointer-events-none opacity-40">
            <TextField
              label="Email"
              type="email"
              value={email}
              disabled
              readOnly
            />
          </div>
          <AuthButton disabled>Sign in</AuthButton>
          <Link
            to="/forgot-password"
            className={authButtonClassName('outline')}
          >
            Reset password instead
          </Link>
        </AuthCard>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <AuthCard
        eyebrow="CIN · Auth"
        kicker="Session · Sign in"
        title="Sign in"
        description="Cinedex catalog — production. Sessions last 30 days on trusted devices."
        footnote="Rate-limited: 5 attempts per 15 min, per account and per IP."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
          }}
        >
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
            }}
          />
          <PasswordField
            label="Password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => {
              setPassword(event.target.value);
            }}
            labelExtra={<AuthLink to="/forgot-password">Forgot?</AuthLink>}
          />
          <Checkbox
            label="Keep me signed in on this device"
            checked={keepSignedIn}
            onChange={(event) => {
              setKeepSignedIn(event.target.checked);
            }}
          />
          <AuthButton type="submit">Sign in</AuthButton>
          <div className="flex items-baseline justify-between gap-2.5 border-t border-border/60 pt-3 font-mono text-[11.5px] text-text">
            <span>No account?</span>
            <AuthLink to="/register">Create one</AuthLink>
          </div>
        </form>
      </AuthCard>
    </AuthLayout>
  );
}
