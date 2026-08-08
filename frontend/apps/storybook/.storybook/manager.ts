import { addons } from 'storybook/manager-api';
import { create } from 'storybook/theming/create';

// `staticDirs` in main.ts serves ./public at the site root, so this resolves
// to the same file this app ships as brand.svg — the static lockup, not the
// animated `Brand`/`BrandApertureAnimation` components: the manager UI is a
// separate bundle from the preview iframe those render into, and Storybook's
// theming only accepts a static image for `brandImage`, never a component.
//
// `base: 'dark'` is required, not cosmetic: the lockup's wordmark is drawn in
// a light colour for a dark ground (see brand.svg), so on Storybook's default
// light theme it would sit near-invisible against the light sidebar bar.
addons.setConfig({
  theme: create({
    base: 'dark',
    brandTitle: 'Cinedex',
    brandUrl: 'https://github.com/felipedferreira/Cinedex',
    brandImage: '/brand.svg',
    brandTarget: '_self',
  }),
});
