import { Graphviz } from '@hpcc-js/wasm/graphviz';
import { Resvg } from '@resvg/resvg-js';
import fs from 'fs';
import path from 'path';

export const OUT = process.env.OUT_DIR || path.resolve(import.meta.dirname, '..');
const gvp = Graphviz.load();

export const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

export function save(relName, svg) {
  const p = path.join(OUT, relName);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p + '.svg', svg);
  const r = new Resvg(svg, {
    fitTo: { mode: 'zoom', value: 2 },
    background: 'white',
    font: { loadSystemFonts: true, defaultFontFamily: 'Arial' },
  });
  fs.writeFileSync(p + '.png', r.render().asPng());
}

// ---------- Graphviz ----------
export const COL = {
  ui: ['#E8F0FE', '#4A78C2'],
  logic: ['#E6F2E8', '#4F8F5A'],
  ent: ['#FFF6E5', '#C9922E'],
  infra: ['#EEEEEE', '#777777'],
  rec: ['#F1EBFA', '#7D5BB0'],
  svc: ['#F5ECF8', '#8E5AA8'],
};

export function cls(id, kind, st, name, members = []) {
  const [bg, bd] = COL[kind];
  const rows = members.map((m) => `<TR><TD ALIGN="LEFT" BALIGN="LEFT">${esc(m)}</TD></TR>`).join('');
  const head = `<TR><TD BGCOLOR="${bg}"><FONT POINT-SIZE="9" COLOR="#555555">${st ? '«' + esc(st) + '»' : ' '}</FONT><BR/><B>${esc(name)}</B></TD></TR>`;
  return `${id} [shape=plain label=<<TABLE BORDER="1" COLOR="${bd}" CELLBORDER="0" CELLSPACING="0" CELLPADDING="5" BGCOLOR="${bg}">${head}${rows ? `<HR/>${rows}` : ''}</TABLE>>];`;
}

export async function dot(src, relName, extra = '') {
  const gv = await gvp;
  const full = `digraph G { graph [fontname="Arial" pad=0.3 ${extra}]; node [fontname="Arial" fontsize=11]; edge [fontname="Arial" fontsize=10 color="#444444" fontcolor="#333333"]; ${src} }`;
  let svg = gv.dot(full, 'svg');
  svg = svg.replace(/font-family="[^"]*"/g, 'font-family="Arial"');
  save(relName, svg);
}

// ---------- Secuencia ----------
const FS = 12;
const tw = (s) => s.length * 6.3 + 14;

