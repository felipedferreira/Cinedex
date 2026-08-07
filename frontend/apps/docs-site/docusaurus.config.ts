import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'Cinedex',
  tagline: 'Docs and changelog for the Cinedex movie catalog',
  favicon: 'img/favicon.svg',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // No deployed site exists yet - this app is local-dev only for now.
  // Update both values together if/when it's actually deployed.
  url: 'https://cinedex.example.com',
  baseUrl: '/',

  organizationName: 'felipedferreira',
  projectName: 'Cinedex',

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/felipedferreira/Cinedex/tree/main/frontend/apps/docs-site/',
        },
        // No blog content or editorial workflow exists for this project yet.
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // No bespoke social card image exists for this project yet.
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Cinedex',
      logo: {
        alt: 'Cinedex',
        src: 'img/favicon.svg',
      },
      items: [
        {
          type: 'doc',
          docId: 'features/overview',
          position: 'left',
          label: 'Features',
        },
        {
          type: 'doc',
          docId: 'security/overview',
          position: 'left',
          label: 'Security',
        },
        { to: '/changelog', label: 'Changelog', position: 'left' },
        {
          href: 'https://github.com/felipedferreira/Cinedex',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Features',
              to: '/docs/features/overview',
            },
            {
              label: 'Security',
              to: '/docs/security/overview',
            },
            {
              label: 'Changelog',
              to: '/changelog',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/felipedferreira/Cinedex',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear().toString()} Cinedex. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
