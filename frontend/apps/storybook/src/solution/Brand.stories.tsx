import { useState, type ReactNode } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/atoms';
import {
  Brand,
  BrandApertureAnimation,
  BrandFocusRingsAnimation,
} from '@cinedex/solution';

/**
 * The Cinedex mark: a camera iris forming a "C". `Static` is what every
 * `AuthCard`-based screen shows via `Brand` — see `Solution/Screens`. The two
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
function BrandRow({ children }: { children: ReactNode }) {
  return <div className="flex items-center gap-2">{children}</div>;
}

/** Remounts its brand on every "Replay" click — the same key-remount
 *  technique `preview.tsx` uses to force the theme toolbar to retake effect. */
function ReplayableBrand({ children }: { children: () => ReactNode }) {
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
}

export const Static: Story = {
  render: () => (
    <BrandRow>
      <Brand />
    </BrandRow>
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
