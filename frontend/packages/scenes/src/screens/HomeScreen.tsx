import type { FC } from 'react';
import { Separator } from '@cinedex/frames';
import { AuthLayout } from '@cinedex/shots';
import { BrandApertureAnimation } from '../Brand/BrandApertureAnimation';
import { SceneLink } from '../link/SceneLink';
import { CinedexAuthCard } from './CinedexAuthCard';

interface ScreenEntry {
  to: string;
  search?: Record<string, string>;
  label: string;
  /** The path as displayed — includes the query string, unlike `to`. */
  display: string;
  note: string;
}

/**
 * Every screen the app can reach. Two of these are states rather than routes of
 * their own, which is exactly why the index is worth having: the locked-out
 * sign-in is only reachable through a search param, and several screens are
 * presentational with no backend to drive them.
 */
const SCREENS: ScreenEntry[] = [
  {
    to: '/login',
    label: 'Sign in',
    display: '/login',
    note: 'Email, password, keep-me-signed-in.',
  },
  {
    to: '/login',
    search: { state: 'locked' },
    label: 'Sign in — locked out',
    display: '/login?state=locked',
    note: 'Rate-limit state. Unreachable in normal use; nothing distinguishes a lockout yet.',
  },
  {
    to: '/login/verify',
    label: 'Two-factor code',
    display: '/login/verify',
    note: 'Six-digit code. Presentational — the backend has no MFA.',
  },
  {
    to: '/register',
    label: 'Create account',
    display: '/register',
    note: 'Live password strength and requirement checklist.',
  },
  {
    to: '/forgot-password',
    label: 'Forgot password',
    display: '/forgot-password',
    note: 'Request, then the deliberately identical confirmation.',
  },
  {
    to: '/reset-password',
    label: 'Set a new password',
    display: '/reset-password',
    note: 'Reads ?email= and ?token= from the reset link.',
  },
  {
    to: '/signed-out',
    label: 'Signed out',
    display: '/signed-out',
    note: 'Presentational — no session-listing endpoint yet.',
  },
];

/**
 * The app's index: a directory of every screen, so the whole flow is reachable
 * without typing URLs. Several screens cannot be reached by clicking through the
 * app — the two-factor step, the signed-out panel and the locked-out sign-in all
 * need backend support that does not exist yet — so this is the only way to
 * review them short of editing the address bar.
 */
export const HomeScreen: FC = () => {
  return (
    <AuthLayout>
      <CinedexAuthCard
        brand={<BrandApertureAnimation />}
        eyebrow="CIN · Index"
        kicker="Catalog · Screens"
        title="Cinedex"
        description="Every screen in the auth flow, in one place. This pass is UI-only — local state and stubbed handlers, no calls to the API yet."
        footnote="Screens live in @cinedex/scenes and render without a router; this app supplies the navigation."
      >
        <nav aria-label="All screens">
          <ul className="m-0 flex list-none flex-col gap-0 p-0">
            {SCREENS.map((screen, index) => (
              <li key={screen.display}>
                {index > 0 ? <Separator className="my-3" /> : null}
                <SceneLink
                  to={screen.to}
                  search={screen.search}
                  className="group flex flex-col gap-1 no-underline"
                >
                  <span className="flex items-baseline justify-between gap-3">
                    <span className="text-body font-semibold text-text-h group-hover:text-accent">
                      {screen.label}
                    </span>
                    <span className="shrink-0 font-mono text-label tracking-label text-text uppercase">
                      {screen.display}
                    </span>
                  </span>
                  <span className="font-mono text-caption leading-[1.45] text-text">
                    {screen.note}
                  </span>
                </SceneLink>
              </li>
            ))}
          </ul>
        </nav>
      </CinedexAuthCard>
    </AuthLayout>
  );
};
