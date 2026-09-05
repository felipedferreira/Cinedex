// Enforces the rule that a Storybook story may only demo a prop value that a
// screen already passes.
//
// Why this needs enforcing at all: a story that enumerates a complete matrix
// pulls the component into growing surface to fill it, and the result compiles,
// lints, tests and renders perfectly. `AuthCard.kickerTone` shipped that way — a
// four-member union plus a `kickerToneClass` record whose only consumers in the
// entire repo were an `argTypes` inline-radio and one story. It survived a
// release, and the CHANGELOG entry that removed its last two real callers
// explicitly decided to keep it. Three more had the same fingerprint one tier
// down: `Button`'s `ghost` variant and `sm` size, and `Alert`'s `success` tone.
// Nothing in a green build says "this option exists only so the control has a
// third radio button".
//
// The second failure mode is subtler: the matrix reads as *documentation*. A
// props table listing four tones is a promise the product never made, and the
// next contributor designs against it.
//
// Scope note: this owns the story-side rule. `frontend/eslint.config.js`'s
// `no-restricted-syntax` block owns the shape-side one (no `ComponentType`
// props, no `render*` props, no `children` member on a `*Props` interface).
// Those are per-file AST checks; this one needs the whole repo at once, which is
// why it is a script and not a lint rule.
//
// Deliberate limits. This scans `argTypes.<prop>.options` arrays only — the form
// every confirmed instance took — and not values passed solely in a story's
// `args`. It matches text, not types: it cannot see a value reached through a
// variable, a spread, or a computed key. It is a tripwire for the specific
// pattern that has actually bitten, not a type checker. A finding is a prompt to
// look, and the fix is usually to delete the option rather than to add a screen.
//
// Style note: single quotes, no Prettier pass — matching `check-diagrams.mjs`.
// The repo root has no Prettier config and `format:check` runs from `frontend/`,
// so nothing formats this directory; keep the two scripts alike by hand.
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { join, relative } from 'node:path';

// Where a value has to appear to count as real. Storybook is excluded on
// purpose — it is the thing being audited — and so are tests: a test that loops
// every member of a union proves the code path renders, not that the product
// uses it. Both `Alert.test.tsx` and `Button.test.tsx` looped members no screen
// passed, which is why their `it.each` tuples shrank when those members went.
const PRODUCTION_GLOBS = [
  'frontend/packages/*/src/**',
  'frontend/apps/cinedex-app/src/**',
  'frontend/apps/docs-site/src/**',
];

const isTest = (path) => /\.test\.[cm]?[jt]sx?$/.test(path);

// Prop values legitimately absent from production. Every entry needs a reason,
// and the reason has to be about the product, not about convenience. Keep this
// list short — an allowlist that grows is the failure mode of a check like this.
const EXEMPTIONS = new Map([
  [
    'Separator.orientation=vertical',
    'Not our prop to delete: `SeparatorProps` is `ComponentProps<typeof ' +
      'SeparatorPrimitive.Root>`, a pure Radix passthrough, so removing a ' +
      'member would mean an `Omit` fighting the library. Every production ' +
      '`<Separator>` is horizontal, which makes the vertical arm of the ' +
      "component's own ternary dead styling to prune — a different, smaller job.",
  ],
]);

const errors = [];
let optionsScanned = 0;

// Anchored at the repo root, not the working directory: `git ls-files` scopes to
// cwd by default, so running this from `frontend/` (via `npm run check:props`)
// would match none of the repo-relative globs above and the check would pass
// vacuously. The vacuity guard at the bottom caught exactly that.
const REPO_ROOT = execFileSync('git', ['rev-parse', '--show-toplevel'], {
  encoding: 'utf-8',
}).trim();

const gitFiles = (patterns) =>
  execFileSync('git', ['ls-files', '-z', ...patterns], {
    encoding: 'utf-8',
    cwd: REPO_ROOT,
  })
    .split('\0')
    .filter(Boolean)
    .map((file) => join(REPO_ROOT, file));

/**
 * Every default prop value in the component tiers, as `Component.prop=value`.
 *
 * A default is reached by every call site that omits the prop, so it is live
 * even though nothing names it. Without this the check reports `Button`'s
 * `primary` and `md` and `Alert`'s `neutral` — three of the most-used values in
 * the app — as speculative, which is exactly backwards.
 *
 * Two forms, because the repo uses both: cva's `defaultVariants` block
 * (`Button`, `Alert`, `Input`) and a plain destructuring default in the
 * component's own signature (`Label`'s `tone = 'default'`, `Separator`'s
 * `orientation = 'horizontal'`).
 */
