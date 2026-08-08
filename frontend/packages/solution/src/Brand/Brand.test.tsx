import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Brand } from './Brand';

describe('Brand', () => {
  it('renders the mark as a labelled image', () => {
    render(<Brand />);

    expect(screen.getByRole('img', { name: 'Cinedex' })).toBeInTheDocument();
  });

  it('renders the wordmark as text', () => {
    render(<Brand />);

    expect(screen.getByText('Cinedex')).toBeInTheDocument();
  });
});
