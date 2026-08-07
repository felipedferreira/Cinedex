import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Alert } from './Alert';

describe('Alert', () => {
  it('renders its children inside a status region', () => {
    render(<Alert>Locked at 14:02 UTC</Alert>);

    expect(screen.getByRole('status')).toHaveTextContent('Locked at 14:02 UTC');
  });

  it.each(['neutral', 'warning', 'success'] as const)(
    'renders the %s tone',
    (tone) => {
      render(<Alert tone={tone}>Message</Alert>);

      expect(screen.getByRole('status')).toBeVisible();
    },
  );
});
