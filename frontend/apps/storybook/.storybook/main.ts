import type { StorybookConfig } from '@storybook/react-vite';

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.tsx'],
  addons: ['@storybook/addon-docs', '@storybook/addon-a11y'],
  framework: {
    name: '@storybook/react-vite',
    options: {},
  },
  // The SPA references its icon sprite as `/icons.svg#id`, and components documented here may do
  // the same, so serve that app's public/ directory to keep those absolute paths resolving.
  // `./public` is this app's own — just the brand lockup manager.ts points `brandImage` at.
  staticDirs: ['../../cinedex-app/public', './public'],
};

export default config;
