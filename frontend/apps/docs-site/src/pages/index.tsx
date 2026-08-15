import { Fragment, type ReactNode } from 'react';
import BrowserOnly from '@docusaurus/BrowserOnly';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import { useColorMode } from '@docusaurus/theme-common';
import Layout from '@theme/Layout';
import { Button, Separator } from '@cinedex/atoms';
import { Brand, BrandApertureAnimation } from '@cinedex/solution';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

/**
 * The Cinedex mark, playing its lens-aperture intro.
 *
 * `BrandApertureAnimation` cannot server-render: `useDelayedReveal` reads
 * `window.matchMedia` in a `useState` initializer, which runs during render, and
 * Docusaurus prerenders every page at build time. `<BrowserOnly>` is the
 * supported seam for that, and it keeps the fix on this site rather than in
 * `@cinedex/solution`, whose other two consumers never server-render.
 *
 * The fallback is the static `Brand` — the same mark at rest, and SSR-clean —
 * so the prerendered HTML carries the logo, a viewer with JavaScript off still
 * sees it, and nothing reflows on hydration. The cost is that the sequence
 * replays once when hydration swaps them, which reads as an intro rather than
 * as a glitch.
 */
function BrandLockup() {
  return (
    <span className="flex items-center gap-2">
      <BrowserOnly fallback={<Brand size={10} />}>
        {() => <BrandApertureAnimation size={10} />}
      </BrowserOnly>
    </span>
  );
}

function HomepageHeader() {
  const { siteConfig } = useDocusaurusContext();

  return (
    <header className="border-b border-border bg-bg px-6 py-16">
      <div className="mx-auto flex max-w-[720px] flex-col items-center gap-5 text-center">
        <BrandLockup />

        <div className="flex flex-col gap-2.5">
          <p className="m-0 text-body text-text">{siteConfig.tagline}</p>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-2.5">
          {/* `asChild` renders Docusaurus's own Link with the button's styling —
              the alternative would nest an <a> inside a <button>. */}
          <Button asChild variant="solid">
            <Link to="/docs/features/overview">Explore the docs</Link>
          </Button>
          <Button asChild variant="outline">
            <Link to="/changelog">View the changelog</Link>
          </Button>
        </div>
      </div>
    </header>
  );
}

const STACK = [
  '.NET 10',
  'React 19',
  'PostgreSQL 17',
  'Docker',
  'OpenTelemetry',
] as const;

function StackRow() {
  return (
    <section
      aria-labelledby="stack-label"
      className="mx-auto max-w-[900px] px-6 pb-16"
    >
      <Separator className="mb-6" />
      <p
        id="stack-label"
        className="m-0 mb-2.5 text-center font-mono text-label font-medium tracking-label text-text uppercase"
      >
        Built with
      </p>
      <ul className="m-0 flex list-none flex-wrap items-center justify-center gap-x-6 gap-y-2 p-0">
        {STACK.map((item) => (
          <li
            key={item}
            className="font-mono text-caption whitespace-nowrap text-text"
          >
            {item}
          </li>
        ))}
      </ul>
    </section>
  );
}

/**
 * The homepage content, remounted whenever the colour mode changes.
 *
 * **Do not drop the `key`.** The design system's tokens resolve through
 * `light-dark()`, which follows the used `color-scheme`; Chrome does not
 * re-resolve such a value on an existing element when `color-scheme` changes at
 * runtime **if that property is being transitioned**. `Button` carries
 * `transition-colors`, so on a theme toggle both call-to-action buttons kept the
 * previous theme's fill and border — a near-white button on a white page — while
 * `Card`, which has no transition, repainted correctly. Remounting sidesteps it,
 * the same fix and the same reason as `@cinedex/storybook`'s preview decorator.
 *
 * This costs a replay of the brand's intro sequence on every toggle. That is
 * acceptable: it is 1.2s, it honours `prefers-reduced-motion`, and it settles on
 * the identical static state.
 *
 * The SPA never hit this because it has no runtime theme switch — a page that
 * merely loads under a theme resolves everything correctly. Only Storybook's
 * toolbar and this site's navbar toggle are affected.
 */
function HomeContent(): ReactNode {
  const { colorMode } = useColorMode();

  return (
    <Fragment key={colorMode}>
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <StackRow />
      </main>
    </Fragment>
  );
}

export default function Home(): ReactNode {
  const { siteConfig } = useDocusaurusContext();

  // `useColorMode` reads Layout's ColorModeProvider, so it has to be called by a
  // child of <Layout>, not here.
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <HomeContent />
    </Layout>
  );
}