const defaultPropValues = () => {
  const defaults = new Set();

  // cva: `buttonVariants.ts` -> Button
  for (const file of gitFiles(['frontend/packages/*/src/**/*Variants.ts'])) {
    const base = /([a-z]\w*)Variants\.ts$/.exec(file)?.[1];
    if (!base) continue;
    const component = base.charAt(0).toUpperCase() + base.slice(1);

    const block = /defaultVariants:\s*\{([^}]*)\}/s.exec(
      readFileSync(file, 'utf-8'),
    )?.[1];
    if (!block) continue;

    for (const [, prop, value] of block.matchAll(
      /(\w+)\s*:\s*['"]?([\w-]+)['"]?/g,
    )) {
      defaults.add(`${component}.${prop}=${value}`);
    }
  }

  // Destructuring defaults: `Separator.tsx` -> Separator, orientation = 'horizontal'
  for (const file of gitFiles(['frontend/packages/*/src/**/*.tsx'])) {
    if (isTest(file)) continue;
    const component = /([A-Z]\w*)\.tsx$/.exec(file)?.[1];
    if (!component) continue;

    for (const [, prop, value] of readFileSync(file, 'utf-8').matchAll(
      /^\s{2}(\w+)\s*=\s*['"]([\w-]+)['"],$/gm,
    )) {
      defaults.add(`${component}.${prop}=${value}`);
    }
  }

  return defaults;
};

const storyFiles = gitFiles(['frontend/apps/storybook/src/**/*.stories.tsx']);
const production = gitFiles(PRODUCTION_GLOBS)
  .filter((file) => !isTest(file))
  .map((file) => readFileSync(file, 'utf-8'));

const DEFAULTS = defaultPropValues();

/**
 * Does any production file pass `prop={value}` to `<component>`?
 *
 * Component-scoped on purpose. `'success'` belongs to both `AlertTone` and
 * `StatPair`'s `StatTone`, and the `StatPair` one is live — a bare text search
 * for `tone: 'success'` would report the opposite of the truth for `Alert`.
 * Matching `<Component …>` first is what keeps the two apart.
 */
const hasProductionConsumer = (component, prop, value) => {
  // A JSX attribute on the component's own tag: <Button variant="ghost" …>.
  const jsx = new RegExp(
    `<${component}\\b[^>]*?\\b${prop}\\s*=\\s*["'{]\\s*['"]?${value}\\b`,
    's',
  );

  // A cva helper call: buttonVariants({ variant: 'ghost' }). These style a
  // non-component element with the component's own classes, so they are real
  // consumers even though no JSX tag names the component.
  const cvaName = component.charAt(0).toLowerCase() + component.slice(1);
  const cva = new RegExp(
    `${cvaName}Variants\\s*\\(\\s*\\{[^}]*?\\b${prop}\\s*:\\s*['"]${value}['"]`,
    's',
  );

  return production.some((text) => jsx.test(text) || cva.test(text));
};

for (const file of storyFiles) {
  const text = readFileSync(file, 'utf-8');

  // `component: Button` in the CSF meta. Every story file in this repo sets it —
  // `apps/storybook/CLAUDE.md` requires it, because a meta without one gets no
  // props table. Without it there is nothing to scope a search to, so skip.
  const component = /\bcomponent:\s*([A-Z]\w*)/.exec(text)?.[1];
  if (!component) continue;

  // `<prop>: { … options: ['a', 'b'] … }` inside argTypes.
  const optionsBlock = /(\w+)\s*:\s*\{[^{}]*?options:\s*\[([^\]]*)\][^{}]*?\}/gs;

  for (const [, prop, rawValues] of text.matchAll(optionsBlock)) {
    const values = [...rawValues.matchAll(/['"]([^'"]+)['"]/g)].map((m) => m[1]);

    for (const value of values) {
      optionsScanned += 1;

      const key = `${component}.${prop}=${value}`;
      if (EXEMPTIONS.has(key)) continue;
      if (DEFAULTS.has(key)) continue;
      if (hasProductionConsumer(component, prop, value)) continue;

      const where = relative(REPO_ROOT, file).replace(/\\/g, '/');

      errors.push(
        `${where}: \`${component}\` story offers ${prop}="${value}", which no ` +
          'screen passes. Delete the option (and the variant definition behind ' +
          'it), or add the call site that needs it. If it genuinely must stay, ' +
          `add "${key}" to EXEMPTIONS in this script with a reason.`,
      );
    }
  }
}

if (optionsScanned === 0) {
  errors.push(
    'No argTypes options found in any story. If that is genuinely intended, ' +
      'delete this check rather than leaving it passing vacuously.',
  );
}

if (errors.length > 0) {
  console.error('[check-speculative-props] failed:\n');
  for (const error of errors) console.error(`  - ${error}`);
  console.error('');
  process.exit(1);
}

console.log(
  `[check-speculative-props] ok - ${optionsScanned.toString()} story options ` +
    `across ${storyFiles.length.toString()} story files, every one passed by a screen.`,
);
