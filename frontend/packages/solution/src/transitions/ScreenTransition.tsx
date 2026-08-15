import {
  useCallback,
  useLayoutEffect,
  useRef,
  type CSSProperties,
  type ReactNode,
} from 'react';
import { CaptureContext, useCaptureOutgoing } from './captureContext';
import {
  buildRackFocusTimeline,
  PANE_STYLE,
  type TransitionVariant,
} from './rackFocus';

/**
 * Plays the rack-focus transition when `transitionKey` changes.
 *
 * The outgoing screen is a **clone of its own DOM**, not a React subtree kept
 * alive. Keeping the subtree alive is the obvious approach and it does not work
 * here: whatever renders the screens (a router `Outlet`, a step-switching
 * screen) re-renders the "outgoing" copy against the new state, so you end up
 * cross-fading a screen with itself. A clone is frozen by construction.
 *
 * The clone has to be taken **before** React commits the swap. React runs layout
 * effects after the DOM is already updated, and reading a ref during render is
 * not safe with the React Compiler enabled — so the capture is imperative,
 * driven by the event that causes the change. `useCaptureOutgoing` is the hook
 * the triggers call; with no capture, the move degrades to incoming-only rather
 * than breaking.
 */

export interface ScreenTransitionProps {
  /** Identifies the current screen. A change plays the transition. */
  transitionKey: string;
  variant: TransitionVariant;
  children: ReactNode;
}

const STAGE_STYLE: CSSProperties = { position: 'relative' };

const OVERLAY_STYLE: CSSProperties = {
  position: 'absolute',
  inset: 0,
  pointerEvents: 'none',
};

export function ScreenTransition({
  transitionKey,
  variant,
  children,
}: ScreenTransitionProps) {
  const captureParent = useCaptureOutgoing();
  const stageRef = useRef<HTMLDivElement>(null);
  const liveRef = useRef<HTMLDivElement>(null);
  const renderedKey = useRef<string | null>(null);
  const pendingRef = useRef<HTMLElement | null>(null);
  const cloneRef = useRef<HTMLElement | null>(null);
  const timelineRef = useRef<gsap.core.Timeline | null>(null);

  const dropClone = useCallback(() => {
    cloneRef.current?.remove();
    cloneRef.current = null;
  }, []);

  /**
   * The clone is built here but **not** attached — the layout effect mounts it
   * only if the key actually changed.
   *
   * That split is what makes nesting work. `ForgotPasswordScreen` has its own
   * `ScreenTransition` inside the router's, and a link click inside it has to
   * capture at *both* levels, since either one might be the one that moves. If
   * capture attached eagerly, the level whose key did not change would be left
   * with a full-screen snapshot pinned over the live UI until its own next
   * transition. Deferring the attach makes an unused capture free: the detached
   * node is simply dropped on the next one.
   */
  const capture = useCallback(() => {
    captureParent();

    const live = liveRef.current;
    if (!live) {
      return;
    }

    // An interrupted move drops its own clone first, so there is never more
    // than one on the page.
    timelineRef.current?.kill();
    timelineRef.current = null;
    dropClone();

    const clone = live.cloneNode(true) as HTMLElement;
    clone.setAttribute('data-cdx-pane', 'outgoing');
    clone.setAttribute('aria-hidden', 'true');
    clone.setAttribute('inert', '');
    Object.assign(clone.style, OVERLAY_STYLE, PANE_STYLE);
    pendingRef.current = clone;
  }, [captureParent, dropClone]);

  useLayoutEffect(() => {
    const previousKey = renderedKey.current;
    if (previousKey === transitionKey) {
      return;
    }

    const incoming = liveRef.current;
    const stage = stageRef.current;
    if (!incoming || !stage) {
      return;
    }
    renderedKey.current = transitionKey;

    const outgoing = pendingRef.current;
    pendingRef.current = null;
    if (outgoing) {
      stage.append(outgoing);
      cloneRef.current = outgoing;
    }

    const timeline = buildRackFocusTimeline(outgoing, incoming, {
      // With nothing captured there is no screen to leave, which is exactly
      // what `coldLoad` describes.
      variant: outgoing ? variant : 'coldLoad',
      reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)')
        .matches,
    });
    timelineRef.current = timeline;

    timeline.eventCallback('onComplete', () => {
      dropClone();
      timelineRef.current = null;

      // Focus follows the move, but only on a real transition — taking focus on
      // the first render would hijack a cold page load. And only on completion:
      // moving it at the start points a screen reader at a heading that is
      // still blurred out and unreadable.
      if (!outgoing) {
        return;
      }
      const heading = incoming.querySelector('h1');
      if (heading instanceof HTMLElement) {
        heading.setAttribute('tabindex', '-1');
        heading.focus({ preventScroll: true });
      }
    });
    timeline.play();

    return () => {
      // `kill` rather than `revert`, for the same reason `useMarkTimeline` does:
      // rewinding every property on a node React is about to drop is pointless,
      // and on a StrictMode remount it would fight the fresh timeline.
      timeline.kill();

      // Then put back everything this run changed, because an effect has to be
      // safe to run twice. StrictMode tears it down and re-runs it on mount, and
      // without this the re-run early-returns on the key it already recorded —
      // leaving the incoming screen frozen on frame zero, which is opacity 0 and
      // an 11px blur. The app renders blank, and no `progress()`-driven test
      // catches it because none of them ever call `play()`.
      renderedKey.current = previousKey;
      if (outgoing) {
        outgoing.remove();
        cloneRef.current = null;
        // Hand the snapshot back only if nothing newer is staged. A real key
        // change captures *before* this cleanup runs, and overwriting that
        // would mount the previous screen's snapshot over the new one — which
        // reads as a screen cross-fading with a copy of itself.
        pendingRef.current ??= outgoing;
      }
    };
  }, [transitionKey, variant, dropClone]);

  useLayoutEffect(() => dropClone, [dropClone]);

  return (
    <CaptureContext.Provider value={capture}>
      <div ref={stageRef} style={STAGE_STYLE}>
        <div ref={liveRef} data-cdx-pane="incoming" style={PANE_STYLE}>
          {children}
        </div>
      </div>
    </CaptureContext.Provider>
  );
}
