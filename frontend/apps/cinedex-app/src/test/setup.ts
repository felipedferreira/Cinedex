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

// Automatically unmount and clean up the DOM after each test.
afterEach(() => {
  cleanup();
});
