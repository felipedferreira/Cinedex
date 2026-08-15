import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useState } from 'react';
import { useCaptureOutgoing } from './captureContext';
import { ScreenTransition } from './ScreenTransition';

/**
 * `src/test/setup.ts` stubs `matchMedia` with `matches: true`, so the whole
 * suite takes the reduced-motion path unless a test says otherwise. That is the
 * right default everywhere else, and exactly wrong here — a test of the
 * full-motion host that forgets to override it passes for the wrong reason.
 */
function useFullMotion() {
  const original = globalThis.matchMedia;

  beforeEach(() => {
    globalThis.matchMedia = (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    });
  });

  afterEach(() => {
    globalThis.matchMedia = original;
  });
}

function Pane({
  label,
  onGo,
}: {
  label: string;
  onGo: (next: string) => void;
}) {
  const capture = useCaptureOutgoing();

  return (
    <div>
      <h1>Screen {label}</h1>
      <button
        type="button"
        onClick={() => {
          capture();
          onGo(label === 'a' ? 'b' : 'a');
        }}
      >
        Go
      </button>
    </div>
  );
}

function Harness() {
  const [key, setKey] = useState('a');

  return (
    <ScreenTransition transitionKey={key} variant="forward">
      <Pane label={key} onGo={setKey} />
    </ScreenTransition>
  );
}

function go() {
  fireEvent.click(screen.getByRole('button', { name: 'Go' }));
}

describe('ScreenTransition', () => {
  useFullMotion();

  it('renders its children', () => {
    render(<Harness />);

    expect(
      screen.getByRole('heading', { name: 'Screen a' }),
    ).toBeInTheDocument();
  });

  it('keeps a snapshot of the outgoing screen on the page during the move', () => {
    const { container } = render(<Harness />);

    go();

    // Two panes: the live one and the frozen clone.
    expect(container.querySelectorAll('[data-cdx-pane]')).toHaveLength(2);
    expect(
      container.querySelector('[data-cdx-pane="outgoing"]')?.textContent,
    ).toContain('Screen a');
    expect(
      container.querySelector('[data-cdx-pane="incoming"]')?.textContent,
    ).toContain('Screen b');
  });

  it('hides the outgoing snapshot from assistive technology and from hit-testing', () => {
    const { container } = render(<Harness />);

    go();

    const clone = container.querySelector('[data-cdx-pane="outgoing"]');
    expect(clone).toHaveAttribute('aria-hidden', 'true');
    expect(clone).toHaveAttribute('inert');
  });

  it('exposes exactly one heading to assistive technology mid-flight', () => {
    render(<Harness />);

    go();

    // The clone's heading is inside an aria-hidden subtree, so the
    // accessibility tree sees only the incoming one.
    expect(screen.getAllByRole('heading')).toHaveLength(1);
    expect(screen.getByRole('heading')).toHaveTextContent('Screen b');
  });

  it('leaves exactly one snapshot when interrupted mid-transition', () => {
    const { container } = render(<Harness />);

    go();
    go();

    expect(
      container.querySelectorAll('[data-cdx-pane="outgoing"]'),
    ).toHaveLength(1);
  });

  it('removes the snapshot once the move completes', async () => {
    const { container } = render(<Harness />);

    go();
    expect(
      container.querySelector('[data-cdx-pane="outgoing"]'),
    ).toBeInTheDocument();

    await waitFor(
      () => {
        expect(
          container.querySelector('[data-cdx-pane="outgoing"]'),
        ).toBeNull();
      },
      { timeout: 3_000 },
    );
  });

  it('does not steal focus while the screens are still mid-flight', () => {
    render(<Harness />);

    go();

    // The incoming heading must not grab focus until it is readable.
    expect(screen.getByRole('heading')).not.toHaveFocus();
  });

  it('moves focus to the incoming heading once the move completes', async () => {
    render(<Harness />);

    go();

    await waitFor(
      () => {
        expect(screen.getByRole('heading', { name: 'Screen b' })).toHaveFocus();
      },
      { timeout: 3_000 },
    );
  });

  it('does not take focus on the very first render', () => {
    render(<Harness />);

    // A cold load must not hijack focus away from the document.
    expect(screen.getByRole('heading')).not.toHaveFocus();
  });

  it('runs with no capture at all, degrading to an incoming-only move', () => {
    function NoCapture() {
      const [key, setKey] = useState('a');

      return (
        <ScreenTransition transitionKey={key} variant="forward">
          <div>
            <h1>Screen {key}</h1>
            <button
              type="button"
              onClick={() => {
                setKey('b');
              }}
            >
              Go
            </button>
          </div>
        </ScreenTransition>
      );
    }

    const { container } = render(<NoCapture />);

    go();

    expect(container.querySelector('[data-cdx-pane="outgoing"]')).toBeNull();
    expect(screen.getByRole('heading')).toHaveTextContent('Screen b');
  });
});

/**
 * `ForgotPasswordScreen` carries its own `ScreenTransition` for its request/sent
 * step, and in the app that sits inside the router's. A click inside it has to
 * capture at both levels — either one might be the one that moves — but only the
 * level whose key actually changed may mount its snapshot. The other's has to be
 * dropped, or a full-screen freeze-frame stays pinned over the live UI until
 * that level's next transition.
 */
describe('ScreenTransition nesting', () => {
  useFullMotion();

  function Leaf({
    step,
    onStep,
    onLeave,
  }: {
    step: string;
    onStep: (next: string) => void;
    onLeave: () => void;
  }) {
    const capture = useCaptureOutgoing();

    return (
      <div>
        <h1>Step {step}</h1>
        <button
          type="button"
          onClick={() => {
            capture();
            onStep('two');
          }}
        >
          Step
        </button>
        <button
          type="button"
          onClick={() => {
            capture();
            onLeave();
          }}
        >
          Leave
        </button>
      </div>
    );
  }

  function Nested() {
    const [outer, setOuter] = useState('x');
    const [step, setStep] = useState('one');

    return (
      <ScreenTransition transitionKey={outer} variant="forward">
        <ScreenTransition transitionKey={step} variant="forward">
          <Leaf
            step={step}
            onStep={setStep}
            onLeave={() => {
              setOuter('y');
            }}
          />
        </ScreenTransition>
      </ScreenTransition>
    );
  }

  it('moves the outer level when a nested screen navigates away', () => {
    const { container } = render(<Nested />);

    fireEvent.click(screen.getByRole('button', { name: 'Leave' }));

    // The outer snapshot mounted; the inner one, whose key did not change,
    // was dropped rather than pinned over the live UI.
    expect(
      container.querySelectorAll('[data-cdx-pane="outgoing"]'),
    ).toHaveLength(1);
  });

  it('moves only the inner level on an in-screen step change', () => {
    const { container } = render(<Nested />);

    fireEvent.click(screen.getByRole('button', { name: 'Step' }));

    expect(
      container.querySelectorAll('[data-cdx-pane="outgoing"]'),
    ).toHaveLength(1);
    expect(screen.getByRole('heading')).toHaveTextContent('Step two');
  });
});

describe('useCaptureOutgoing', () => {
  it('is a no-op outside a ScreenTransition, so screens stay storyable', () => {
    function Bare() {
      const capture = useCaptureOutgoing();

      return (
        <button
          type="button"
          onClick={() => {
            capture();
          }}
        >
          Go
        </button>
      );
    }

    render(<Bare />);

    expect(() => {
      go();
    }).not.toThrow();
  });
});
