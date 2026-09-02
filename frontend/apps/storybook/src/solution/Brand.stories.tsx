import { useState, type ReactNode } from 'react';
import type { FC, PropsWithChildren } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/atoms';
import {
  Brand,
  BrandApertureAnimation,
  BrandFocusRingsAnimation,
} from '@cinedex/solution';
import type { BrandSize } from '@cinedex/solution';

const BRAND_SIZES = [
  'XS',
  'S',
  'M',
  'L',
  'XL',
] as const satisfies readonly BrandSize[];

const NUMERIC_BRAND_SIZES = [1, 2, 3, 4, 5, 6] as const;

/**
 * The Cinedex mark: a camera iris forming a "C". `Static` is what every
 * `AuthCard`-based screen shows via `Brand` — see `Solution/Auth Stories`. The two
 * animated variants each run their 1.2s sequence once on mount and settle
 * into that exact same static look; `HomeScreen` is the only current
 * consumer, using the aperture sequence. Both ship so either is a one-line
 * swap, and so they can be reviewed here on their own.
 */
const meta = {
  title: 'Solution/Brand',
  tags: ['autodocs'],
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

/** `AuthCard`'s own brand-row layout, reproduced here since these stories
 *  show `Brand` outside that context — see `AuthCard.tsx`'s header row. */
const BrandRow: FC<PropsWithChildren> = ({ children }) => {
  return <div className="flex items-center gap-2">{children}</div>;
};

/** Remounts its brand on every "Replay" click — the same key-remount
 *  technique `preview.tsx` uses to force the theme toolbar to retake effect. */
const ReplayableBrand: FC<{ children: () => ReactNode }> = ({ children }) => {
  const [run, setRun] = useState(0);

  return (
    <div className="flex flex-col items-start gap-3">
      <BrandRow key={run}>{children()}</BrandRow>
      <Button
        variant="outline"
        size="sm"
        onClick={() => {
          setRun((n) => n + 1);
        }}
      >
        Replay
      </Button>
    </div>
  );
};

export const Static: Story = {
  render: () => (
    <BrandRow>
      <Brand />
    </BrandRow>
  ),
};

/** Every supported lockup scale. `M` is the component default; `XS` retains
 *  the lockup dimensions used before sizing was introduced. */
export const Sizes: Story = {
  render: () => (
    <div className="flex flex-wrap items-end gap-x-8 gap-y-5">
      {BRAND_SIZES.map((size) => (
        <div key={size} className="flex flex-col items-start gap-2">
          <p className="font-mono text-label text-text uppercase">
            {size}
            {size === 'M' ? ' (default)' : ''}
          </p>
          <BrandRow>
            {size === 'M' ? <Brand /> : <Brand size={size} />}
          </BrandRow>
        </div>
      ))}
    </div>
  ),
};

/** Numeric steps 1–5 alias `XS`–`XL`; larger positive integers continue the
 *  lockup scale, so 6 is the first size above `XL`. */
export const NumericSizes: Story = {
  render: () => (
    <div className="flex flex-wrap items-end gap-x-8 gap-y-5">
      {NUMERIC_BRAND_SIZES.map((size) => (
        <div key={size} className="flex flex-col items-start gap-2">
          <p className="font-mono text-label text-text uppercase">
            {size}
            {size <= 5 ? ` (${BRAND_SIZES[size - 1]})` : ' (above XL)'}
          </p>
          <BrandRow>
            <Brand size={size} />
          </BrandRow>
        </div>
      ))}
    </div>
  ),
};

export const LensApertureIntro: Story = {
  render: () => (
    <ReplayableBrand>{() => <BrandApertureAnimation />}</ReplayableBrand>
  ),
};

export const FocusRingsIntro: Story = {
  render: () => (
    <ReplayableBrand>{() => <BrandFocusRingsAnimation />}</ReplayableBrand>
  ),
};
