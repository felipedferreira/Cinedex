import type { Preview } from '@storybook/react-vite';
// Imported through the package exports rather than by relative path — this app is just another
// consumer of @cinedex/components, so loading its styles the way the SPA does also proves those two export
// entries resolve.
import '@cinedex/components/tokens.css';
import '@cinedex/components/base.css';

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    a11y: {
      // Report violations as errors in the accessibility panel rather than
      // as passive warnings. Note this does not fail `build-storybook` —
      // that needs the Vitest addon, which this package does not install.
      test: 'error',
    },
  },
  globalTypes: {
    theme: {
      description: 'Colour scheme applied to the preview',
      toolbar: {
        title: 'Theme',
        icon: 'mirror',
        items: [
          { value: 'light dark', title: 'System' },
          { value: 'light', title: 'Light' },
          { value: 'dark', title: 'Dark' },
        ],
        dynamicTitle: true,
      },
    },
  },
  initialGlobals: {
    theme: 'light dark',
  },
  decorators: [
    (Story, context) => {
      // @cinedex/components's tokens resolve through `light-dark()`, so forcing the
      // used `color-scheme` is all it takes to repaint every component.
      const { theme } = context.globals;
      const scheme = typeof theme === 'string' ? theme : 'light dark';
      document.documentElement.style.colorScheme = scheme;

      // Keyed so switching the toolbar remounts the story. Chrome does not
      // re-resolve `light-dark()` for every property on an existing element
      // when `color-scheme` changes at runtime — a form control's
      // `border-color`, for instance, keeps the previous theme's value until
      // the element is recreated. A page that simply loads under a theme is
      // unaffected; this only makes the toolbar honest.
      return <Story key={scheme} />;
    },
  ],
};

export default preview;
