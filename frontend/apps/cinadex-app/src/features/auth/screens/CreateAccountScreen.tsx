import { useState } from 'react';
import { TextField } from '@cinedex/components';
import {
  AuthButton,
  AuthCard,
  AuthLayout,
  AuthLink,
  Checkbox,
  PasswordChecklist,
  PasswordField,
  PasswordStrengthMeter,
  strengthFromRequirements,
} from '../components';

export function CreateAccountScreen() {
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
      <AuthCard
        eyebrow="CIN · Auth"
        kicker="Account · Register"
        title="Create account"
        footnote="Submitting always shows the same confirmation, registered or not. Email verification is required before privileged use."
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
            <p className="m-0 font-mono text-[10.5px] text-text">
              Also checked against the breached-password corpus on submit.
            </p>
          </div>
          <Checkbox
            label="I accept the catalog terms and moderation policy"
            checked={acceptedTerms}
            onChange={(event) => {
              setAcceptedTerms(event.target.checked);
            }}
          />
          <AuthButton type="submit" disabled={!canSubmit}>
            Create account
          </AuthButton>
          <div className="flex items-baseline justify-between gap-2.5 border-t border-border pt-3 font-mono text-[11.5px] text-text">
            <span>Already registered?</span>
            <AuthLink to="/login">Sign in</AuthLink>
          </div>
        </form>
      </AuthCard>
    </AuthLayout>
  );
}
