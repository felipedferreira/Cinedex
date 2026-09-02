import type { FC } from 'react';
import {
  BLADE_GRADIENT_STOPS,
  BLADE_GRADIENT_VECTOR,
  DOME_GRADIENT_STOPS,
  GLINT_GRADIENT_STOPS,
  INNER_ARC_PATH,
  OUTER_ARC_PATH,
  PIVOT_RADIUS,
  RING_GRADIENT_STOPS,
} from './mark';

export interface MarkDefsProps {
  /** Scopes every id to one mounted instance — see `Brand.tsx` for why. */
  uid: string;
}

/**
 * The mark's gradients and clip path, shared by `Brand`,
 * `BrandApertureAnimation` and `BrandFocusRingsAnimation` so the three stay
 * pixel-identical and a fill tweak in `mark.ts` reaches all of them.
 *
 * Not exported from the package barrel — an internal building block, not part
 * of `@cinedex/solution`'s public surface.
 */
export const MarkDefs: FC<MarkDefsProps> = ({ uid }) => {
  return (
    <defs>
      <linearGradient id={`cdx-ring-${uid}`} x1="0" y1="0" x2="1" y2="1">
        {RING_GRADIENT_STOPS.map((stop) => (
          <stop key={stop.offset} offset={stop.offset} stopColor={stop.color} />
        ))}
      </linearGradient>
      <linearGradient
        id={`cdx-blade-${uid}`}
        gradientUnits="userSpaceOnUse"
        x1={BLADE_GRADIENT_VECTOR.x1}
        y1={BLADE_GRADIENT_VECTOR.y1}
        x2={BLADE_GRADIENT_VECTOR.x2}
        y2={BLADE_GRADIENT_VECTOR.y2}
      >
        {BLADE_GRADIENT_STOPS.map((stop) => (
          <stop key={stop.offset} offset={stop.offset} stopColor={stop.color} />
        ))}
      </linearGradient>
      <radialGradient id={`cdx-dome-${uid}`} cx=".34" cy=".28" r=".82">
        {DOME_GRADIENT_STOPS.map((stop) => (
          <stop key={stop.offset} offset={stop.offset} stopColor={stop.color} />
        ))}
      </radialGradient>
      <clipPath id={`cdx-iris-${uid}`} clipPathUnits="userSpaceOnUse">
        <circle cx="100" cy="100" r={PIVOT_RADIUS} />
      </clipPath>

      {/* Only `BrandApertureAnimation`/`BrandFocusRingsAnimation` raise this
          above 0 opacity — inert defs for `Brand`'s static render. */}
      <linearGradient id={`cdx-glint-${uid}`} x1="0" y1="0" x2="1" y2="0">
        {GLINT_GRADIENT_STOPS.map((stop) => (
          <stop
            key={stop.offset}
            offset={stop.offset}
            stopColor={stop.color}
            stopOpacity={stop.opacity}
          />
        ))}
      </linearGradient>
      <mask
        id={`cdx-glintmask-${uid}`}
        maskUnits="userSpaceOnUse"
        x="0"
        y="0"
        width="200"
        height="200"
      >
        <g fill="none" stroke="#fff" strokeLinecap="butt">
          <path d={OUTER_ARC_PATH} strokeWidth="15" />
          <path d={INNER_ARC_PATH} strokeWidth="9" />
        </g>
        <circle cx="100" cy="100" r={PIVOT_RADIUS} fill="#fff" />
      </mask>
    </defs>
  );
};
