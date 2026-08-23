import tailwindcss from '@tailwindcss/postcss';
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

  // The deployed origin, not a local one. It is load-bearing beyond canonical
  // links and the sitemap: `themeConfig.image` below resolves against
  // `url + baseUrl`, and social crawlers reject a relative og:image — so a
  // placeholder host here means no link preview anywhere, with a green build.
  url: 'https://cinedex.online',
  // Local npm development serves at `/`; the Docker build sets this to
  // `/documentation/` because Compose publishes the site behind Caddy there.
  baseUrl: process.env.DOCUSAURUS_BASE_URL ?? '/',

  // Emit directory URLs with the trailing slash already on them, so no static
  // host ever has to redirect to add one. Docusaurus writes
  // `docs/features/overview/index.html` but links to it without the slash by
  // default, and nginx answers that with a 301 to the directory form. Under
  // `nginx.conf`'s `/documentation/` location that redirect is generated from
  // the *rewritten* path, so it comes back missing the prefix and lands on
  // Caddy's catch-all — the SPA — instead of the docs. A prefix-stripping proxy
  // (Dokploy's Traefik) loses the prefix the same way. Emitting the slash up
  // front means the directory index resolves directly and no redirect happens.
  trailingSlash: true,

  organizationName: 'felipedferreira',
  projectName: 'Cinedex',

  customFields: {
    storybookBaseUrl: process.env.STORYBOOK_BASE_URL ?? 'http://localhost:9001',
  },

  onBrokenLinks: 'throw',

  // Every diagram on this site is a ```mermaid fence — there is deliberately no
  // ASCII box art left in docs/. Docusaurus does NOT error on a fence whose
  // language it doesn't recognise; it silently renders it as a code block, so
  // dropping this flag (or the theme below) turns every diagram back into raw
  // `flowchart TD ...` text with a green build. Both must stay.
  markdown: {
    mermaid: true,
  },

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
          // Order matters. `custom.css` points Infima's variables at
          // @cinedex/theme's tokens; `tailwind.css` brings the tokens themselves
          // and the utility classes @cinedex/atoms and @cinedex/solution are
          // built from. Utilities go last so that, unlayered, they win ties
          // against Infima's own classes on source order — see the comment in
          // `tailwind.css` for why they are unlayered in the first place.
          customCss: ['./src/css/custom.css', './src/css/tailwind.css'],
        },
      } satisfies Preset.Options,
    ],
  ],

  plugins: [
    // Tailwind v4 as a PostCSS pass. The SPA and Storybook use
    // `@tailwindcss/vite`; Docusaurus is webpack, and `configurePostCss` is the
    // supported seam for adding to its pipeline. Without this, `tailwind.css`
    // ships to the browser as literal `@import` and `@source` lines and every
    // library component renders with correct markup and no styling — the same
    // silent failure `frontend/packages/theme/CLAUDE.md` documents.
    function cinedexTailwindPlugin() {
      return {
        name: 'cinedex-tailwind',
        configurePostCss(postCssOptions: { plugins: unknown[] }) {
          postCssOptions.plugins.push(tailwindcss());
          return postCssOptions;
        },
      };
    },
  ],

  themes: [
    '@docusaurus/theme-mermaid',
    [
      // Offline search index, built at `docusaurus build` time — no Algolia
      // account, no application, no network call from the rendered page.
      '@easyops-cn/docusaurus-search-local',
      {
        // Content-hash the index filename so a rebuilt site never serves a
        // stale index out of a browser or CDN cache.
        hashed: true,
        indexDocs: true,
        indexPages: true,
        // There is no blog (`blog: false` in the preset above).
        indexBlog: false,
        docsRouteBasePath: '/docs',
      },
    ],
  ],

  themeConfig: {
    // The social card — what a messaging app or a social post renders when this
    // site's link is shared. Docusaurus turns this into og:image and
    // twitter:image, resolved to an absolute URL from `url` + `baseUrl` above,
    // and theme-classic already emits `twitter:card: summary_large_image`.
    // Same 1200x630 shape as the SPA's `og-cinedex-light.png`.
    image: 'img/og-cinedex-docs.png',
    // The tags Docusaurus does NOT derive on its own. og:title, og:description
    // and og:url come per page (from `<Layout title description>` on the
    // homepage, front matter elsewhere); these are the site-wide constants and
    // the image's own metadata, which several crawlers use to lay the card out
    // before the image has finished downloading.
    metadata: [
      { property: 'og:type', content: 'website' },
      { property: 'og:site_name', content: 'Cinedex' },
      { property: 'og:image:type', content: 'image/png' },
      { property: 'og:image:width', content: '1200' },
      { property: 'og:image:height', content: '630' },
      { property: 'og:image:alt', content: 'Cinedex documentation' },
      { name: 'twitter:image:alt', content: 'Cinedex documentation' },
    ],
    colorMode: {
      respectPrefersColorScheme: true,
    },
    // Diagrams have to follow the color mode, not just the page around them —
    // Mermaid's own default palette is built for a light background and turns
    // near-illegible on the dark theme `respectPrefersColorScheme` can select.
    mermaid: {
      theme: { light: 'neutral', dark: 'dark' },
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
