// Enforces the repo-wide rule that diagrams in rendered markdown are Mermaid
// fences, never hand-drawn ASCII box art.
//
// Why this needs enforcing at all: neither GitHub nor Docusaurus errors on a
// fenced block whose language it doesn't recognise — both render it as a plain
// code block. So a diagram can stop being a diagram with a completely green
// build and a passing CI. That exact failure shipped once (PR #55), where a
// ```mermaid fence sat in the docs site for a release without the renderer ever
// being wired up.
//
// ASCII box art has the other failure mode: it drifts out of alignment one edit
// at a time, and nothing catches it. The backend README's architecture diagram
// had three boxes whose content was one character wider than their own borders.
// Mermaid computes the layout, so that class of bug can't recur, and the source
// diffs meaningfully in review instead of appearing as a wall of `─│┌` churn.
//
// Scope note: this owns the *content* rules for every tracked markdown file.
// `frontend/apps/docs-site/scripts/check-diagrams.mjs` is a separate, narrower
// check that only asserts that site's Mermaid *rendering* is still wired up in
// its docusaurus.config.ts.
//
// This is a static check and cannot catch a Mermaid *syntax* error — Mermaid
// renders in the browser, so a malformed diagram fails only there. Look at the
// rendered page (or the file on GitHub) when you add or change one.
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { basename } from 'node:path';

// U+2500–U+257F is the Box Drawing block; the four arrowheads are what the old
// hand-drawn diagrams used for flow direction. Deliberately NOT included: the
// em dash (—) and arrow (→), which are ordinary prose punctuation here.
const BOX_DRAWING = /[─-╿▲▶▼◀]/;

// CLAUDE.md files are agent context: they are ingested as raw text and never
// rendered in a browser, so a Mermaid fence in one would be read as literal
// `flowchart LR ...` source — strictly less legible than the text tree it would
// replace. This is the one deliberate exception to the rule.
const EXEMPT_BASENAMES = new Set(['CLAUDE.md']);

const errors = [];
let mermaidBlocks = 0;
let filesScanned = 0;

// `git ls-files` rather than a directory walk: it gets node_modules, build
// output, and the git-ignored generated docs-site/src/pages/changelog.md for
// free, and only ever reports files that are actually committed.
const files = execFileSync('git', ['ls-files', '-z', '*.md', '*.mdx'], {
  encoding: 'utf-8',
})
  .split('\0')
  .filter(Boolean);

for (const file of files) {
  if (EXEMPT_BASENAMES.has(basename(file))) continue;

  filesScanned += 1;

  let fence = null; // the exact ``` / ~~~ run that opened the current fence
  let language = '';
  let inSequence = false;

  readFileSync(file, 'utf-8')
    .split('\n')
    .forEach((line, index) => {
      const at = `${file}:${(index + 1).toString()}`;
      const delimiter = /^\s*(`{3,}|~{3,})\s*(\S*)/.exec(line);

      if (delimiter) {
        const [, run, info] = delimiter;

        // CommonMark: the info string of a *backtick* fence may not contain a
        // backtick. That rule exists to disambiguate a fence from an inline
        // code span written with a longer backtick run, e.g. a line starting
        // ```` ```mermaid ```` to show a fence literally in prose — which is
        // exactly how CONTRIBUTING.md documents this rule.
        const isInlineSpan = run[0] === '`' && info.includes('`');

        if (isInlineSpan) return;

        if (fence === null) {
          fence = run;
          language = info.toLowerCase();
          inSequence = false;
          if (language === 'mermaid') mermaidBlocks += 1;
          return;
        }

        // A closing fence is the same character, at least as long, and carries
        // no info string — so a ```` inside a ``` block doesn't close it.
        if (run[0] === fence[0] && run.length >= fence.length && !info) {
          fence = null;
          language = '';
          inSequence = false;
          return;
        }
      }

      // Only look *inside* fenced blocks. Prose legitimately mentions these
      // characters — the CHANGELOG entry describing this very change quotes
      // `─│┌` inline — and a real diagram is always fenced.
      if (fence === null) return;

      if (BOX_DRAWING.test(line)) {
        errors.push(
          `${at}: box-drawing character inside a code fence. Diagrams in this repo are ` +
            'Mermaid — see the rule in CONTRIBUTING.md.',
        );
      }

      if (language === 'mermaid' && /^\s*sequenceDiagram\b/.test(line)) {
        inSequence = true;
        return;
      }

      // In Mermaid's sequence grammar `;` is a statement separator, and message
      // and Note text is *unquoted* — so a semicolon there truncates the
      // statement and the remainder parses as garbage. The diagram then fails to
      // render *in the browser only*: every static check, including this one,
      // stays green. HTML entities (&amp;) are fine.
      //
      // Scoped to sequence diagrams on purpose. Flowchart labels are quoted
      // (`A["one; two"]`), so the quotes delimit the semicolon and it is
      // harmless there — flagging it would be a false positive.
      if (inSequence && /;/.test(line.replace(/&[a-z]+;/gi, ''))) {
        errors.push(
          `${at}: semicolon inside a sequenceDiagram. Mermaid treats it as a statement ` +
            'separator, so the diagram will fail to render in the browser while every ' +
            'build stays green. Use a comma or dash instead.',
        );
      }
    });

  if (fence !== null) {
    errors.push(`${file}: unterminated code fence — the file ends inside a block.`);
  }
}

if (mermaidBlocks === 0) {
  errors.push(
    'No ```mermaid fences found anywhere in the repo. If that is genuinely intended, ' +
      'delete this check rather than leaving it passing vacuously.',
  );
}

if (errors.length > 0) {
  console.error('[check-diagrams] failed:\n');
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  process.exit(1);
}

console.log(
  `[check-diagrams] ok - ${mermaidBlocks.toString()} mermaid diagrams across ` +
    `${filesScanned.toString()} files, no ASCII box art.`,
);
