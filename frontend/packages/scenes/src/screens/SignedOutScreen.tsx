import { useState } from 'react';
import type { FC } from 'react';
import { Button } from '@cinedex/frames';
import { AuthLayout, StatPair } from '@cinedex/shots';
import { SceneLink } from '../link/SceneLink';
import { CinedexAuthCard } from './CinedexAuthCard';

export interface SignedOutScreenProps {
  /**
   * How many other sessions are still live. Illustrative today — the backend has
   * no session-listing endpoint yet — so it defaults to a sample value.
   */
  initialOtherSessions?: number;
  onSignOutEverywhere?: () => void;
}

/**
 * Presentational on purpose: there is no session-listing or revoke-all endpoint
 * to drive this yet. See `docs/auth-security-model.md`'s "Known gaps".
 */
export const SignedOutScreen: FC<SignedOutScreenProps> = ({
  initialOtherSessions = 2,
  onSignOutEverywhere,
}) => {
  const [revokedAt] = useState(() =>
    new Date().toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }),
  );
  const [otherSessions, setOtherSessions] = useState(initialOtherSessions);

  return (
    <AuthLayout>
      <CinedexAuthCard
        title="You're signed out"
        description={`This device's refresh-token family was revoked at ${revokedAt}.`}
      >
        <StatPair
          stats={[
            { value: '1', label: 'Device signed out', tone: 'success' },
            { value: String(otherSessions), label: 'Sessions still active' },
          ]}
        />
        <Button asChild variant="solid" size="block">
          <SceneLink to="/login">Sign in again</SceneLink>
        </Button>
        <Button
          variant="outline"
          size="block"
          disabled={otherSessions === 0}
          onClick={() => {
            setOtherSessions(0);
            onSignOutEverywhere?.();
          }}
        >
          {otherSessions === 0
            ? 'Signed out everywhere'
            : `Sign out everywhere (${String(otherSessions)})`}
        </Button>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
