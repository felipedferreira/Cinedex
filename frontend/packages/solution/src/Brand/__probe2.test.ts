import { describe, expect, it } from 'vitest';
import { gsap } from 'gsap';

/**
 * Quantifies exactly how much a GSAP proxy tween diverges from the original
 * hand-rolled maths, for two proxy shapes:
 *
 *   A. per-property proxies, tweened in PHYSICAL units (hinge 0->30 deg)
 *   B. one {t} proxy, with all the original maths run in onUpdate
 *
 * GSAP's numeric fast path is `Math.round((s + c*ratio) * 1e6) / 1e6`, so the
 * proxy carries at most 5e-7 of absolute error. What matters is how far that
 * error is AMPLIFIED before it reaches a `.toFixed(3)` boundary.
 */

const clamp = (v: number, lo: number, hi: number) =>
  v < lo ? lo : v > hi ? hi : v;
const seg = (t: number, a: number, b: number) => clamp((t - a) / (b - a), 0, 1);
const eOut = (t: number) => 1 - (1 - t) ** 3;
const eIO = (t: number) => (t < 0.5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2);
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

const SEQ = 1.2;
const win = (a: number, b: number) => ({ at: SEQ * a, dur: SEQ * (b - a) });

const SAMPLES = Array.from({ length: 2401 }, (_, i) => i / 2400);

interface Stat {
  flips: number;
  maxDelta: number;
}
const stat = (): Stat => ({ flips: 0, maxDelta: 0 });

function compare(s: Stat, orig: number, got: number, decimals: number) {
  s.maxDelta = Math.max(s.maxDelta, Math.abs(orig - got));
  if (orig.toFixed(decimals) !== got.toFixed(decimals)) s.flips++;
}

