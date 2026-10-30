// Generador de diagramas draw.io (.drawio) para OpenRIN.
// El layout de clases/DER lo calcula Graphviz (WASM); luego cada nodo/arista se emite
// como celda mxGraph con coordenadas explícitas, de modo que el archivo se abre y se edita en draw.io.
import { Graphviz } from '@hpcc-js/wasm/graphviz';
import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';

export const OUT = process.env.OUT_DIR || path.resolve(import.meta.dirname, '..');
const gvp = Graphviz.load();

export const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const xa = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const r = (v) => Math.round(v * 10) / 10;

// ---------- Colores ----------
export const COL = {
  ui: ['#E8F0FE', '#4A78C2'],
  logic: ['#E6F2E8', '#4F8F5A'],
  ent: ['#FFF6E5', '#C9922E'],
  infra: ['#EEEEEE', '#777777'],
  rec: ['#F1EBFA', '#7D5BB0'],
  svc: ['#F5ECF8', '#8E5AA8'],
};

// ---------- XML mxGraph ----------
class Mx {
  constructor() { this.cells = []; this.n = 2; }
  id() { return 'c' + this.n++; }
  vertex(value, style, x, y, w, h, id) {
    id = id || this.id();
    this.cells.push(`<mxCell id="${id}" value="${xa(value)}" style="${xa(style)}" vertex="1" parent="1"><mxGeometry x="${r(x)}" y="${r(y)}" width="${r(w)}" height="${r(h)}" as="geometry"/></mxCell>`);
    return id;
  }
  edge(value, style, o = {}) {
    const id = this.id();
    const pts = (o.points || []).map((p) => `<mxPoint x="${r(p[0])}" y="${r(p[1])}"/>`).join('');
    const sp = o.from && !o.source ? `<mxPoint x="${r(o.from[0])}" y="${r(o.from[1])}" as="sourcePoint"/>` : '';
    const tp = o.to && !o.target ? `<mxPoint x="${r(o.to[0])}" y="${r(o.to[1])}" as="targetPoint"/>` : '';
    this.cells.push(`<mxCell id="${id}" value="${xa(value || '')}" style="${xa(style)}" edge="1" parent="1"${o.source ? ` source="${o.source}"` : ''}${o.target ? ` target="${o.target}"` : ''}><mxGeometry relative="1" as="geometry">${sp}${tp}${pts ? `<Array as="points">${pts}</Array>` : ''}</mxGeometry></mxCell>`);
    return id;
  }
  xml(name) {
    return `<mxfile host="app.diagrams.net"><diagram name="${xa(name)}" id="d1"><mxGraphModel dx="1000" dy="800" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="0" pageScale="1" math="0" shadow="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>${this.cells.join('')}</root></mxGraphModel></diagram></mxfile>`;
  }
}

export function save(relName, mx, title) {
  const p = path.join(OUT, relName + '.drawio');
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, mx.xml(title || path.basename(relName)));
}

// Exporta con draw.io (CLI) a PNG todos los .drawio de la carpeta de salida.
export function exportAll(dir = OUT) {
  const exe = process.env.DRAWIO || 'C:/Program Files/draw.io/draw.io.exe';
  const files = [];
  (function walk(d) { for (const f of fs.readdirSync(d, { withFileTypes: true })) { const q = path.join(d, f.name); if (f.isDirectory()) { if (f.name !== '_fuente') walk(q); } else if (f.name.endsWith('.drawio')) files.push(q); } })(dir);
  let ok = 0;
  for (const f of files) {
    const out = f.replace(/\.drawio$/, '.png');
    const res = spawnSync(exe, ['-x', '-f', 'png', '-s', '2', '-b', '20', '-o', out, f], { encoding: 'utf8', timeout: 120000 });
    if (res.status === 0 && fs.existsSync(out)) ok++; else console.log('FALLÓ', f, res.stderr || res.stdout);
  }
  console.log(`exportados ${ok}/${files.length}`);
}

