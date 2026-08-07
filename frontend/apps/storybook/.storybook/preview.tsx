import type { Preview } from '@storybook/react-vite';
// Imported through the package export rather than by relative path — this app is just another
// consumer of @cinedex/theme, so loading the design system exactly the way the SPA's `main.tsx`
// does also proves that export entry resolves. One import: `tailwind.css` pulls in the tokens and
// the base element styling itself, in the cascade-layer order they have to be in.
import '@cinedex/theme/tailwind.css';
import { SolutionProvider } from '@cinedex/solution';

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
      // @cinedex/theme's tokens resolve through `light-dark()`, so forcing the
      // used `color-scheme` is all it takes to repaint every component.
      const { theme } = context.globals;
      const scheme = typeof theme === 'string' ? theme : 'light dark';
      document.documentElement.style.colorScheme = scheme;

      // No `linkComponent`, so @cinedex/solution's screens fall back to plain
      // anchors. That is the point of injecting navigation rather than importing
      // it: a full screen renders here with no router and no mock.
      //
      // Keyed so switching the toolbar remounts the story. Chrome does not
      // re-resolve `light-dark()` for every property on an existing element
      // when `color-scheme` changes at runtime — a form control's
      // `border-color`, for instance, keeps the previous theme's value until
      // the element is recreated. A page that simply loads under a theme is
      // unaffected; this only makes the toolbar honest.
      return (
        <SolutionProvider key={scheme}>
          <Story />
        </SolutionProvider>
      );
    },
  ],
};

export default preview;
