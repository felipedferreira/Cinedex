import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TextField } from './TextField';

describe('TextField', () => {
  it('associates the label with the input', () => {
    render(<TextField label="Email" />);

    expect(screen.getByLabelText('Email')).toBeInTheDocument();
  });

  it('generates a unique id per instance', () => {
    render(
      <>
        <TextField label="First" />
        <TextField label="Last" />
      </>,
    );

    expect(screen.getByLabelText('First').id).not.toBe(
      screen.getByLabelText('Last').id,
    );
  });

  it('accepts typed input', async () => {
    const user = userEvent.setup();
    render(<TextField label="Email" />);

    await user.type(screen.getByLabelText('Email'), 'ada@example.com');

    expect(screen.getByLabelText('Email')).toHaveValue('ada@example.com');
  });

  it('is not marked invalid when there is no error', () => {
    render(<TextField label="Email" />);

    expect(screen.getByLabelText('Email')).not.toHaveAttribute('aria-invalid');
    expect(screen.getByLabelText('Email')).not.toHaveAccessibleDescription();
  });

  it('announces the error through aria-describedby', () => {
    render(<TextField label="Email" error="Enter a valid address" />);

    const input = screen.getByLabelText('Email');
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription('Enter a valid address');
  });

  it('forwards unknown props to the input', () => {
    render(<TextField label="Email" type="email" placeholder="you@here" />);

    const input = screen.getByLabelText('Email');
    expect(input).toHaveAttribute('type', 'email');
    expect(input).toHaveAttribute('placeholder', 'you@here');
  });

  it('keeps a caller-supplied className alongside its own', () => {
    render(<TextField label="Email" className="custom" />);

    expect(screen.getByLabelText('Email')).toHaveClass('custom');
  });

  it('styles the input as invalid only when there is an error', () => {
    const { rerender } = render(<TextField label="Email" />);
    // `toContain` would also pass on `border-border`, which is the hairline the
    // resting control deliberately does NOT use — assert the exact class.
    expect(screen.getByLabelText('Email').className).toContain(
      'border-border-strong',
    );

    rerender(<TextField label="Email" error="Required" />);

    const input = screen.getByLabelText('Email');
    expect(input.className).toContain('border-danger-border');
    expect(input.className).not.toContain('border-border-strong');
  });

  it('renders labelExtra in the label row', () => {
    render(
      <TextField label="Password" labelExtra={<a href="/r">Forgot?</a>} />,
    );

    expect(screen.getByRole('link', { name: 'Forgot?' })).toBeInTheDocument();
  });
});