// ---------- Clases (registro de nodos para convertirlos en celdas) ----------
const REG = new Map();
function wrapMember(m, max = 44) {
  const out = [];
  let rest = m;
  let first = true;
  while (rest.length > max) {
    let cut = rest.lastIndexOf(', ', max);
    if (cut < 12) cut = rest.lastIndexOf(' : ', max);
    if (cut < 12) break;
    out.push((first ? '' : '    ') + rest.slice(0, cut + (rest[cut] === ',' ? 1 : 0)));
    rest = rest.slice(cut + (rest[cut] === ',' ? 2 : 1));
    first = false;
  }
  out.push((first ? '' : '    ') + rest);
  return out;
}
const nb = (t) => esc(t).replace(/^ +/, (sp) => '&#160;'.repeat(sp.length));
const CP = 3;

export function cls(id, kind, st, name, members = []) {
  const [bg, bd] = COL[kind];
  const wrapped = members.map((m) => wrapMember(m));
  REG.set(id, { kind, st, name, wrapped });
  const rows = wrapped.map((ls) => `<TR><TD ALIGN="LEFT" BALIGN="LEFT">${ls.map(nb).join('<BR ALIGN="LEFT"/>')}</TD></TR>`).join('');
  const head = `<TR><TD BGCOLOR="${bg}"><FONT POINT-SIZE="9" COLOR="#555555">${st ? '«' + esc(st) + '»' : ' '}</FONT><BR/><B>${esc(name)}</B></TD></TR>`;
  return `${id} [shape=plain label=<<TABLE BORDER="1" COLOR="${bd}" CELLBORDER="0" CELLSPACING="0" CELLPADDING="${CP}" BGCOLOR="${bg}">${head}${rows ? `<HR/>${rows}` : ''}</TABLE>>];`;
}

// Tabla de DER (columnas con PK/FK)
export function tbl(id, name, rows, color = ['#FFF6E5', '#C9922E']) {
  REG.set(id, { er: true, name, rows, color });
  const [bg, bd] = color;
  return `${id} [shape=plain label=<<TABLE BORDER="1" COLOR="${bd}" CELLBORDER="0" CELLSPACING="0" CELLPADDING="${CP}" BGCOLOR="${bg}"><TR><TD BGCOLOR="#F5E2B8"><B>${esc(name)}</B></TD></TR><HR/>${rows.map((x) => `<TR><TD ALIGN="LEFT">${esc(x)}</TD></TR>`).join('')}</TABLE>>];`;
}

const HF = 'font-family:Arial;font-size:11px;';
function clsHtml(n) {
  if (n.er) {
    return `<div style="${HF}padding:3px 0;text-align:center;line-height:13px;background-color:#F5E2B8;"><b>${esc(n.name)}</b></div><hr style="margin:0;" size="1"/>` +
      n.rows.map((x) => `<div style="${HF}padding:3px 6px;line-height:13px;text-align:left;">${esc(x)}</div>`).join('');
  }
  const head = `<div style="${HF}padding:3px 0 0 0;text-align:center;line-height:11px;font-size:9px;color:#555555;">${n.st ? '«' + esc(n.st) + '»' : '&nbsp;'}</div><div style="${HF}padding:0 0 3px 0;text-align:center;line-height:13px;font-weight:bold;">${esc(n.name)}</div>`;
  const rows = n.wrapped.map((ls) => `<div style="${HF}padding:3px 6px;line-height:13px;text-align:left;">${ls.map(nb).join('<br/>')}</div>`).join('');
  return head + (rows ? `<hr style="margin:0;" size="1"/>` + rows : '');
}

