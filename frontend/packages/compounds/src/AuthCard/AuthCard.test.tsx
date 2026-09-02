import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuthCard } from './AuthCard';

describe('AuthCard', () => {
  it('renders the title as the page heading', () => {
    render(
      <AuthCard
        eyebrow="Step 1 of 2"
        kicker="Session · Sign in"
        title="Sign in"
      >
        <p>body</p>
      </AuthCard>,
    );

    expect(
      screen.getByRole('heading', { level: 1, name: 'Sign in' }),
    ).toBeInTheDocument();
  });

  it('renders the injected brand rather than one of its own', () => {
    render(
      <AuthCard
        brand={<span>Acme</span>}
        eyebrow="ACME · Auth"
        kicker="Session"
        title="Sign in"
      >
        <p>body</p>
      </AuthCard>,
    );

    expect(screen.getByText('Acme')).toBeInTheDocument();
    expect(screen.queryByText('Cinedex')).not.toBeInTheDocument();
  });

  it('omits the description and footnote when they are not given', () => {
    const { container } = render(
      <AuthCard eyebrow="e" kicker="k" title="Sign in">
        <p>body</p>
      </AuthCard>,
    );

    expect(container.querySelectorAll('p')).toHaveLength(2); // kicker + body
  });

  // Both header labels are optional, and every Cinedex auth screen now omits at
  // least one of them. Asserting the *elements* are gone rather than the text:
  // rendering an empty <span>/<p> would still satisfy a text query, while
  // leaving the brand row a pixel taller than a screen without an eyebrow.
  it('omits the eyebrow and kicker elements when they are not given', () => {
    const { container } = render(
      <AuthCard title="Sign in">
        <p>body</p>
      </AuthCard>,
    );

    expect(container.querySelector('span')).toBeNull();
    expect(container.querySelectorAll('p')).toHaveLength(1); // body only
    expect(
      screen.getByRole('heading', { level: 1, name: 'Sign in' }),
    ).toBeInTheDocument();
  });
  it('renders the description and footnote when they are', () => {
    render(
      <AuthCard
        eyebrow="e"
        kicker="k"
        title="Sign in"
        description="Sessions last 30 days."
        footnote="Rate-limited: 5 attempts per 15 min."
      >
        <p>body</p>
      </AuthCard>,
    );

    expect(screen.getByText('Sessions last 30 days.')).toBeInTheDocument();
    expect(
      screen.getByText('Rate-limited: 5 attempts per 15 min.'),
    ).toBeInTheDocument();
  });
});
