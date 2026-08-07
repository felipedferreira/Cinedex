import { useEffect, useState } from 'react';
import { Button, OtpInput } from '@cinedex/atoms';
import { AuthLayout } from '@cinedex/compounds';
import { AuthActionLink } from '../link/AuthLink';
import { CinedexAuthCard } from './CinedexAuthCard';
import { formatCountdown } from './formatCountdown';

const CODE_LIFETIME_SECONDS = 60;

export interface TwoFactorScreenProps {
  onSubmit?: (values: { code: string }) => void;
  onResend?: () => void;
}

/**
 * Presentational on purpose: the backend has no MFA yet, so nothing can drive
 * this screen for real. It renders against local state, ready to wire.
 */
export function TwoFactorScreen({ onSubmit, onResend }: TwoFactorScreenProps) {
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
      <CinedexAuthCard
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
            onSubmit?.({ code });
          }}
        >
          <OtpInput label="Verification code" value={code} onChange={setCode} />
          <div className="flex items-center justify-between gap-2.5 font-mono text-brand font-medium tracking-[0.06em] text-text uppercase">
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
                onResend?.();
              }}
            >
              Resend
            </AuthActionLink>
          </div>
          <Button
            type="submit"
            variant="solid"
            size="block"
            disabled={code.length < 6}
          >
            Verify
          </Button>
          <div className="flex flex-wrap gap-x-4 gap-y-2 border-t border-border/60 pt-3 font-mono text-caption text-text">
            <AuthActionLink className="border-accent-border text-accent">
              Use a recovery code
            </AuthActionLink>
            <AuthActionLink className="border-border text-text">
              Sign in as someone else
            </AuthActionLink>
          </div>
        </form>
      </CinedexAuthCard>
    </AuthLayout>
  );
}
