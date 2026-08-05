import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from './Button';

describe('Button', () => {
  it('renders with its children as the accessible name', () => {
    render(<Button>Count is 0</Button>);

    expect(screen.getByRole('button', { name: 'Count is 0' })).toBeVisible();
  });

  it('defaults to type="button" so it never submits a form by accident', () => {
    render(<Button>Save</Button>);

    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');
  });

  it('accepts an explicit type', () => {
    render(<Button type="submit">Save</Button>);

    expect(screen.getByRole('button')).toHaveAttribute('type', 'submit');
  });

  it('calls onClick when pressed', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(<Button onClick={onClick}>Press</Button>);

    await user.click(screen.getByRole('button'));

    expect(onClick).toHaveBeenCalledOnce();
  });

  // CSS Modules are hashed as `_<name>_<hash>`, so the readable prefix is what
  // these assertions match on.
  it('defaults to the primary variant at medium size', () => {
    render(<Button>Press</Button>);

    const { className } = screen.getByRole('button');
    expect(className).toContain('primary');
    expect(className).toContain('md');
  });

  it('renders the ghost variant at small size on request', () => {
    render(
      <Button variant="ghost" size="sm">
        Press
      </Button>,
    );

    const { className } = screen.getByRole('button');
    expect(className).toContain('ghost');
    expect(className).toContain('sm');
  });

  it('keeps a caller-supplied className alongside its own', () => {
    render(<Button className="custom">Press</Button>);

    expect(screen.getByRole('button')).toHaveClass('custom');
  });

  it('does not call onClick when disabled', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(
      <Button disabled onClick={onClick}>
        Press
      </Button>,
    );

    await user.click(screen.getByRole('button'));

    expect(onClick).not.toHaveBeenCalled();
  });
});
