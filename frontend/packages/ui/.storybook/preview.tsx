import type { Preview } from '@storybook/react-vite';
import '../src/styles/tokens.css';
import '../src/styles/base.css';

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
      // Tokens resolve through `light-dark()`, so forcing the used
      // `color-scheme` is all it takes to repaint every component.
      const { theme } = context.globals;
      document.documentElement.style.colorScheme =
        typeof theme === 'string' ? theme : 'light dark';

      return <Story />;
    },
  ],
};

export default preview;
