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

  it('keeps a caller-supplied className alongside its own', () => {
    render(<Button className="custom">Press</Button>);

    expect(screen.getByRole('button')).toHaveClass('custom');
  });

  it('lets a caller-supplied utility win over the variant it conflicts with', () => {
    // What `cn()` buys over plain concatenation: both classes are in the
    // `rounded` group, so the caller's has to be the one that survives.
    render(<Button className="rounded-lg">Press</Button>);

    const { className } = screen.getByRole('button');
    expect(className).toContain('rounded-lg');
    expect(className).not.toContain('rounded-md');
  });

  it.each([
    ['primary', 'solid'],
    ['solid', 'outline'],
  ] as const)('renders %s and %s differently', (first, second) => {
    const { rerender } = render(<Button variant={first}>Go</Button>);
    const firstClass = screen.getByRole('button').className;

    rerender(<Button variant={second}>Go</Button>);

    expect(screen.getByRole('button').className).not.toBe(firstClass);
  });

  it('renders each size differently', () => {
    const { rerender } = render(<Button size="md">Go</Button>);
    const mdClass = screen.getByRole('button').className;

    rerender(<Button size="block">Go</Button>);

    expect(screen.getByRole('button').className).not.toBe(mdClass);
  });

  it('renders the child element instead of a button when asChild is set', () => {
    render(
      <Button asChild variant="outline" size="block">
        <a href="/forgot-password">Reset password instead</a>
      </Button>,
    );

    const link = screen.getByRole('link', { name: 'Reset password instead' });
    expect(link).toHaveAttribute('href', '/forgot-password');
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    // The styling still comes from the variant, which is the whole point.
    expect(link.className).toContain('w-full');
  });
});
