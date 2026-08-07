import type { ReactNode } from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function HomepageHeader() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/features/overview"
          >
            Explore the docs
          </Link>
          <Link className="button button--secondary button--lg" to="/changelog">
            View the changelog
          </Link>
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
    <section aria-labelledby="stack-label">
      <p id="stack-label" className={styles.stackLabel}>
        Built with
      </p>
      <ul className={styles.stack}>
        {STACK.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </section>
  );
}

export default function Home(): ReactNode {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <StackRow />
      </main>
    </Layout>
  );
}
