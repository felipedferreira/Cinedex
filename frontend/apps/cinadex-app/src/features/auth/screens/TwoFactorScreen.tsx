import { useEffect, useState } from 'react';
import {
  AuthActionLink,
  AuthButton,
  AuthCard,
  AuthLayout,
  OtpInput,
} from '../components';

const CODE_LIFETIME_SECONDS = 60;

function formatCountdown(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

export function TwoFactorScreen() {
  const [code, setCode] = useState('');
  const [secondsLeft, setSecondsLeft] = useState(CODE_LIFETIME_SECONDS);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setSecondsLeft((current) => Math.max(0, current - 1));
    }, 1_000);
    return () => {
      window.clearInterval(timer);
    };
  }, []);

  return (
    <AuthLayout>
      <AuthCard
        eyebrow="Step 2 of 2"
        kicker="Administrator · Verify"
        kickerTone="accent"
        title="Two-factor code"
        description="Six digits from your authenticator app for this account."
        footnote="Administrator sessions require MFA. Catalog-only accounts skip this step."
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
          }}
        >
          <OtpInput label="Verification code" value={code} onChange={setCode} />
          <div className="flex items-center justify-between gap-2.5 font-mono text-[11px] font-medium tracking-[0.06em] text-text uppercase">
            <span>
              Code expires in{' '}
              <b className="text-text-h tabular-nums">
                {formatCountdown(secondsLeft)}
              </b>
            </span>
            <AuthActionLink
              onClick={() => {
                setCode('');
                setSecondsLeft(CODE_LIFETIME_SECONDS);
              }}
            >
              Resend
            </AuthActionLink>
          </div>
          <AuthButton type="submit" disabled={code.length < 6}>
            Verify
          </AuthButton>
          <div className="flex flex-wrap gap-x-4 gap-y-2 border-t border-border pt-3 font-mono text-[11.5px] text-text">
            <AuthActionLink className="border-accent-border text-accent">
              Use a recovery code
            </AuthActionLink>
            <AuthActionLink className="border-border text-text">
              Sign in as someone else
            </AuthActionLink>
          </div>
        </form>
      </AuthCard>
    </AuthLayout>
  );
}
