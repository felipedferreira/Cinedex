import { useEffect, useState } from 'react';
import type { FC } from 'react';
import { Alert, Button, TextField } from '@cinedex/frames';
import { AuthLayout, InlineActionRow, StatPair } from '@cinedex/shots';
import { AuthActionLink, AuthLink } from '../link/AuthLink';
import { CinedexAuthCard } from './CinedexAuthCard';
import { formatCountdown } from './formatCountdown';

const RESEND_COOLDOWN_SECONDS = 58;

export interface ForgotPasswordScreenProps {
  onSubmit?: (values: { email: string }) => void;
}

type Step = 'request' | 'sent';

/**
 * The request/sent step is not a navigation — both moves are this one
 * component's state and neither leaves `/forgot-password`. Holding the step
 * beside the email and the resend countdown is what lets "Start over" return to
 * a form that still has the address the user typed.
 */
export const ForgotPasswordScreen: FC<ForgotPasswordScreenProps> = ({
  onSubmit,
}) => {
  const [step, setStep] = useState<Step>('request');
  const [email, setEmail] = useState('');
  const [resendIn, setResendIn] = useState(RESEND_COOLDOWN_SECONDS);

  useEffect(() => {
    if (step !== 'sent') return;
    const timer = window.setInterval(() => {
      setResendIn((current) => Math.max(0, current - 1));
    }, 1_000);
    return () => {
      window.clearInterval(timer);
    };
  }, [step]);

  if (step === 'sent') {
    return (
      <AuthLayout>
        <CinedexAuthCard
          eyebrow="Step 1 of 2"
          title="Check your inbox"
          footnote="Recovery is rate-limited separately from sign-in. Timing is equalised across both outcomes."
        >
          <Alert>
            <p className="m-0">
              If an account exists for{' '}
              <span className="font-mono text-note text-text-h">{email}</span>,
              a reset link is on its way.
            </p>
            <p className="mt-1.5 mb-0 font-mono text-brand text-text">
              This wording is identical whether or not the address is registered
              — that is deliberate.
            </p>
          </Alert>
          <StatPair
            stats={[
              { value: '30:00', label: 'Link valid for' },
              { value: formatCountdown(resendIn), label: 'Resend in' },
            ]}
          />
          <Button
            variant="outline"
            size="block"
            disabled={resendIn > 0}
            onClick={() => {
              setResendIn(RESEND_COOLDOWN_SECONDS);
            }}
          >
            Send again
          </Button>
          <InlineActionRow
            action={
              <AuthActionLink
                onClick={() => {
                  setStep('request');
                }}
              >
                Start over
              </AuthActionLink>
            }
          >
            Wrong address?
          </InlineActionRow>
        </CinedexAuthCard>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <CinedexAuthCard
        eyebrow="Step 1 of 2"
        title="Reset your password"
        description="Enter the email on your account and we'll send a reset link."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            setResendIn(RESEND_COOLDOWN_SECONDS);
            setStep('sent');
            onSubmit?.({ email });
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
          <Button type="submit" variant="solid" size="block">
            Send reset link
          </Button>
          <InlineActionRow action={<AuthLink to="/login">Sign in</AuthLink>}>
            Remembered it?
          </InlineActionRow>
        </form>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
