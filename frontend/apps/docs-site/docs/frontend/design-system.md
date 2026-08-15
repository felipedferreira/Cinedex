---
sidebar_position: 2
---

# Design choices and theme

The visual system is deliberately **amethyst, disciplined**: a restrained
violet accent sits alongside neutral surfaces with a subtle violet cast. The
accent directs attention; it is not the background for every decision. This
keeps the interface calm enough for dense catalog and account workflows while
giving Cinedex a distinct identity.

## One source of truth

`@cinedex/theme` owns the design tokens. Components consume semantic utility
names such as `bg-bg`, `text-label`, and `tracking-eyebrow`; they do not carry
their own color values or invented type sizes. A rebrand therefore begins in
the token file, not with a search through every component.

```css
:root {
  --bg: light-dark(#fff, #17151b);
  --text-h: light-dark(#120f18, #f2f1f5);
  --accent: light-dark(#6d41a9, #bc98f9);
}
```

The same tokens feed the SPA, Storybook, and this documentation site. The docs
site maps Docusaurus's Infima variables to the Cinedex tokens rather than
maintaining a second palette, so documentation and product remain visually
aligned.

## Palette at a glance

The swatches below use the live token values, so they follow this page's light
or dark theme. The labels show the source value for each mode; `--bg` is the
reference ground for the palette.

<div className="cinedex-palette" role="list" aria-label="Cinedex color tokens">
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--bg" />
    <strong>ground</strong>
    <code><span>--bg</span><span>#fff / #17151b</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--code-bg" />
    <strong>paper</strong>
    <code><span>--code-bg</span><span>#f5f3ee / #201e25</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--border" />
    <strong>rule</strong>
    <code><span>--border</span><span>#e5e4e8 / #34313a</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--border-strong" />
    <strong>edge</strong>
    <code><span>--border-strong</span><span>#93909a / #57535f</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--text" />
    <strong>quiet</strong>
    <code><span>--text</span><span>#605b6a / #a6a2b0</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--text-h" />
    <strong>ink</strong>
    <code><span>--text-h</span><span>#120f18 / #f2f1f5</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--accent" />
    <strong>accent</strong>
    <code><span>--accent</span><span>#6d41a9 / #bc98f9</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--danger" />
    <strong>danger</strong>
    <code><span>--danger</span><span>#a83630 / #e8796e</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--warning" />
    <strong>warning</strong>
    <code><span>--warning</span><span>#8a5619 / #dea45f</span></code>
  </div>
  <div className="cinedex-palette__token" role="listitem">
    <div className="cinedex-palette__swatch cinedex-palette__swatch--success" />
    <strong>success</strong>
    <code><span>--success</span><span>#037465 / #5fc6b3</span></code>
  </div>
</div>

## Palette rules

The palette favors hierarchy over decoration:

- **Surfaces and ink** establish a high-contrast reading environment in both
  themes.
- **The amethyst accent** is intentionally less chromatic than the error state;
  status feedback must always remain the loudest visual signal.
- **Violet-tinted neutrals** avoid the detached blue-grey cast of stock ramps.
- **Two border strengths** distinguish quiet separation from controls that a
  person must locate and use, such as inputs and outline buttons.
- **Status colors** each provide a foreground, tinted background, and border,
  so alerts and validation feedback use one coherent token set instead of
  ad-hoc opacity values.

## Theme behavior and typography

Light and dark values are declared together with CSS `light-dark()`. The used
`color-scheme` selects the active value: system preference is the default,
while Storybook's toolbar can force Light or Dark. Native controls receive
matching browser chrome as part of the same mechanism.

Typography is role-based. The compact mono styles identify labels, eyebrow
copy, and the wordmark; the sans-serif scale serves reading and headings.
Components ask for a role such as `text-body` or `text-title`, which lets the
system tune the scale without changing component APIs.

## Review in Storybook

The [Storybook workbench](http://localhost:9001) is the live reference for
these choices. Use its theme toolbar to compare both color schemes and its
accessibility panel to check interactive components as they are composed. Run
it locally with `npm run storybook` from `frontend/`.