function arrowStyle(a) {
  const dash = a.style && a.style.includes('dashed') ? 'dashed=1;' : '';
  const map = { open: 'endArrow=open;endFill=0;', empty: 'endArrow=block;endFill=0;', none: 'endArrow=none;', normal: 'endArrow=block;endFill=1;', crowodot: 'endArrow=ERzeroToMany;endFill=0;', teetee: 'endArrow=ERmandOne;endFill=0;', teeodot: 'endArrow=ERzeroToOne;endFill=0;', odiamond: 'endArrow=diamondThin;endFill=0;' };
  const smap = { empty: 'startArrow=block;startFill=0;', odiamond: 'startArrow=diamondThin;startFill=0;', crowodot: 'startArrow=ERzeroToMany;startFill=0;', teetee: 'startArrow=ERmandOne;startFill=0;', teeodot: 'startArrow=ERzeroToOne;startFill=0;', none: 'startArrow=none;' };
  const dir = a.dir || 'forward';
  const head = a.arrowhead || 'normal';
  const tail = a.arrowtail || 'none';
  let s = '';
  if (dir === 'forward') { s += map[head] || map.normal; s += 'startArrow=none;'; }
  else if (dir === 'back') { s += 'endArrow=none;'; s += smap[tail] || smap.none; }
  else { s += map[head] || map.normal; s += smap[tail] || smap.none; }
  return `html=1;rounded=0;curved=1;edgeStyle=none;strokeColor=#444444;fontSize=10;fontFamily=Arial;labelBackgroundColor=#FFFFFF;${dash}${s}`;
}

// ---------- Graphviz -> draw.io ----------
export async function dot(src, relName, extra = '') {
  const gv = await gvp;
  const full = `digraph G { graph [fontname="Arial" pad=0.3 ${extra}]; node [fontname="Arial" fontsize=11]; edge [fontname="Arial" fontsize=10 color="#444444" fontcolor="#333333"]; ${src} }`;
  const j = JSON.parse(gv.dot(full, 'json'));
  const [bx0, by0, , by1] = j.bb.split(',').map(Number);
  const H = by1 - by0;
  const X = (x) => x - bx0;
  const Y = (y) => H - (y - by0);
  const mx = new Mx();
  const gid = new Map();
  for (const o of j.objects || []) {
    if (o.bb && String(o.name).startsWith('cluster')) {
      const [a, b, c, d] = o.bb.split(',').map(Number);
      mx.vertex(o.label || '', `rounded=1;arcSize=3;whiteSpace=wrap;html=1;fillColor=none;strokeColor=${o.color || '#888888'};${o.style && o.style.includes('dashed') ? 'dashed=1;' : ''}verticalAlign=top;fontFamily=Arial;fontSize=12;`, X(a) - 8, Y(d) - 4, c - a + 16, d - b + 18);
    }
  }
  for (const o of j.objects || []) {
    if (String(o.name).startsWith('cluster') || o.pos === undefined) continue;
    const [px, py] = o.pos.split(',').map(Number);
    let w = Number(o.width) * 72, h = Number(o.height) * 72;
    const n = REG.get(o.name);
    let id;
    if (n) {
      const bd = n.er ? n.color[1] : COL[n.kind][1];
      const bg = n.er ? n.color[0] : COL[n.kind][0];
      w = w * 1.1 + 12;
      const nrows = (n.er ? n.rows.length : n.wrapped.length) + 1;
      const h2 = h + 10 + nrows * 0.5;
      id = mx.vertex(clsHtml(n), `whiteSpace=wrap;html=1;align=left;verticalAlign=top;overflow=hidden;spacing=0;fillColor=${bg};strokeColor=${bd};`, X(px) - w / 2, Y(py) - h / 2, w, h2);
    } else {
      const label = String(o.label || '').replace(/\\n/g, '<br/>').replace(/\n/g, '<br/>');
      w *= 1.1;
      id = mx.vertex(label, 'shape=note;whiteSpace=wrap;html=1;fillColor=#FFFBE6;strokeColor=#C9B24A;fontFamily=Arial;fontSize=11;size=12;', X(px) - w / 2, Y(py) - h / 2, w, h);
    }
    gid.set(o._gvid, id);
  }
  for (const e of j.edges || []) {
    if (e.style && String(e.style).includes("invis")) continue;
    const pts = [];
    const draw = (e._draw_ || []).find((d) => d.op === 'b');
    let from, to;
    if (draw) {
      const p = draw.points.map(([x, y]) => [X(x), Y(y)]);
      from = p[0]; to = p[p.length - 1];
      for (let i = 3; i < p.length - 1; i += 3) pts.push(p[i]);
    }
    const style = arrowStyle({ style: e.style, arrowhead: e.arrowhead, arrowtail: e.arrowtail, dir: e.dir });
    mx.edge(e.label || '', style, { source: gid.get(e.tail), target: gid.get(e.head), points: pts, from, to });
    for (const key of ['_tldraw_', '_hldraw_']) {
      const t = (e[key] || []).find((d) => d.op === 'T');
      if (t) mx.vertex(t.text, 'text;html=1;strokeColor=none;fillColor=none;fontFamily=Arial;fontSize=10;align=center;verticalAlign=middle;', X(t.pt[0]) - 16, Y(t.pt[1]) - 8, 32, 16);
    }
  }
  save(relName, mx);
}