export function seq(spec, relName) {
  const { title, parts, steps } = spec;
  const idx = Object.fromEntries(parts.map((p, i) => [p.id, i]));
  const gap = Math.max(210, ...steps.flatMap(function f(s) { return s.alt ? s.alt.flatMap((o) => o.steps.flatMap(f)) : [s.text ? tw(s.text) + 20 : 0]; }));
  const X0 = 110;
  const xs = parts.map((_, i) => X0 + i * gap);
  const W = X0 + (parts.length - 1) * gap + 260;
  let body = '';
  let y = 140;
  const involved = (list) => {
    const s = new Set();
    (function walk(l) { for (const st of l) { if (st.alt) st.alt.forEach((o) => walk(o.steps)); else { s.add(idx[st.from]); s.add(idx[st.to]); if (st.from === st.to) s.add('self' + idx[st.from]); } } })(list);
    return s;
  };
  function run(list, depth) {
    for (const st of list) {
      if (st.alt) {
        const inv = involved(st.alt.flatMap((o) => o.steps));
        const ids = [...inv].filter((v) => typeof v === 'number');
        const selfRight = [...inv].some((v) => typeof v === 'string' && Number(v.slice(4)) === Math.max(...ids));
        const x1 = xs[Math.min(...ids)] - 34 - depth * 6;
        const x2 = xs[Math.max(...ids)] + (selfRight ? 230 : 34) + depth * 6;
        const top = y;
        y += 12;
        const dividers = [];
        st.alt.forEach((o, oi) => {
          if (oi > 0) { dividers.push([y, o.guard]); y += 8; } else { dividers.push([top, o.guard]); }
          y += 34;
          run(o.steps, depth + 1);
          y += 6;
        });
        const bottom = y;
        body += `<rect x="${x1}" y="${top}" width="${x2 - x1}" height="${bottom - top}" fill="none" stroke="#888" stroke-width="1.2"/>`;
        body += `<path d="M${x1} ${top} h44 v14 l-8 8 h-36 z" fill="#EDEDED" stroke="#888"/><text x="${x1 + 6}" y="${top + 15}" font-size="11" font-weight="bold">${esc(st.kind || 'alt')}</text>`;
        dividers.forEach(([dy, g], i) => {
          if (i > 0) body += `<line x1="${x1}" y1="${dy}" x2="${x2}" y2="${dy}" stroke="#888" stroke-dasharray="6 4"/>`;
          body += `<text x="${x1 + 52}" y="${dy + 15}" font-size="11" fill="#333">[${esc(g)}]</text>`;
        });
        y += 14;
        continue;
      }
      const a = xs[idx[st.from]], b = xs[idx[st.to]];
      const dash = st.ret ? ' stroke-dasharray="6 4"' : '';
      if (st.from === st.to) {
        body += `<path d="M${a} ${y} h42 v22 h-42" fill="none" stroke="#222"${dash}/>`;
        body += `<path d="M${a + 7} ${y + 22 - 4} l-7 4 l7 4 z" fill="#222"/>`;
        body += `<text x="${a + 50}" y="${y + 15}" font-size="${FS}">${esc(st.text)}</text>`;
        y += 44;
      } else {
        const dir = b > a ? 1 : -1;
        body += `<line x1="${a}" y1="${y}" x2="${b - dir * 1}" y2="${y}" stroke="#222"${dash}/>`;
        body += st.ret
          ? `<path d="M${b - dir * 9} ${y - 4} L${b} ${y} L${b - dir * 9} ${y + 4}" fill="none" stroke="#222"/>`
          : `<path d="M${b - dir * 10} ${y - 4} L${b} ${y} L${b - dir * 10} ${y + 4} z" fill="#222"/>`;
        body += `<text x="${(a + b) / 2}" y="${y - 6}" font-size="${FS}" text-anchor="middle">${esc(st.text)}</text>`;
        y += 40;
      }
    }
  }
  run(steps, 0);
  const H = y + 30;
  let svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" font-family="Arial"><rect width="100%" height="100%" fill="white"/>`;
  svg += `<text x="${W / 2 - 120}" y="30" font-size="17" font-weight="bold" fill="#1F3864" text-anchor="middle">${esc(title)}</text>`;
  parts.forEach((p, i) => {
    const w = Math.max(120, tw(p.label.replace(/\n/g, '')) );
    const lines = p.label.split('\n');
    const h = 24 + lines.length * 16;
    const fill = p.actor ? '#FFFFFF' : '#F2F2F2';
    if (p.actor) {
      svg += `<circle cx="${xs[i]}" cy="62" r="8" fill="none" stroke="#222"/><line x1="${xs[i]}" y1="70" x2="${xs[i]}" y2="90" stroke="#222"/><line x1="${xs[i] - 12}" y1="77" x2="${xs[i] + 12}" y2="77" stroke="#222"/><line x1="${xs[i]}" y1="90" x2="${xs[i] - 10}" y2="104" stroke="#222"/><line x1="${xs[i]}" y1="90" x2="${xs[i] + 10}" y2="104" stroke="#222"/>`;
      svg += `<line x1="${xs[i]}" y1="128" x2="${xs[i]}" y2="${H - 20}" stroke="#999" stroke-dasharray="4 4"/>`;
      svg += lines.map((l, k) => `<text x="${xs[i]}" y="${122 + k * 14}" font-size="12" text-anchor="middle" font-weight="bold">${esc(l)}</text>`).join('');
    } else {
      svg += `<rect x="${xs[i] - w / 2}" y="46" width="${w}" height="${h}" fill="${fill}" stroke="#555"/>`;
      svg += lines.map((l, k) => `<text x="${xs[i]}" y="${46 + 22 + k * 16}" font-size="12" text-anchor="middle" font-weight="bold">${esc(l)}</text>`).join('');
      svg += `<line x1="${xs[i]}" y1="${46 + h}" x2="${xs[i]}" y2="${H - 20}" stroke="#999" stroke-dasharray="4 4"/>`;
    }
  });
  svg += body + '</svg>';
  save(relName, svg);
}

