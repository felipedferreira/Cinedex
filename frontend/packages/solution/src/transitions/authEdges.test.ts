import { describe, expect, it } from 'vitest';
import { variantForEdge } from './authEdges';

describe('variantForEdge', () => {
  it('treats a first render as a cold load, whatever the destination', () => {
    expect(variantForEdge(null, '/login', false)).toBe('coldLoad');
    expect(variantForEdge(null, '/reset-password', false)).toBe('coldLoad');
    expect(variantForEdge(null, '/login?state=locked', false)).toBe('coldLoad');
  });

  it('advances forward between sibling auth screens', () => {
    expect(variantForEdge('/login', '/register', false)).toBe('forward');
    expect(variantForEdge('/login', '/forgot-password', false)).toBe('forward');
    expect(variantForEdge('/signed-out', '/login', false)).toBe('forward');
  });

  it('reads a lockout as its own variant, not as progress', () => {
    expect(variantForEdge('/', '/login?state=locked', false)).toBe('lockout');
    expect(variantForEdge('/login', '/login?state=locked', false)).toBe(
      'lockout',
    );
  });

  it('recedes into the signed-out screen, because the user is leaving', () => {
    expect(variantForEdge('/', '/signed-out', false)).toBe('back');
    expect(variantForEdge('/login', '/signed-out', false)).toBe('back');
  });

  it('runs backward whenever history went backward, whatever the map says', () => {
    expect(variantForEdge('/login', '/register', true)).toBe('back');
    expect(variantForEdge('/signed-out', '/login', true)).toBe('back');
  });

  // The history override must not swallow the two variants that carry meaning
  // the direction cannot: a lockout is still a lockout when reached via Back.
  it('does not let the history override outrank a lockout', () => {
    expect(variantForEdge('/login', '/login?state=locked', true)).toBe(
      'lockout',
    );
  });

  it('holds before the account-ready screen', () => {
    expect(variantForEdge('/register', '/account-ready', false)).toBe(
      'accountReady',
    );
  });

  it('ignores a change that is not a change', () => {
    expect(variantForEdge('/login', '/login', false)).toBe('forward');
  });
});