// ---------- Secuencia ----------
const FS = 12;
const tw = (s) => s.length * 6.3 + 14;
const wrapT = (t, n = 42) => { const w = String(t).split(' '); const out = []; let cur = ''; for (const x of w) { if ((cur + ' ' + x).trim().length > n && cur) { out.push(cur); cur = x; } else cur = (cur + ' ' + x).trim(); } if (cur) out.push(cur); return out; };
const TXT = 'text;html=1;strokeColor=none;fillColor=none;fontFamily=Arial;whiteSpace=nowrap;';

export function seq(spec, relName) {
  const { title, parts, steps } = spec;
  const mx = new Mx();
  const idx = Object.fromEntries(parts.map((p, i) => [p.id, i]));
  const gap = Math.max(200, ...steps.flatMap(function f(s) { return s.alt ? s.alt.flatMap((o) => o.steps.flatMap(f)) : [s.text && s.from !== s.to ? Math.max(...wrapT(s.text).map((l) => l.length)) * 6.3 + 44 : 0]; }));
  const X0 = 110;
  const xs = parts.map((_, i) => X0 + i * gap);
  const W = X0 + (parts.length - 1) * gap + 260;
  let y = 158;
  const shapes = [];
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
          if (oi > 0) { dividers.push([y, o.guard]); y += 8; } else dividers.push([top, o.guard]);
          { const f0 = o.steps[0]; const nl = f0 && !f0.alt && f0.text ? wrapT(f0.text, f0.from === f0.to ? 44 : 42).length : 1; y += 34 + (f0 && f0.from !== f0.to ? (nl - 1) * 14 : 0); }
          run(o.steps, depth + 1);
          y += 6;
        });
        const bottom = y;
        shapes.push(() => {
          mx.vertex(st.kind || 'alt', 'shape=umlFrame;whiteSpace=wrap;html=1;pointerEvents=0;width=44;height=20;fillColor=none;strokeColor=#888888;fontFamily=Arial;fontSize=11;fontStyle=1;', x1, top, x2 - x1, bottom - top);
          dividers.forEach(([dy, g], i) => {
            if (i > 0) mx.edge('', 'endArrow=none;dashed=1;strokeColor=#888888;html=1;', { from: [x1, dy], to: [x2, dy] });
            mx.vertex(`[${esc(g)}]`, TXT + 'fontSize=11;align=left;', x1 + 50, dy + 3, tw(g) + 20, 18);
          });
        });
        y += 14;
        continue;
      }
      const a = xs[idx[st.from]], b = xs[idx[st.to]];
      const yy = y;
      const wl = wrapT(st.text, st.from === st.to ? 44 : 42);
      if (st.from === st.to) {
        shapes.push(() => {
          mx.edge('', `endArrow=block;endFill=1;html=1;edgeStyle=none;strokeColor=#222222;${st.ret ? 'dashed=1;' : ''}`, { from: [a, yy], to: [a, yy + 22], points: [[a + 42, yy], [a + 42, yy + 22]] });
          mx.vertex(wl.map(esc).join('<br/>'), TXT + `fontSize=${FS};align=left;`, a + 48, yy + 2, 280, 18 + (wl.length - 1) * 14);
        });
        y += 44 + (wl.length - 1) * 14;
      } else {
        shapes.push(() => {
          mx.edge(wl.map(esc).join('<br/>'), `html=1;edgeStyle=none;strokeColor=#222222;fontFamily=Arial;fontSize=${FS};verticalLabelPosition=top;verticalAlign=bottom;labelBackgroundColor=none;${st.ret ? 'endArrow=open;endFill=0;dashed=1;' : 'endArrow=block;endFill=1;'}`, { from: [a, yy], to: [b, yy] });
        });
        y += 40 + (wl.length - 1) * 14;
      }
    }
  }
  run(steps, 0);
  const H = y + 30;
  mx.vertex(esc(title), TXT + 'fontSize=17;fontStyle=1;fontColor=#1F3864;align=center;whiteSpace=wrap;', 20, 6, W - 140, 28);
  parts.forEach((p, i) => {
    const lines = p.label.split('\n');
    if (p.actor) {
      mx.vertex('', 'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;strokeColor=#222222;fillColor=none;', xs[i] - 15, 46, 30, 58);
      mx.vertex(lines.map(esc).join('<br/>'), TXT + 'fontSize=12;fontStyle=1;align=center;whiteSpace=wrap;', xs[i] - 70, 106, 140, 30);
      mx.edge('', 'endArrow=none;dashed=1;strokeColor=#999999;html=1;', { from: [xs[i], 138], to: [xs[i], H - 20] });
    } else {
      const w = Math.max(120, tw(p.label.replace(/\n/g, '')));
      const h = 24 + lines.length * 16;
      mx.vertex(lines.map(esc).join('<br/>'), 'rounded=0;whiteSpace=wrap;html=1;fillColor=#F2F2F2;strokeColor=#555555;fontFamily=Arial;fontSize=12;fontStyle=1;', xs[i] - w / 2, 46, w, h);
      mx.edge('', 'endArrow=none;dashed=1;strokeColor=#999999;html=1;', { from: [xs[i], 46 + h], to: [xs[i], H - 20] });
    }
  });
  shapes.forEach((f) => f());
  save(relName, mx, title);
}