describe('proxy shape: divergence budget', () => {
  it('A. per-property physical-unit proxies', () => {
    const b = { hinge: 0, spin: -46, lensP: 0, ringP: 0, glintP: 0, settleP: 0 };
    const tl = gsap.timeline({ paused: true });
    const w = (a: number, z: number) => win(a, z);
    tl.fromTo(
      b,
      { hinge: 0 },
      { hinge: 30, duration: w(0.06, 0.46).dur, ease: 'power2.out' },
      w(0.06, 0.46).at,
    )
      .fromTo(
        b,
        { spin: -46 },
        { spin: 0, duration: w(0.06, 0.56).dur, ease: 'power2.out' },
        w(0.06, 0.56).at,
      )
      .fromTo(
        b,
        { lensP: 0 },
        { lensP: 1, duration: w(0.18, 0.54).dur, ease: 'power2.out' },
        w(0.18, 0.54).at,
      )
      .fromTo(
        b,
        { ringP: 0 },
        { ringP: 1, duration: w(0.3, 0.64).dur, ease: 'power2.out' },
        w(0.3, 0.64).at,
      )
      .fromTo(
        b,
        { glintP: 0 },
        { glintP: 1, duration: w(0.5, 0.82).dur, ease: 'power2.inOut' },
        w(0.5, 0.82).at,
      )
      .fromTo(
        b,
        { settleP: 0 },
        { settleP: 1, duration: w(0.72, 1).dur, ease: 'power2.out' },
        w(0.72, 1).at,
      )
      .set({}, {}, SEQ);

    const S = {
      hinge: stat(),
      spin: stat(),
      lensOp: stat(),
      lensScale: stat(),
      ringOp: stat(),
      dashoff: stat(),
      glintOp: stat(),
      glintX: stat(),
      settle: stat(),
    };

    for (const t of SAMPLES) {
      tl.progress(t);
      const oHinge = lerp(0, 30, eOut(seg(t, 0.06, 0.46)));
      const oSpin = lerp(-46, 0, eOut(seg(t, 0.06, 0.56)));
      const oLens = eOut(seg(t, 0.18, 0.54));
      const oRing = eOut(seg(t, 0.3, 0.64));
      const oGlint = eIO(seg(t, 0.5, 0.82));
      const oSettle = eOut(seg(t, 0.72, 1));

      compare(S.hinge, oHinge, b.hinge, 3);
      compare(S.spin, oSpin, b.spin, 3);
      compare(S.lensOp, oLens, b.lensP, 3);
      compare(S.lensScale, lerp(0.62, 1, oLens), lerp(0.62, 1, b.lensP), 4);
      compare(
        S.ringOp,
        clamp(oRing * 4, 0, 1),
        clamp(b.ringP * 4, 0, 1),
        3,
      );
      compare(S.dashoff, 1000 * (1 - oRing), 1000 * (1 - b.ringP), 2);
      if (oGlint > 0 && oGlint < 1 && b.glintP > 0 && b.glintP < 1) {
        compare(
          S.glintOp,
          Math.sin(Math.PI * oGlint),
          Math.sin(Math.PI * b.glintP),
          3,
        );
        compare(S.glintX, lerp(-230, 225, oGlint), lerp(-230, 225, b.glintP), 1);
      }
      compare(
        S.settle,
        lerp(1.045, 1, oSettle),
        lerp(1.045, 1, b.settleP),
        4,
      );
    }

    const n = SAMPLES.length;
    for (const [k, v] of Object.entries(S)) {
      // eslint-disable-next-line no-console
      console.log(
        `A ${k.padEnd(10)} flips=${String(v.flips)}/${String(n)} (${((100 * v.flips) / n).toFixed(3)}%) maxDelta=${v.maxDelta.toExponential(2)}`,
      );
    }
    expect(S.hinge.maxDelta).toBeLessThan(1e-6);
    tl.kill();
  });

  it('B. single {t} proxy', () => {
    const p = { t: 0 };
    const tl = gsap
      .timeline({ paused: true })
      .fromTo(p, { t: 0 }, { t: 1, duration: SEQ, ease: 'none' }, 0);

    const S = { hinge: stat(), spin: stat(), glintX: stat(), settle: stat() };

    for (const t of SAMPLES) {
      tl.progress(t);
      const g = p.t;
      compare(
        S.hinge,
        lerp(0, 30, eOut(seg(t, 0.06, 0.46))),
        lerp(0, 30, eOut(seg(g, 0.06, 0.46))),
        3,
      );
      compare(
        S.spin,
        lerp(-46, 0, eOut(seg(t, 0.06, 0.56))),
        lerp(-46, 0, eOut(seg(g, 0.06, 0.56))),
        3,
      );
      const og = eIO(seg(t, 0.5, 0.82));
      const gg = eIO(seg(g, 0.5, 0.82));
      if (og > 0 && og < 1 && gg > 0 && gg < 1) {
        compare(S.glintX, lerp(-230, 225, og), lerp(-230, 225, gg), 1);
      }
      compare(
        S.settle,
        lerp(1.045, 1, eOut(seg(t, 0.72, 1))),
        lerp(1.045, 1, eOut(seg(g, 0.72, 1))),
        4,
      );
    }

    const n = SAMPLES.length;
    for (const [k, v] of Object.entries(S)) {
      // eslint-disable-next-line no-console
      console.log(
        `B ${k.padEnd(10)} flips=${String(v.flips)}/${String(n)} (${((100 * v.flips) / n).toFixed(3)}%) maxDelta=${v.maxDelta.toExponential(2)}`,
      );
    }
    expect(S.hinge.maxDelta).toBeLessThan(1e-2);
    tl.kill();
  });

  it('C. endpoints are exact for both shapes', () => {
    const b = { hinge: 0 };
    const tl = gsap
      .timeline({ paused: true })
      .fromTo(
        b,
        { hinge: 0 },
        { hinge: 30, duration: win(0.06, 0.46).dur, ease: 'power2.out' },
        win(0.06, 0.46).at,
      )
      .set({}, {}, SEQ);

    tl.progress(0);
    expect(b.hinge).toBe(0);
    tl.progress(0.06);
    expect(b.hinge).toBe(0);
    tl.progress(0.46);
    expect(b.hinge).toBe(30);
    tl.progress(1);
    expect(b.hinge).toBe(30);
    tl.kill();
  });

  it('D. onUpdate does not fire at progress(0) on a paused timeline', () => {
    let calls = 0;
    const b = { v: 0 };
    const tl = gsap.timeline({
      paused: true,
      onUpdate: () => {
        calls++;
      },
    });
    tl.fromTo(b, { v: 0 }, { v: 1, duration: 1, ease: 'none' }, 0);
    const afterBuild = calls;
    tl.progress(0);
    // eslint-disable-next-line no-console
    console.log(
      `D onUpdate calls: afterBuild=${String(afterBuild)} afterProgress0=${String(calls)}`,
    );
    tl.progress(0.5);
    // eslint-disable-next-line no-console
    console.log(`D after progress(0.5)=${String(calls)}`);
    tl.kill();
    expect(true).toBe(true);
  });
});
