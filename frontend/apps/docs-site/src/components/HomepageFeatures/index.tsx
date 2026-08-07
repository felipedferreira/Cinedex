import type { ReactNode } from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

interface FeatureItem {
  title: string;
  description: ReactNode;
}

const FeatureList: FeatureItem[] = [
  {
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
    title: 'Always Up to Date',
    description: (
      <>
        The <Link to="/changelog">Changelog</Link> page is generated straight
        from the repository's root <code>CHANGELOG.md</code> — every release
        shows up here automatically.
      </>
    ),
  },
];

function Feature({ title, description }: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
