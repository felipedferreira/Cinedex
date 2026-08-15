/* eslint-disable */
import { describe, it } from 'vitest';
import { gsap } from 'gsap';

function enableSvgMode() {
  const proto: any = (globalThis as any).SVGElement.prototype;
  proto.getCTM = function () {
    return { a: 1, b: 0, c: 0, d: 1, e: 0, f: 0 };
  };
  proto.getBBox = function () {
    return { x: 0, y: 0, width: 100, height: 100 };
  };
  Object.defineProperty(proto, 'transform', {
    configurable: true,
    get() {
      const el = this as Element;
      return {
        baseVal: {
          consolidate: () => {
            const raw = el.getAttribute('transform');
            return raw ? { matrix: parseTransform(raw) } : null;
          },
        },
      };
    },
  });
}
function parseTransform(s: string) {
  let m = [1, 0, 0, 1, 0, 0];
  const re = /(matrix|translate|scale|rotate)\(([^)]*)\)/g;
  let k: RegExpExecArray | null;
  while ((k = re.exec(s))) {
    const n = k[2].split(/[\s,]+/).filter(Boolean).map(Number);
    let t = [1, 0, 0, 1, 0, 0];
    if (k[1] === 'translate') t = [1, 0, 0, 1, n[0], n[1] || 0];
    else if (k[1] === 'scale') t = [n[0], 0, 0, n.length > 1 ? n[1] : n[0], 0, 0];
    else if (k[1] === 'rotate') {
      const r = (n[0] * Math.PI) / 180, c = Math.cos(r), si = Math.sin(r);
      t = [c, si, -si, c, 0, 0];
      if (n.length === 3) t = mul([1, 0, 0, 1, n[1], n[2]], mul(t, [1, 0, 0, 1, -n[1], -n[2]]));
    } else t = n as any;
    m = mul(m, t);
  }
  return { a: m[0], b: m[1], c: m[2], d: m[3], e: m[4], f: m[5] };
}
function mul(x: number[], y: number[]) {
  return [
    x[0] * y[0] + x[2] * y[1], x[1] * y[0] + x[3] * y[1],
    x[0] * y[2] + x[2] * y[3], x[1] * y[2] + x[3] * y[3],
    x[0] * y[4] + x[2] * y[5] + x[4], x[1] * y[4] + x[3] * y[5] + x[5],
  ];
}

const clamp = (v: number, a: number, b: number) => (v < a ? a : v > b ? b : v);
const seg = (t: number, a: number, b: number) => clamp((t - a) / (b - a), 0, 1);
const eOut = (t: number) => 1 - (1 - t) ** 3;