// ---------- Gráfico de caso de uso ----------
export function ucg(spec, relName) {
  const { title, left = [], right = [], main, rel = [] } = spec;
  const n = rel.length;
  const W = Math.max(900, 260 * n + 300);
  const cx = W / 2, cy = 100 + (Math.max(left.length, right.length, 1) - 1) * 60 + 20, rx = Math.max(150, tw(main) / 2 + 10), ry = 40;
  const relY = cy + 175;
  const H = n ? relY + 90 : cy + 110;
  let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" font-family="Arial"><rect width="100%" height="100%" fill="white"/>`;
  s += `<text x="${W / 2}" y="32" font-size="17" font-weight="bold" fill="#1F3864" text-anchor="middle">${esc(title)}</text>`;
  const actor = (x, y, label) => `<circle cx="${x}" cy="${y - 28}" r="10" fill="none" stroke="#222" stroke-width="1.4"/><line x1="${x}" y1="${y - 18}" x2="${x}" y2="${y + 10}" stroke="#222" stroke-width="1.4"/><line x1="${x - 16}" y1="${y - 8}" x2="${x + 16}" y2="${y - 8}" stroke="#222" stroke-width="1.4"/><line x1="${x}" y1="${y + 10}" x2="${x - 13}" y2="${y + 30}" stroke="#222" stroke-width="1.4"/><line x1="${x}" y1="${y + 10}" x2="${x + 13}" y2="${y + 30}" stroke="#222" stroke-width="1.4"/><text x="${x}" y="${y + 50}" font-size="12" text-anchor="middle">${esc(label)}</text>`;
  const place = (list, x, side) => {
    const step = 120;
    const y0 = cy - ((list.length - 1) * step) / 2;
    list.forEach((a, i) => {
      const y = y0 + i * step;
      s += actor(x, y, a);
      const ex = cx + (side < 0 ? -1 : 1) * rx * Math.sqrt(Math.max(0, 1 - ((y - cy) / ry) ** 2 > 0 ? 0 : 0));
      const ty = Math.max(cy - ry + 8, Math.min(cy + ry - 8, y));
      const dx = rx * Math.sqrt(1 - ((ty - cy) / ry) ** 2);
      s += `<line x1="${x + side * -1 * -16}" y1="${y - 8}" x2="${cx + (side < 0 ? -dx : dx)}" y2="${ty}" stroke="#444"/>`;
    });
  };
  place(left, 110, 1);
  place(right, W - 110, -1);
  s += `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="#FFF6E5" stroke="#C9922E" stroke-width="1.4"/><text x="${cx}" y="${cy + 4}" font-size="13" text-anchor="middle">${esc(main)}</text>`;
  rel.forEach((r, i) => {
    const x = (W / (n + 1)) * (i + 1);
    const rrx = Math.max(110, tw(r.text) / 2 + 8), rry = 30;
    const off = (i - (n - 1) / 2) * 70;
    const ax = cx + off;
    const ay = cy + ry * Math.sqrt(Math.max(0, 1 - (off / rx) ** 2));
    const bx = x, by = relY - rry;
    const [x1, y1, x2, y2] = r.rel === 'include' ? [ax, ay, bx, by] : [bx, by, ax, ay];
    s += `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="#444" stroke-dasharray="7 5"/>`;
    const ang = Math.atan2(y2 - y1, x2 - x1);
    s += `<path d="M${x2 - 12 * Math.cos(ang - 0.4)} ${y2 - 12 * Math.sin(ang - 0.4)} L${x2} ${y2} L${x2 - 12 * Math.cos(ang + 0.4)} ${y2 - 12 * Math.sin(ang + 0.4)}" fill="none" stroke="#444"/>`;
    const mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
    const lab = `«${r.rel}»${r.cond ? ' ' + r.cond : ''}`;
    s += `<rect x="${mx - tw(lab) / 2 + 5}" y="${my - 10}" width="${tw(lab) - 10}" height="16" fill="white"/><text x="${mx}" y="${my + 3}" font-size="11" fill="#444" text-anchor="middle">${esc(lab)}</text>`;
    s += `<ellipse cx="${x}" cy="${relY}" rx="${rrx}" ry="${rry}" fill="${r.fill || '#F1EBFA'}" stroke="#7D5BB0" stroke-width="1.3"/><text x="${x}" y="${relY + 4}" font-size="12" text-anchor="middle">${esc(r.text)}</text>`;
  });
  s += '</svg>';
  save(relName, s);
}
