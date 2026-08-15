import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {
  createMemoryHistory,
  createRouter,
  RouterProvider,
} from '@tanstack/react-router';
import { routeTree } from './routeTree.gen';

/**
 * Mounts the real route tree, so these exercise the actual `__root.tsx` wiring
 * rather than a transition host in isolation. `ScreenTransition`'s own contract
 * is covered in `@cinedex/solution`; what is under test here is that the router
 * reaches it — that `RouterLink` freezes the outgoing screen before React
 * re-renders, which is the one moment its DOM still exists.
 */
function renderAt(path: string) {
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [path] }),
  });

  return render(<RouterProvider router={router} />);
}

/**
 * `src/test/setup.ts` reports reduced motion, which is right for every other
 * test here — nothing else waits on an animation. These tests do: the outgoing
 * snapshot only exists for the length of the move, and a 200ms reduced-motion
 * cross-fade can finish before Testing Library has finished polling for the
 * incoming screen. That made an earlier version of this file flake.
 *
 * Full motion gives the assertions the design's real 820ms window instead.
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

describe('auth transitions', () => {
  useFullMotion();

  it('wraps the routed screen in a transition stage', async () => {
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    expect(
      container.querySelector('[data-cdx-pane="incoming"]'),
    ).toBeInTheDocument();
  });

  it('does not freeze anything on a cold load, since there is nothing to leave', async () => {
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    expect(container.querySelector('[data-cdx-pane="outgoing"]')).toBeNull();
  });

  it('freezes the outgoing screen when a screen link navigates', async () => {
    const user = userEvent.setup();
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    await user.click(screen.getByRole('link', { name: 'Create one' }));

    // One `waitFor` for both facts: a clone that exists but holds the wrong
    // screen is the failure this is really guarding against — it would mean the
    // capture ran after React had already swapped the DOM.
    await waitFor(() => {
      const clone = container.querySelector('[data-cdx-pane="outgoing"]');
      expect(clone).not.toBeNull();
      expect(clone).toHaveAttribute('aria-hidden', 'true');
      expect(clone?.textContent).toContain('Sign in');
    });
  });

  it('exposes one heading to assistive technology across the move', async () => {
    const user = userEvent.setup();
    const { container } = renderAt('/login');

    await screen.findByRole('heading', { name: 'Sign in' });
    await user.click(screen.getByRole('link', { name: 'Create one' }));

    await waitFor(() => {
      // Assert it while both screens are genuinely on the page — otherwise this
      // passes trivially once the snapshot has been dropped.
      expect(
        container.querySelector('[data-cdx-pane="outgoing"]'),
      ).not.toBeNull();
      expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
        'Create account',
      );
    });
  });
});
