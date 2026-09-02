import { useState } from 'react';
import type { FC } from 'react';
import { Alert, Button, Checkbox, TextField } from '@cinedex/atoms';
import {
  AuthLayout,
  InlineActionRow,
  PasswordField,
  StatPair,
} from '@cinedex/compounds';
import { AuthLink } from '../link/AuthLink';
import { SolutionLink } from '../link/SolutionLink';
import { CinedexAuthCard } from './CinedexAuthCard';

export interface SignInValues {
  email: string;
  password: string;
  keepSignedIn: boolean;
}

export interface SignInScreenProps {
  /**
   * The rate-limited state. Unreachable through normal use today — nothing
   * distinguishes a lockout from any other failed login yet — so it is driven
   * by this prop and reviewable at `/login?state=locked`.
   */
  locked?: boolean;
  onSubmit?: (values: SignInValues) => void;
}

export const SignInScreen: FC<SignInScreenProps> = ({
  locked = false,
  onSubmit,
}) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [keepSignedIn, setKeepSignedIn] = useState(true);

  if (locked) {
    return (
      <AuthLayout>
        <CinedexAuthCard
          title="Too many attempts"
          description="Sign-in for this account is paused. Nothing is wrong with your password — the limiter does not say either way."
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
            <p className="mt-1 mb-0 text-note">
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
          <Button variant="solid" size="block" disabled>
            Sign in
          </Button>
          <Button asChild variant="outline" size="block">
            <SolutionLink to="/forgot-password">
              Reset password instead
            </SolutionLink>
          </Button>
        </CinedexAuthCard>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <CinedexAuthCard
        title="Sign in"
        description="Cinedex catalog — production. Sessions last 30 days on trusted devices."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit?.({ email, password, keepSignedIn });
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
            onCheckedChange={(checked) => {
              setKeepSignedIn(checked === true);
            }}
          />
          <Button type="submit" variant="solid" size="block">
            Sign in
          </Button>
          <InlineActionRow
            action={<AuthLink to="/register">Create one</AuthLink>}
          >
            No account?
          </InlineActionRow>
        </form>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