// ---------- Gráfico de caso de uso ----------
export function ucg(spec, relName) {
  const { title, left = [], right = [], main, rel = [] } = spec;
  const mx = new Mx();
  const n = rel.length;
  const W = Math.max(900, 260 * n + 300);
  const cy = 100 + (Math.max(left.length, right.length, 1) - 1) * 60 + 20;
  const rx = Math.max(150, tw(main) / 2 + 10), ry = 40, cx = W / 2;
  const relY = cy + 175;
  mx.vertex(esc(title), TXT + 'fontSize=17;fontStyle=1;fontColor=#1F3864;align=center;', 20, 6, W - 40, 28);
  const mainId = mx.vertex(esc(main), 'ellipse;whiteSpace=wrap;html=1;fillColor=#FFF6E5;strokeColor=#C9922E;strokeWidth=1.5;fontFamily=Arial;fontSize=13;', cx - rx, cy - ry, rx * 2, ry * 2);
  const place = (list, x) => {
    const step = 120;
    const y0 = cy - ((list.length - 1) * step) / 2;
    list.forEach((a, i) => {
      const y = y0 + i * step;
      const id = mx.vertex(esc(a), 'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;strokeColor=#222222;fillColor=none;fontFamily=Arial;fontSize=12;', x - 15, y - 35, 30, 60);
      mx.edge('', 'endArrow=none;html=1;strokeColor=#444444;', { source: id, target: mainId });
    });
  };
  place(left, 110);
  place(right, W - 110);
  rel.forEach((q, i) => {
    const x = (W / (n + 1)) * (i + 1);
    const rrx = Math.max(110, tw(q.text) / 2 + 8), rry = 30;
    const id = mx.vertex(esc(q.text), `ellipse;whiteSpace=wrap;html=1;fillColor=${q.fill || '#F1EBFA'};strokeColor=#7D5BB0;strokeWidth=1.3;fontFamily=Arial;fontSize=12;`, x - rrx, relY - rry, rrx * 2, rry * 2);
    const lab = esc(`«${q.rel}»${q.cond ? ' ' + q.cond : ''}`);
    const st = 'html=1;dashed=1;endArrow=open;endFill=0;strokeColor=#444444;fontFamily=Arial;fontSize=11;labelBackgroundColor=#FFFFFF;';
    if (q.rel === 'include') mx.edge(lab, st, { source: mainId, target: id });
    else mx.edge(lab, st, { source: id, target: mainId });
  });
  save(relName, mx, title);
}
