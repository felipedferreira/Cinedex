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
 * jsdom has no `matchMedia` either, and `Brand`'s animated variants call it to
 * check `prefers-reduced-motion` before starting a `requestAnimationFrame`
 * loop — jsdom has no `requestAnimationFrame` at all, so defaulting `matches`
 * to `true` here keeps every test on the synchronous "settle immediately"
 * branch unless a test overrides this mock and separately polyfills rAF
 * itself. This is also the actual scenario the animation tests assert against
 * — the multi-frame rAF path is exercised by hand in the browser, not here.
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
