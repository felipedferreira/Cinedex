import { useEffect, useState } from 'react';
import type { FC } from 'react';
import { Button, OtpInput } from '@cinedex/frames';
import { AuthLayout } from '@cinedex/shots';
import { AuthActionLink } from '../link/AuthLink';
import { CinedexAuthCard } from './CinedexAuthCard';
import { formatCountdown } from './formatCountdown';

const CODE_LIFETIME_SECONDS = 60;

export interface TwoFactorScreenProps {
  codeLength?: number;
  onSubmit?: (values: { code: string }) => void;
  onResend?: () => void;
}

/**
 * Presentational on purpose: the backend has no MFA yet, so nothing can drive
 * this screen for real. It renders against local state, ready to wire.
 */
export const TwoFactorScreen: FC<TwoFactorScreenProps> = ({
  codeLength = 6,
  onSubmit,
  onResend,
}) => {
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
        title="Two-factor code"
        description={`${String(codeLength)} digits from your authenticator app for this account.`}
      >
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit?.({ code });
          }}
        >
          <OtpInput
            label="Verification code"
            length={codeLength}
            value={code}
            onChange={setCode}
          />
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
            disabled={code.length < codeLength}
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
};
