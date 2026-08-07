// Guards this site's Mermaid *rendering wiring*, which is the half of the
// problem that is specific to Docusaurus.
//
// Docusaurus does NOT error on a fenced code block whose language it doesn't
// recognise — it renders it as a plain code block. So if `markdown.mermaid` or
// the `@docusaurus/theme-mermaid` entry is ever dropped from
// docusaurus.config.ts, every ```mermaid fence degrades to raw
// `flowchart TD ...` text on the page while the build stays green and CI stays
// happy. That exact failure shipped once (PR #55), which is why this runs as
// part of `npm run build` rather than living in a doc as a reminder.
//
// The *content* rules — no ASCII box art, no semicolons inside a
// sequenceDiagram — are repo-wide and live in `scripts/check-diagrams.mjs` at
// the repository root, which CI runs separately. They are not duplicated here,
// so a violation is reported once, by one owner.
import { readFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const SITE_ROOT = resolve(__dirname, '..');
const CONFIG = join(SITE_ROOT, 'docusaurus.config.ts');

const errors = [];
const config = readFileSync(CONFIG, 'utf-8');
const where = relative(SITE_ROOT, CONFIG);

if (!/mermaid:\s*true/.test(config)) {
  errors.push(
    `${where}: \`markdown: { mermaid: true }\` is missing. ` +
      'Every ```mermaid fence will render as a plain code block.',
  );
}

if (!config.includes('@docusaurus/theme-mermaid')) {
  errors.push(
    `${where}: '@docusaurus/theme-mermaid' is not registered in \`themes\`. ` +
      'Every ```mermaid fence will render as a plain code block.',
  );
}

if (errors.length > 0) {
  console.error('[check-diagrams] failed:\n');
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  process.exit(1);
}

console.log('[check-diagrams] ok - mermaid rendering is wired up.');
