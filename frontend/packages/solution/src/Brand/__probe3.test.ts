import { describe, expect, it } from 'vitest';
import { gsap } from 'gsap';

const clamp = (v: number, lo: number, hi: number) =>
  v < lo ? lo : v > hi ? hi : v;
const seg = (t: number, a: number, b: number) => clamp((t - a) / (b - a), 0, 1);
const eOut = (t: number) => 1 - (1 - t) ** 3;
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
const SEQ = 1.2;
const win = (a: number, b: number) => ({ at: SEQ * a, dur: SEQ * (b - a) });
const SAMPLES = Array.from({ length: 2401 }, (_, i) => i / 2400);

describe('refinements', () => {
  it('E. dash offset tweened in ATTRIBUTE units (1000->0) instead of 0..1', () => {
    const b = { dashoff: 1000 };
    const w = win(0.3, 0.64);
    const tl = gsap
      .timeline({ paused: true })
      .fromTo(
        b,
        { dashoff: 1000 },
        { dashoff: 0, duration: w.dur, ease: 'power2.out' },
        w.at,
      )
      .set({}, {}, SEQ);

    let flips = 0;
    let maxDelta = 0;
    let ringFlips = 0;
    for (const t of SAMPLES) {
      tl.progress(t);
      const ringP = eOut(seg(t, 0.3, 0.64));
      const orig = 1000 * (1 - ringP);
      maxDelta = Math.max(maxDelta, Math.abs(orig - b.dashoff));
      if (orig.toFixed(2) !== b.dashoff.toFixed(2)) flips++;
      // group opacity derived back out of the same proxy
      const derived = clamp((1 - b.dashoff / 1000) * 4, 0, 1);
      if (clamp(ringP * 4, 0, 1).toFixed(3) !== derived.toFixed(3)) ringFlips++;
    }
    // eslint-disable-next-line no-console
    console.log(
      `E dashoff flips=${String(flips)}/${String(SAMPLES.length)} maxDelta=${maxDelta.toExponential(2)} | derived ringOp flips=${String(ringFlips)}`,
    );
    tl.kill();
    expect(flips).toBe(0);
  });

  it('F. is tl.progress() read inside onUpdate exact (unrounded)?', () => {
    const seen: number[] = [];
    const tl = gsap.timeline({
      paused: true,
      onUpdate() {
        seen.push(tl.progress());
      },
    });
    tl.to({}, { duration: SEQ, ease: 'none' }, 0);

    let worst = 0;
    for (const t of SAMPLES) {
      seen.length = 0;
      tl.progress(t);
      if (seen.length) worst = Math.max(worst, Math.abs(seen[0] - t));
    }
    // eslint-disable-next-line no-console
    console.log(`F max |tl.progress() - requested| = ${worst.toExponential(3)}`);
    tl.kill();
    expect(worst).toBeLessThan(1e-12);
  });

  it('G. glint x tweened in attribute units (-230 -> 225)', () => {
    const b = { x: -230 };
    const w = win(0.5, 0.82);
    const tl = gsap
      .timeline({ paused: true })
      .fromTo(
        b,
        { x: -230 },
        { x: 225, duration: w.dur, ease: 'power2.inOut' },
        w.at,
      )
      .set({}, {}, SEQ);

    const eIO = (t: number) =>
      t < 0.5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2;
    let flips = 0;
    let maxDelta = 0;
    for (const t of SAMPLES) {
      tl.progress(t);
      const orig = lerp(-230, 225, eIO(seg(t, 0.5, 0.82)));
      maxDelta = Math.max(maxDelta, Math.abs(orig - b.x));
      if (orig.toFixed(1) !== b.x.toFixed(1)) flips++;
    }
    // eslint-disable-next-line no-console
    console.log(
      `G glintX flips=${String(flips)}/${String(SAMPLES.length)} maxDelta=${maxDelta.toExponential(2)}`,
    );
    tl.kill();
    expect(flips).toBe(0);
  });

  it('H. a timeline whose last child ends before SEQ needs padding', () => {
    const a = gsap.timeline({ paused: true });
    a.to({}, { duration: win(0.06, 0.46).dur }, win(0.06, 0.46).at);
    // eslint-disable-next-line no-console
    console.log(`H unpadded duration = ${String(a.duration())}`);
    a.set({}, {}, SEQ);
    // eslint-disable-next-line no-console
    console.log(`H padded duration   = ${String(a.duration())}`);
    expect(a.duration()).toBeCloseTo(SEQ, 12);
    a.kill();
  });

  it('I. kill() leaves values put; revert() rewinds to pre-timeline', () => {
    const el = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    el.setAttribute('transform', 'rotate(30)');
    document.body.append(el);
    const proxy = { hinge: 30 };
    const tl = gsap.timeline({
      paused: true,
      onUpdate: () => {
        el.setAttribute('transform', `rotate(${proxy.hinge.toFixed(3)})`);
      },
    });
    tl.fromTo(proxy, { hinge: 0 }, { hinge: 30, duration: SEQ, ease: 'none' }, 0);
    tl.progress(0.4);
    const mid = el.getAttribute('transform');
    tl.kill();
    // eslint-disable-next-line no-console
    console.log(
      `I mid=${String(mid)} afterKill=${String(el.getAttribute('transform'))}`,
    );
    expect(el.getAttribute('transform')).toBe(mid);
  });
});
