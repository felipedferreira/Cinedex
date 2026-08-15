import type { ReactNode } from 'react';
import Link from '@docusaurus/Link';
import Heading from '@theme/Heading';
import { Card } from '@cinedex/atoms';

interface FeatureItem {
  kicker: string;
  title: string;
  description: ReactNode;
}

const FeatureList: FeatureItem[] = [
  {
    kicker: 'Catalog · Stack',
    title: 'Features',
    description: (
      <>
        A hexagonal .NET backend, a React 19 SPA, and a shared component library
        — the <Link to="/docs/features/overview">Features</Link> section covers
        the movie catalog, the architecture, the frontend, and how the whole
        stack runs locally.
      </>
    ),
  },
  {
    kicker: 'Session · Auth',
    title: 'Security',
    description: (
      <>
        JWT access tokens, rotating refresh-token families, and a members-only
        catalog — the <Link to="/docs/security/overview">Security</Link> section
        explains the auth model in depth, including the gaps that are still
        open.
      </>
    ),
  },
  {
    kicker: 'Repo · Generated',
    title: 'Always up to date',
    description: (
      <>
        The <Link to="/changelog">Changelog</Link> page is generated straight
        from the repository's root <code>CHANGELOG.md</code> — every release
        shows up here automatically.
      </>
    ),
  },
];

/**
 * `Card` is the design system's raised surface and owns no padding of its own,
 * so the inset here is this component's to supply — same as every other caller.
 */
function Feature({ kicker, title, description }: FeatureItem) {
  return (
    <Card className="cinedex-home-card flex flex-col gap-2 p-5">
      <p className="m-0 font-mono text-label font-semibold tracking-eyebrow text-accent uppercase">
        {kicker}
      </p>
      <Heading
        as="h3"
        className="m-0 text-body font-bold tracking-tight text-text-h"
      >
        {title}
      </Heading>
      <p className="m-0 text-body leading-[1.5] text-text">{description}</p>
    </Card>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className="cinedex-home-features mx-auto max-w-[900px] px-6 py-12">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {FeatureList.map((props) => (
          <Feature key={props.title} {...props} />
        ))}
      </div>
    </section>
  );
}
