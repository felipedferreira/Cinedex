import type { Ref } from 'react';
import {
  BLADE_PATH,
  BLADE_PIVOT_ANGLES_DEG,
  BLADE_STROKE,
  DOME_RADIUS,
  DOME_SPECULAR_PRIMARY,
  DOME_SPECULAR_SECONDARY,
  INNER_ARC_PATH,
  MARK_VIEW_BOX,
  OPEN_HINGE_DEG,
  OUTER_ARC_PATH,
  PIVOT_RADIUS,
} from './mark';
import type { ResolvedBrandSize } from './brandSize';
import { MarkDefs } from './MarkDefs';

export interface MarkBodyProps {
  uid: string;
  size: ResolvedBrandSize;
  ref?: Ref<SVGSVGElement>;
}

/**
 * The mark's full SVG body — `Brand`, `BrandApertureAnimation` and
 * `BrandFocusRingsAnimation` all render this and nothing else, so the three
 * stay pixel-identical and a change here reaches all of them.
 *
 * Rendered at rest (iris open, rings fully drawn, no ghosts, no glint) — the
 * settled state `Brand` shows statically and the two animated components
 * settle into after their sequence finishes. Authoring the settled state in the
 * markup rather than the opening one is what makes a pre-hydration or no-JS
 * render show a complete logo; the animated variants rewind to frame zero in a
 * layout effect, before first paint.
 *
 * Every animatable piece carries a `data-*` hook. `timelines.ts` builds its
 * GSAP tweens against those hooks off the forwarded `ref`, tweening SVG
 * attributes directly rather than React state — a 1.2s sequence is ~70 frames,
 * and none of `transform`, `stroke-dashoffset` or `opacity` needs to pass
 * through a diff. `Brand` itself passes no `ref` and builds no timeline, so
 * these attributes stay inert there.
 */
export const MarkBody = ({ uid, size, ref }: MarkBodyProps) => (
    <svg
      ref={ref}
      viewBox={MARK_VIEW_BOX}
      className={`${size.markClassName} shrink-0`}
      style={size.markStyle}
      role="img"
      aria-label="Cinedex"
    >
      <MarkDefs uid={uid} />

      <g data-settle="">
        <g data-assembly="">
          <circle
            data-barrel=""
            cx="100"
            cy="100"
            r={PIVOT_RADIUS}
            fill="light-dark(#16171B, #08090B)"
          />
          <g data-lens="">
            <circle
              cx="100"
              cy="100"
              r={DOME_RADIUS}
              fill={`url(#cdx-dome-${uid})`}
            />
            <circle
              cx={DOME_SPECULAR_PRIMARY.cx}
              cy={DOME_SPECULAR_PRIMARY.cy}
              r={DOME_SPECULAR_PRIMARY.r}
              fill="#fff"
            />
            <circle
              cx={DOME_SPECULAR_SECONDARY.cx}
              cy={DOME_SPECULAR_SECONDARY.cy}
              r={DOME_SPECULAR_SECONDARY.r}
              fill="#8A9099"
              opacity={DOME_SPECULAR_SECONDARY.opacity}
            />
          </g>
          <g
            data-irisclip=""
            clipPath={`url(#cdx-iris-${uid})`}
            fill={`url(#cdx-blade-${uid})`}
            stroke={BLADE_STROKE}
            strokeWidth="1.1"
          >
            <g data-spin="">
              {BLADE_PIVOT_ANGLES_DEG.map((angle) => (
                <g
                  key={angle}
                  transform={`translate(100,100) rotate(${String(angle)}) translate(${String(PIVOT_RADIUS)},0)`}
                >
                  <g
                    data-blade=""
                    transform={`rotate(${String(OPEN_HINGE_DEG)})`}
                  >
                    <path d={BLADE_PATH} />
                  </g>
                </g>
              ))}
            </g>
          </g>
        </g>

        {/* Ghost arcs — only `BrandFocusRingsAnimation` animates these in; inert
            (opacity 0) for `Brand` and `BrandApertureAnimation`. */}
        <g
          data-ghosts=""
          fill="none"
          stroke="#6E747D"
          strokeLinecap="butt"
          opacity="0"
        >
          <path data-ghost="" d={OUTER_ARC_PATH} strokeWidth="2.5" />
          <path
            data-ghost=""
            d="M144.35 47.14A69 69 0 1 0 144.35 152.86"
            strokeWidth="1.6"
          />
          <path data-ghost="" d={INNER_ARC_PATH} strokeWidth="2" />
        </g>

        <g data-rings="" fill="none" strokeLinecap="butt">
          <path
            data-outer=""
            d={OUTER_ARC_PATH}
            stroke={`url(#cdx-ring-${uid})`}
            strokeWidth="15"
            pathLength={1000}
          />
          <path
            data-inner=""
            d={INNER_ARC_PATH}
            stroke={`url(#cdx-ring-${uid})`}
            strokeWidth="9"
            pathLength={1000}
          />
        </g>

        {/* Light sweep — only the two animated components ever raise this above
            0 opacity. Masked to the mark's own silhouette so it never spills
            outside the "C" or the barrel. */}
        <g
          data-glintwrap=""
          mask={`url(#cdx-glintmask-${uid})`}
          style={{ mixBlendMode: 'screen' }}
          opacity="0"
        >
          <g transform="rotate(-18 100 100)">
            <rect
              data-glint=""
              x="-220"
              y="-70"
              width="66"
              height="340"
              fill={`url(#cdx-glint-${uid})`}
            />
          </g>
        </g>
      </g>
    </svg>
  );
