import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  CreateAccountScreen,
  ResetPasswordScreen,
  ScreenTransition,
  SignInScreen,
  SignedOutScreen,
  TwoFactorScreen,
  useCaptureOutgoing,
  type TransitionVariant,
} from '@cinedex/solution';

/**
 * The rack-focus transition across the whole auth flow — the review surface for
 * the motion, next to the components it moves.
 *
 * Three of these variants are unreachable in the running app: the lockout and
 * the two-factor step need backend support that does not exist, and the
 * account-ready hold has no route. This is where they can be seen at all.
 */

interface RailScreen {
  id: string;
  label: string;
  variant: TransitionVariant;
  render: () => ReactNode;
}

/**
 * Two of the design's ten screens have no component in `@cinedex/solution`: the
 * source design marks their copy as an unreviewed draft, so they stand in as
 * labelled placeholders rather than shipping wording nobody signed off.
 */
function NotBuilt({ name }: { name: string }) {
  return (
    <div className="flex min-h-svh items-center justify-center bg-bg p-6">
      <div className="w-full max-w-[420px] rounded-md border border-accent-border bg-accent-bg p-6">
        <p className="m-0 font-mono text-label font-semibold tracking-eyebrow text-accent uppercase">
          Not built
        </p>
        <h1 className="mt-2 mb-0 text-title font-bold tracking-tight text-text-h">
          {name}
        </h1>
        <p className="mt-2 mb-0 text-body text-text">
          No component upstream yet. The source design marks this screen&rsquo;s
          copy as a draft for review, so it is deliberately not shipped in{' '}
          <code>@cinedex/solution</code>.
        </p>
      </div>
    </div>
  );
}

const SCREENS: RailScreen[] = [
  {
    id: '01',
    label: '01 Sign in',
    variant: 'forward',
    render: () => <SignInScreen />,
  },
  {
    id: '02',
    label: '02 Two-factor code',
    variant: 'forward',
    render: () => <TwoFactorScreen />,
  },
  {
    id: '03',
    label: '03 Too many attempts',
    variant: 'lockout',
    render: () => <SignInScreen locked />,
  },
  {
    id: '04',
    label: '04 Create account',
    variant: 'forward',
    render: () => <CreateAccountScreen />,
  },
  {
    id: '05',
    label: '05 Verify your email',
    variant: 'forward',
    render: () => <NotBuilt name="Verify your email" />,
  },
  {
    id: '06',
    label: '06 Account ready',
    variant: 'accountReady',
    render: () => <NotBuilt name="Account ready" />,
  },
  {
    id: '07',
    label: '07 Reset your password',
    variant: 'forward',
    render: () => <ResetPasswordScreen email="felipe@cinedex.io" />,
  },
  {
    id: '08',
    label: '08 Set a new password',
    variant: 'coldLoad',
    render: () => <ResetPasswordScreen email="felipe@cinedex.io" />,
  },
  {
    id: '09',
    label: '09 Signed out',
    variant: 'back',
    render: () => <SignedOutScreen />,
  },
];

/**
 * Hands the rail's controls a capture function from inside the provider.
 *
 * `useCaptureOutgoing` only resolves inside a `ScreenTransition`, but the chips
 * have to render *outside* the animated pane or they blur out along with the
 * screen. This renders nothing and exists purely to cross that boundary.
 *
 * If it is flattened away, the capture silently returns the no-op and every move
 * degrades to a plain `coldLoad` fade-in — a working page with the feature
 * switched off. The tell when reviewing: click a chip and watch the *outgoing*
 * screen. If it blurs and shrinks away, capture is working. If it simply
 * vanishes and the new one fades in, it is not.
 */
function CaptureBridge({
  onReady,
}: {
  onReady: (capture: () => void) => void;
}) {
  const capture = useCaptureOutgoing();

  useEffect(() => {
    onReady(capture);
  }, [capture, onReady]);

  return null;
}

function Chips({
  index,
  onGo,
}: {
  index: number;
  onGo: (next: number) => void;
}) {
  const current = SCREENS[index];

  return (
    <div className="flex flex-col gap-3 border-b border-border bg-bg p-4">
      <div className="flex flex-wrap gap-1.5">
        {SCREENS.map((screen, i) => (
          <button
            key={screen.id}
            type="button"
            onClick={() => {
              onGo(i);
            }}
            className={
              i === index
                ? 'rounded-sm border border-text-h bg-text-h px-3 py-1.5 font-mono text-label text-bg uppercase'
                : 'rounded-sm border border-border bg-bg px-3 py-1.5 font-mono text-label text-text uppercase'
            }
          >
            {screen.label}
          </button>
        ))}
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          onClick={() => {
            onGo(index - 1);
          }}
          className="rounded-sm border border-border px-4 py-2 font-mono text-label text-text uppercase"
        >
          &larr; Prev
        </button>
        <button
          type="button"
          onClick={() => {
            onGo(index + 1);
          }}
          className="rounded-sm border border-text-h bg-text-h px-4 py-2 font-mono text-label text-bg uppercase"
        >
          Next &rarr;
        </button>
        <span className="ml-2 font-mono text-footnote text-text">
          Now showing <b className="text-text-h">{current.label}</b> &middot;{' '}
          {current.variant}
        </span>
      </div>
    </div>
  );
}

function Rail() {
  const [index, setIndex] = useState(0);
  const captureRef = useRef<() => void>(() => undefined);

  const onReady = useCallback((capture: () => void) => {
    captureRef.current = capture;
  }, []);

  const go = (next: number) => {
    captureRef.current();
    setIndex((next + SCREENS.length) % SCREENS.length);
  };

  const current = SCREENS[index];

  return (
    <div className="flex flex-col">
      <Chips index={index} onGo={go} />
      <ScreenTransition transitionKey={current.id} variant={current.variant}>
        <CaptureBridge onReady={onReady} />
        {current.render()}
      </ScreenTransition>
    </div>
  );
}

const meta = {
  title: 'Solution/AuthTransitions',
  component: Rail,
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof Rail>;

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * Step through with Prev / Next, or jump with a chip.
 *
 * The spec: outgoing 520ms, opacity 1→0, blur 0→9px, scale 1→0.94, easing
 * `cubic-bezier(.4, 0, .6, 1)`. Incoming 640ms delayed 180ms, blur 11px→0,
 * scale 1.05→1, easing `cubic-bezier(.16, .8, .24, 1)`. 820ms door to door,
 * with a 340ms overlap where both screens are defocused and neither is
 * readable. Under `prefers-reduced-motion` every variant collapses to the same
 * 200ms opacity cross-fade.
 */
export const RackFocus: Story = {};
