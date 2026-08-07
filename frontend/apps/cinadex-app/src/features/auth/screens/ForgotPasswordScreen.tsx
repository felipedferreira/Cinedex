import { useEffect, useState } from 'react';
import { TextField } from '@cinedex/components';
import {
  Alert,
  AuthActionLink,
  AuthButton,
  AuthCard,
  AuthLayout,
  AuthLink,
  StatPair,
} from '../components';

const RESEND_COOLDOWN_SECONDS = 58;

function formatCountdown(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

export function ForgotPasswordScreen() {
  const [step, setStep] = useState<'request' | 'sent'>('request');
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
        <AuthCard
          eyebrow="Step 1 of 2"
          kicker="Recovery · Request"
          title="Check your inbox"
          footnote="Recovery is rate-limited separately from sign-in. Timing is equalised across both outcomes."
        >
          <Alert>
            <p className="m-0">
              If an account exists for{' '}
              <span className="font-mono text-[12.5px] text-text-h">
                {email}
              </span>
              , a reset link is on its way.
            </p>
            <p className="mt-1.5 mb-0 font-mono text-[11px] text-text">
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
          <AuthButton
            variant="outline"
            disabled={resendIn > 0}
            onClick={() => {
              setResendIn(RESEND_COOLDOWN_SECONDS);
            }}
          >
            Send again
          </AuthButton>
          <div className="flex items-baseline justify-between gap-2.5 border-t border-border pt-3 font-mono text-[11.5px] text-text">
            <span>Wrong address?</span>
            <AuthActionLink
              onClick={() => {
                setStep('request');
              }}
            >
              Start over
            </AuthActionLink>
          </div>
        </AuthCard>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout>
      <AuthCard
        eyebrow="Step 1 of 2"
        kicker="Recovery · Request"
        title="Reset your password"
        description="Enter the email on your account and we'll send a reset link."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            setResendIn(RESEND_COOLDOWN_SECONDS);
            setStep('sent');
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
          <AuthButton type="submit">Send reset link</AuthButton>
          <div className="flex items-baseline justify-between gap-2.5 border-t border-border pt-3 font-mono text-[11.5px] text-text">
            <span>Remembered it?</span>
            <AuthLink to="/login">Sign in</AuthLink>
          </div>
        </form>
      </AuthCard>
    </AuthLayout>
  );
}
