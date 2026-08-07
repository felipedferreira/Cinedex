import { useState } from 'react';
import { Link } from '@tanstack/react-router';
import {
  AuthButton,
  AuthCard,
  AuthLayout,
  StatPair,
  authButtonClassName,
} from '../components';

export function SignedOutScreen() {
  const [revokedAt] = useState(() =>
    new Date().toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }),
  );
  const [otherSessions, setOtherSessions] = useState(2);

  return (
    <AuthLayout>
      <AuthCard
        eyebrow="Session ended"
        kicker="Session · Revoked"
        kickerTone="success"
        title="You're signed out"
        description={`This device's refresh-token family was revoked at ${revokedAt}.`}
        footnote="Revocation is bound to the authenticated subject — a stolen token cannot end someone else's session."
      >
        <StatPair
          stats={[
            { value: '1', label: 'Device signed out', tone: 'success' },
            { value: String(otherSessions), label: 'Sessions still active' },
          ]}
        />
        <Link to="/login" className={authButtonClassName('solid')}>
          Sign in again
        </Link>
        <AuthButton
          variant="outline"
          disabled={otherSessions === 0}
          onClick={() => {
            setOtherSessions(0);
          }}
        >
          {otherSessions === 0
            ? 'Signed out everywhere'
            : `Sign out everywhere (${String(otherSessions)})`}
        </AuthButton>
      </AuthCard>
    </AuthLayout>
  );
}
