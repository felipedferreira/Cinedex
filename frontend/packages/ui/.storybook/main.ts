import type { StorybookConfig } from '@storybook/react-vite';

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.tsx'],
  addons: ['@storybook/addon-docs', '@storybook/addon-a11y'],
  framework: {
    name: '@storybook/react-vite',
    options: {},
  },
  // The app references its icon sprite as `/icons.svg#id`, so serve the app's
  // public/ directory to keep those absolute paths resolving inside Storybook.
  staticDirs: ['../../../apps/cinadex-ui/public'],
};

export default config;
