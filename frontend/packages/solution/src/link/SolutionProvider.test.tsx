import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SignInScreen } from '../screens/SignInScreen';
import { SolutionProvider } from './SolutionProvider';
import type { FC } from 'react';
import type { SolutionLinkProps } from './linkTypes';

describe('SolutionProvider', () => {
  it('falls back to plain anchors when no link component is injected', () => {
    render(<SignInScreen />);

    expect(screen.getByRole('link', { name: 'Create one' })).toHaveAttribute(
      'href',
      '/register',
    );
  });

  it('routes screen navigation through the injected link component', async () => {
    const navigate = vi.fn();
    const user = userEvent.setup();

    const RouterLink: FC<SolutionLinkProps> = ({ to, children, className }) => {
      return (
        <button
          type="button"
          className={className}
          onClick={() => {
            navigate(to);
          }}
        >
          {children}
        </button>
      );
    };

    render(
      <SolutionProvider linkComponent={RouterLink}>
        <SignInScreen />
      </SolutionProvider>,
    );

    // The screen still owns the path — only the navigation mechanism is injected.
    await user.click(screen.getByRole('button', { name: 'Create one' }));

    expect(navigate).toHaveBeenCalledExactlyOnceWith('/register');
    expect(screen.queryByRole('link', { name: 'Create one' })).toBeNull();
  });
});
