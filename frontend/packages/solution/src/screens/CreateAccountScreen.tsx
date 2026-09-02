import { useState } from 'react';
import type { FC } from 'react';
import { Button, Checkbox, TextField } from '@cinedex/atoms';
import {
  AuthLayout,
  InlineActionRow,
  PasswordChecklist,
  PasswordField,
  PasswordStrengthMeter,
  strengthFromRequirements,
} from '@cinedex/compounds';
import { AuthLink } from '../link/AuthLink';
import { CinedexAuthCard } from './CinedexAuthCard';

export interface CreateAccountValues {
  email: string;
  username: string;
  password: string;
}

export interface CreateAccountScreenProps {
  onSubmit?: (values: CreateAccountValues) => void;
}

export const CreateAccountScreen: FC<CreateAccountScreenProps> = ({
  onSubmit,
}) => {
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [acceptedTerms, setAcceptedTerms] = useState(false);

  const requirements = [
    { label: '12 characters or more', met: password.length >= 12 },
    {
      label: 'Not your email or username',
      met:
        password.length > 0 &&
        password.toLowerCase() !== email.toLowerCase() &&
        password.toLowerCase() !== username.toLowerCase(),
    },
  ];
  const strength = strengthFromRequirements(requirements);
  const canSubmit =
    email.length > 0 &&
    username.length > 0 &&
    requirements.every((requirement) => requirement.met) &&
    acceptedTerms;

  return (
    <AuthLayout>
      <CinedexAuthCard title="Create account">
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit?.({ email, username, password });
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
          <TextField
            label="Username"
            autoComplete="username"
            value={username}
            onChange={(event) => {
              setUsername(event.target.value);
            }}
          />
          <div className="flex flex-col gap-2.5">
            <PasswordField
              label="Password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => {
                setPassword(event.target.value);
              }}
            />
            {password ? (
              <PasswordStrengthMeter
                ratio={strength.ratio}
                label={strength.label}
              />
            ) : null}
            <PasswordChecklist requirements={requirements} />
            <p className="m-0 font-mono text-footnote text-text">
              Also checked against the breached-password corpus on submit.
            </p>
          </div>
          <Checkbox
            label="I accept the catalog terms and moderation policy"
            checked={acceptedTerms}
            onCheckedChange={(checked) => {
              setAcceptedTerms(checked === true);
            }}
          />
          <Button
            type="submit"
            variant="solid"
            size="block"
            disabled={!canSubmit}
          >
            Create account
          </Button>
          <InlineActionRow action={<AuthLink to="/login">Sign in</AuthLink>}>
            Already registered?
          </InlineActionRow>
        </form>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
