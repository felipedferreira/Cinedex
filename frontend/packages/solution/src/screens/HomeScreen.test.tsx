import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { HomeScreen } from './HomeScreen';

describe('HomeScreen', () => {
  it('links to every screen in the flow', () => {
    render(<HomeScreen />);

    const hrefs = screen
      .getAllByRole('link')
      .map((link) => link.getAttribute('href'));

    expect(hrefs).toEqual([
      '/login',
      '/login?state=locked',
      '/login/verify',
      '/register',
      '/forgot-password',
      '/reset-password',
      '/signed-out',
    ]);
  });

  it('serialises search params onto the href rather than into the path', () => {
    render(<HomeScreen />);

    // The locked-out state is a search param, not a route — writing it into `to`
    // would break any router that matches `to` against its route table.
    expect(screen.getByRole('link', { name: /locked out/i })).toHaveAttribute(
      'href',
      '/login?state=locked',
    );
  });

  it('groups the links in a labelled navigation landmark', () => {
    render(<HomeScreen />);

    expect(
      screen.getByRole('navigation', { name: 'All screens' }),
    ).toBeInTheDocument();
  });

  it('gives each link an accessible name carrying its screen and path', () => {
    render(<HomeScreen />);

    // The whole row is one link, so its accessible name is label + path + note.
    // Verbose, but it means a screen reader gets the caveat ("no MFA backend")
    // along with the destination rather than just a bare route.
    expect(
      screen.getByRole('link', { name: /Two-factor code/ }),
    ).toHaveAccessibleName(expect.stringContaining('/login/verify'));
    expect(
      screen.getByRole('link', { name: /Two-factor code/ }),
    ).toHaveAccessibleName(expect.stringContaining('no MFA'));
  });

  it('distinguishes the two sign-in entries', () => {
    render(<HomeScreen />);

    // Both start "Sign in", so the locked one has to be separable by more than
    // its prefix — otherwise the index cannot express a state that is not a route.
    const signIn = screen.getAllByRole('link', { name: /Sign in/ });
    expect(signIn).toHaveLength(2);
    expect(signIn.map((link) => link.getAttribute('href'))).toEqual([
      '/login',
      '/login?state=locked',
    ]);
  });
});
