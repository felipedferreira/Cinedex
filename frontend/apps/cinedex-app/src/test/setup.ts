import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

/**
 * jsdom has no `ResizeObserver`, but `lib.dom` says every browser does — hence
 * the unconditional assignment rather than a `??=`, which TypeScript would read
 * as a check that can never fail.
 *
 * Radix reaches for it through `useSize` whenever a control participates in a
 * form: `Checkbox` mirrors its rendered size onto the hidden input it submits
 * with. Any test rendering one inside a `<form>` throws without this. A no-op is
 * enough — nothing here asserts on measurements.
 */
class ResizeObserverStub implements ResizeObserver {
  observe = () => undefined;
  unobserve = () => undefined;
  disconnect = () => undefined;
}

globalThis.ResizeObserver = ResizeObserverStub;

/**
 * jsdom has no `matchMedia` either, and `__root.tsx` now renders
 * `@cinedex/solution`'s `ScreenTransition`, which checks
 * `prefers-reduced-motion` before building its timeline. Without this every
 * routed test throws inside a layout effect.
 *
 * `matches` defaults to `true` for the same reason it does in
 * `packages/solution/src/test/setup.ts`: it keeps these tests on the short
 * reduced-motion cross-fade rather than the full 820ms move, so nothing here
 * waits on an animation it does not assert about. The transition's own motion is
 * covered by `rackFocus.test.ts`, and its structure by
 * `ScreenTransition.test.tsx` — what the routing tests care about is that the
 * host is wired up at all.
 */
class MediaQueryListStub implements MediaQueryList {
  matches = true;
  media = '';
  onchange = null;
  addListener = () => undefined;
  removeListener = () => undefined;
  addEventListener = () => undefined;
  removeEventListener = () => undefined;
  dispatchEvent = () => false;

  constructor(media: string) {
    this.media = media;
  }
}

globalThis.matchMedia = (query) => new MediaQueryListStub(query);

// Automatically unmount and clean up the DOM after each test.
afterEach(() => {
  cleanup();
});
