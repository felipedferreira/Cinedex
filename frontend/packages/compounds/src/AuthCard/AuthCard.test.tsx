import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuthCard } from './AuthCard';

describe('AuthCard', () => {
  it('renders the title as the page heading', () => {
    render(
      <AuthCard eyebrow="CIN · Auth" kicker="Session · Sign in" title="Sign in">
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

  it('omits the description when it is not given', () => {
    const { container } = render(
      <AuthCard eyebrow="e" kicker="k" title="Sign in">
        <p>body</p>
      </AuthCard>,
    );

    expect(container.querySelectorAll('p')).toHaveLength(2); // kicker + body
  });

  it('renders the description when it is given', () => {
    render(
      <AuthCard
        eyebrow="e"
        kicker="k"
        title="Sign in"
        description="Sessions last 30 days."
      >
        <p>body</p>
      </AuthCard>,
    );

    expect(screen.getByText('Sessions last 30 days.')).toBeInTheDocument();
  });
});
