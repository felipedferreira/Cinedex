// Guards the two ways this site's diagrams can silently stop being diagrams.
//
// Docusaurus does NOT error on a fenced code block whose language it doesn't
// recognise — it renders it as a plain code block. So if `markdown.mermaid` or
// the `@docusaurus/theme-mermaid` entry is ever dropped from
// docusaurus.config.ts, every ```mermaid fence degrades to raw
// `flowchart TD ...` text on the page while the build stays green and CI stays
// happy. That exact failure shipped once (PR #55), which is why this runs as
// part of `npm run build` rather than living in a doc as a reminder.
//
// It also enforces the "no ASCII box art" rule: diagrams here are Mermaid, so
// they version, diff, and theme with the site instead of drifting out of
// alignment one character at a time.
//
// This is a static check. It cannot catch a Mermaid *syntax* error — Mermaid
// renders in the browser, so a malformed diagram only fails there. Look at the
// rendered pages when you add or change one.
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, extname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const SITE_ROOT = resolve(__dirname, '..');
const CONFIG = join(SITE_ROOT, 'docusaurus.config.ts');
const CONTENT_DIRS = [join(SITE_ROOT, 'docs'), join(SITE_ROOT, 'src/pages')];

// The box-drawing range plus the arrowheads the old hand-drawn diagrams used.
const BOX_DRAWING = /[─-╿▲▶▼◀]/;

const errors = [];

function markdownFilesIn(dir) {
  let found = [];

  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);

    if (statSync(full).isDirectory()) {
      found = found.concat(markdownFilesIn(full));
    } else if (['.md', '.mdx'].includes(extname(entry))) {
      found.push(full);
    }
  }

  return found;
}

// 1. The config must still wire up both halves of Mermaid rendering.
const config = readFileSync(CONFIG, 'utf-8');

if (!/mermaid:\s*true/.test(config)) {
  errors.push(
    `${relative(SITE_ROOT, CONFIG)}: \`markdown: { mermaid: true }\` is missing. ` +
      'Every ```mermaid fence will render as a plain code block.',
  );
}

if (!config.includes('@docusaurus/theme-mermaid')) {
  errors.push(
    `${relative(SITE_ROOT, CONFIG)}: '@docusaurus/theme-mermaid' is not registered in \`themes\`. ` +
      'Every ```mermaid fence will render as a plain code block.',
  );
}

// 2. No hand-drawn ASCII diagrams, and the fences we expect are still there.
let mermaidBlocks = 0;

for (const dir of CONTENT_DIRS) {
  for (const file of markdownFilesIn(dir)) {
    // src/pages/changelog.md is generated from the root CHANGELOG.md by
    // sync-changelog.mjs; its content is not ours to police here.
    if (file.endsWith(join('src', 'pages', 'changelog.md'))) continue;

    const contents = readFileSync(file, 'utf-8');
    const where = relative(SITE_ROOT, file);

    mermaidBlocks += (contents.match(/^```mermaid\s*$/gm) ?? []).length;

    let inMermaid = false;
    let inSequence = false;

    contents.split('\n').forEach((line, index) => {
      const at = `${where}:${(index + 1).toString()}`;

      if (/^```mermaid\s*$/.test(line)) {
        inMermaid = true;
        inSequence = false;
        return;
      }

      if (inMermaid && /^```\s*$/.test(line)) {
        inMermaid = false;
        inSequence = false;
        return;
      }

      if (inMermaid && /^\s*sequenceDiagram\b/.test(line)) inSequence = true;

      if (BOX_DRAWING.test(line)) {
        errors.push(
          `${at}: box-drawing character in an ASCII diagram. ` +
            'Diagrams on this site are Mermaid — see the note in CLAUDE.md.',
        );
      }

      // In Mermaid's sequence grammar `;` is a statement separator, so a
      // semicolon inside message or Note text truncates the statement and the
      // remainder parses as garbage. The diagram then fails to render *in the
      // browser only* — `docusaurus build` stays green. Entities like `&amp;`
      // are fine.
      if (inSequence && /;/.test(line.replace(/&[a-z]+;/gi, ''))) {
        errors.push(
          `${at}: semicolon inside a sequenceDiagram. Mermaid treats it as a ` +
            'statement separator, so the diagram will fail to render in the browser ' +
            'while the build stays green. Use a comma or dash instead.',
        );
      }
    });
  }
}

if (mermaidBlocks === 0) {
  errors.push(
    'No ```mermaid fences found at all. If that is genuinely intended, remove this check ' +
      'rather than leaving it passing vacuously.',
  );
}

if (errors.length > 0) {
  console.error('[check-diagrams] failed:\n');
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  process.exit(1);
}

console.log(
  `[check-diagrams] ok - ${mermaidBlocks.toString()} mermaid diagrams, no ASCII box art.`,
);
