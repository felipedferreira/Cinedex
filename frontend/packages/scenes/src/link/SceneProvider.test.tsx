import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SignInScreen } from '../screens/SignInScreen';
import { SceneProvider } from './SceneProvider';
import type { FC } from 'react';
import type { SceneLinkProps } from './linkTypes';

describe('SceneProvider', () => {
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

    const RouterLink: FC<SceneLinkProps> = ({ to, children, className }) => {
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
      <SceneProvider linkComponent={RouterLink}>
        <SignInScreen />
      </SceneProvider>,
    );

    // The screen still owns the path — only the navigation mechanism is injected.
    await user.click(screen.getByRole('button', { name: 'Create one' }));

    expect(navigate).toHaveBeenCalledExactlyOnceWith('/register');
    expect(screen.queryByRole('link', { name: 'Create one' })).toBeNull();
  });
});