describe('probe3', () => {
  it('composite eases, cleanup, residue', () => {
    enableSvgMode();
    const p2out = gsap.parseEase('power2.out');

    console.log('=== 1. focus-rings ring opacity as ONE function ease ===');
    // original: clamp(eOut(seg(t,0,.16))*.42 + eOut(seg(t,.14,.54))*.58, 0, 1)
    // one tween spanning t in [0, .54] -> local p = t/.54
    const SPAN = 0.54;
    const ringEase = (p: number) => {
      const t = p * SPAN;
      return clamp(
        p2out(seg(t, 0, 0.16)) * 0.42 + p2out(seg(t, 0.14, 0.54)) * 0.58,
        0,
        1,
      );
    };
    let maxErr = 0;
    for (let i = 0; i <= 2000; i++) {
      const t = i / 2000;
      const original = clamp(
        eOut(seg(t, 0, 0.16)) * 0.42 + eOut(seg(t, 0.14, 0.54)) * 0.58,
        0,
        1,
      );
      const viaEase = t >= SPAN ? ringEase(1) : ringEase(t / SPAN);
      maxErr = Math.max(maxErr, Math.abs(original - viaEase));
    }
    console.log(' max err vs original =', maxErr);
    console.log(' ease(0)=', ringEase(0), ' ease(1)=', ringEase(1));

    console.log('=== 2. ghost fade (non-monotone) as ONE function ease ===');
    // original: eOut(seg(t,0,.1)) * (1 - eOut(seg(t,.16,.5)))
    const GSPAN = 0.5;
    const ghostEase = (p: number) => {
      const t = p * GSPAN;
      return p2out(seg(t, 0, 0.1)) * (1 - p2out(seg(t, 0.16, 0.5)));
    };
    let gErr = 0;
    for (let i = 0; i <= 2000; i++) {
      const t = i / 2000;
      const original = eOut(seg(t, 0, 0.1)) * (1 - eOut(seg(t, 0.16, 0.5)));
      const viaEase = t >= GSPAN ? ghostEase(1) : ghostEase(t / GSPAN);
      gErr = Math.max(gErr, Math.abs(original - viaEase));
    }
    console.log(' max err vs original =', gErr);
    console.log(' ease(0)=', ghostEase(0), ' ease(1)=', ghostEase(1), ' peak~', ghostEase(0.2));

    console.log('=== 3. does a non-monotone ease actually render? ===');
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    g.setAttribute('opacity', '0');
    svg.append(g);
    document.body.append(svg);
    const t3 = gsap.timeline({ paused: true });
    t3.fromTo(g, { opacity: 0 }, { opacity: 1, duration: 0.6, ease: ghostEase }, 0);
    for (const p of [0, 0.2, 0.4, 1]) {
      t3.progress(p);
      console.log(`  p=${p} style.opacity=`, (g as any).style.opacity, ' attr=', g.getAttribute('opacity'));
    }
    t3.kill();

    console.log('=== 4. kill vs revert on a transform tween ===');
    const svg2 = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg2.innerHTML = `<g data-blade transform="rotate(30)"></g><g data-spin></g>`;
    document.body.append(svg2);
    const blade = svg2.querySelector('[data-blade]')!;
    const spin = svg2.querySelector('[data-spin]')!;
    console.log(' authored blade   =', JSON.stringify(blade.getAttribute('transform')));

    const t4 = gsap.timeline({ paused: true });
    t4.fromTo(blade, { rotation: 0, svgOrigin: '0 0' }, { rotation: 30, duration: 1 }, 0);
    t4.progress(0.4);
    console.log(' mid-flight       =', JSON.stringify(blade.getAttribute('transform')));
    console.log(' data-svg-origin  =', JSON.stringify(blade.getAttribute('data-svg-origin')));
    console.log(' style.transformOrigin =', JSON.stringify((blade as any).style.transformOrigin));
    t4.revert();
    console.log(' after revert()   =', JSON.stringify(blade.getAttribute('transform')));
    console.log(' origin residue   =', JSON.stringify(blade.getAttribute('data-svg-origin')));

    const t5 = gsap.timeline({ paused: true });
    t5.fromTo(spin, { rotation: -46, svgOrigin: '100 100' }, { rotation: 0, duration: 1 }, 0);
    t5.progress(0.4);
    console.log(' spin mid         =', JSON.stringify(spin.getAttribute('transform')));
    t5.revert();
    console.log(' spin after revert=', JSON.stringify(spin.getAttribute('transform')));
    console.log(' spin origin res  =', JSON.stringify(spin.getAttribute('data-svg-origin')));

    console.log('=== 5. context + selector scope + revert ===');
    const svg3 = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg3.innerHTML = `<g data-settle><g data-blade transform="rotate(30)"></g><g data-blade transform="rotate(30)"></g></g>`;
    document.body.append(svg3);
    let tlRef: any;
    const ctx = gsap.context(() => {
      tlRef = gsap.timeline({ paused: true });
      tlRef.fromTo('[data-blade]', { rotation: 0, svgOrigin: '0 0' }, { rotation: 30, duration: 1, stagger: 0 }, 0);
    }, svg3 as any);
    tlRef.progress(0.5);
    console.log(' scoped blades    =', [...svg3.querySelectorAll('[data-blade]')].map((b) => b.getAttribute('transform')));
    ctx.revert();
    console.log(' after ctx.revert =', [...svg3.querySelectorAll('[data-blade]')].map((b) => b.getAttribute('transform')));
    console.log(' origin residue   =', [...svg3.querySelectorAll('[data-blade]')].map((b) => b.getAttribute('data-svg-origin')));

    console.log('=== 6. reduced motion: build paused + progress(1), settled DOM ===');
    const svg4 = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg4.innerHTML = `<g data-settle><g data-blade transform="rotate(30)"></g></g>`;
    document.body.append(svg4);
    const b4 = svg4.querySelector('[data-blade]')!;
    const s4 = svg4.querySelector('[data-settle]')!;
    const t6 = gsap.timeline({ paused: true });
    t6.fromTo(b4, { rotation: 0, svgOrigin: '0 0' }, { rotation: 30, duration: 1 }, 0)
      .fromTo(s4, { scale: 1.045, svgOrigin: '100 100' }, { scale: 1, duration: 1 }, 0);
    console.log(' ticker frame before =', gsap.ticker.frame);
    t6.progress(1);
    console.log(' ticker frame after  =', gsap.ticker.frame);
    console.log(' blade  =', JSON.stringify(b4.getAttribute('transform')));
    console.log(' settle =', JSON.stringify(s4.getAttribute('transform')));
    t6.kill();
  });
});
