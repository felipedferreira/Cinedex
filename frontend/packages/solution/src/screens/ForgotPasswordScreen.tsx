import { useEffect, useState } from 'react';
import { Alert, Button, TextField } from '@cinedex/atoms';
import { AuthLayout, InlineActionRow, StatPair } from '@cinedex/compounds';
import { AuthActionLink, AuthLink } from '../link/AuthLink';
import { useCaptureOutgoing } from '../transitions/captureContext';
import { ScreenTransition } from '../transitions/ScreenTransition';
import { CinedexAuthCard } from './CinedexAuthCard';
import { formatCountdown } from './formatCountdown';

const RESEND_COOLDOWN_SECONDS = 58;

export interface ForgotPasswordScreenProps {
  onSubmit?: (values: { email: string }) => void;
}

type Step = 'request' | 'sent';

/**
 * The request/sent step is the auth flow's only forward *and* backward edge that
 * a user can reach by clicking today, and neither move is a navigation — both
 * are this one component's state. So the transition host sits here rather than
 * only at the router, with the step as its key.
 *
 * Splitting the screen in two is what makes that possible: this wrapper owns the
 * step so it can key the host on it, while the body renders whichever step it is
 * handed. The body is not remounted across the change — same element type, same
 * position — so the email the user typed and the resend countdown both survive.
 */
export function ForgotPasswordScreen(props: ForgotPasswordScreenProps) {
  const [step, setStep] = useState<Step>('request');

  return (
    <ScreenTransition
      transitionKey={`/forgot-password#${step}`}
      // Sending the link advances; starting over pulls back out of the flow.
      variant={step === 'sent' ? 'forward' : 'back'}
    >
      <ForgotPasswordScreenBody {...props} step={step} onStepChange={setStep} />
    </ScreenTransition>
  );
}

interface ForgotPasswordScreenBodyProps extends ForgotPasswordScreenProps {
  step: Step;
  onStepChange: (next: Step) => void;
}

function ForgotPasswordScreenBody({
  onSubmit,
  step,
  onStepChange,
}: ForgotPasswordScreenBodyProps) {
  const [email, setEmail] = useState('');
  const [resendIn, setResendIn] = useState(RESEND_COOLDOWN_SECONDS);
  const capture = useCaptureOutgoing();

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
          kicker="Recovery · Request"
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
                  capture();
                  onStepChange('request');
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
        kicker="Recovery · Request"
        title="Reset your password"
        description="Enter the email on your account and we'll send a reset link."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            capture();
            setResendIn(RESEND_COOLDOWN_SECONDS);
            onStepChange('sent');
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
}
