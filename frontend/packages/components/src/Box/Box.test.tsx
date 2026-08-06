import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Box } from './Box';

describe('Box', () => {
  it('renders its children in a div by default', () => {
    render(<Box>content</Box>);

    const box = screen.getByText('content');
    expect(box.tagName).toBe('DIV');
  });

  it('renders the element given by `as`', () => {
    render(<Box as="section">content</Box>);

    expect(screen.getByText('content').tagName).toBe('SECTION');
  });

  it('forwards unknown props to the underlying element', () => {
    render(<Box aria-label="wrapper">content</Box>);

    expect(screen.getByLabelText('wrapper')).toBeInTheDocument();
  });

  it('keeps a caller-supplied className alongside its own', () => {
    render(
      <Box className="custom" aria-label="wrapper">
        content
      </Box>,
    );

    expect(screen.getByLabelText('wrapper')).toHaveClass('custom');
  });

  // CSS Modules are hashed as `_<name>_<hash>`, so the readable prefix is what
  // these assertions match on.
  it('defaults to a column', () => {
    render(<Box aria-label="wrapper">content</Box>);

    expect(screen.getByLabelText('wrapper').className).toContain('column');
  });

  it('switches to a row on request', () => {
    render(
      <Box direction="row" aria-label="wrapper">
        content
      </Box>,
    );

    expect(screen.getByLabelText('wrapper').className).toContain('row');
  });

  it('maps padding and gap onto the spacing scale', () => {
    render(
      <Box padding={3} gap={5} aria-label="wrapper">
        content
      </Box>,
    );

    const { className } = screen.getByLabelText('wrapper');
    expect(className).toContain('p3');
    expect(className).toContain('g5');
  });

  it('applies alignment and justification', () => {
    render(
      <Box align="center" justify="between" aria-label="wrapper">
        content
      </Box>,
    );

    const { className } = screen.getByLabelText('wrapper');
    expect(className).toContain('alignCenter');
    expect(className).toContain('justifyBetween');
  });

  it('omits alignment classes when those props are not given', () => {
    render(<Box aria-label="wrapper">content</Box>);

    const { className } = screen.getByLabelText('wrapper');
    expect(className).not.toContain('align');
    expect(className).not.toContain('justify');
  });
});
