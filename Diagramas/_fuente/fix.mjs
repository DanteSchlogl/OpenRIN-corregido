import * as mupdf from 'mupdf';
import fs from 'fs';
const IN = process.argv[2], OUT = process.argv[3];
const doc = mupdf.Document.openDocument(fs.readFileSync(IN), 'application/pdf');
const pdf = doc.asPDF();
const font = new mupdf.Font('Helvetica');
const fontRef = pdf.addSimpleFont(font, 'Latin');
const fbold = new mupdf.Font('Helvetica-Bold');
const SIZE = 12;

// ediciones: {p: página (1-based), q: texto a buscar (palabra/segmento), r: reemplazo, n: ocurrencia}
const word = (p, q, r, n = 0) => ({ p, q, r, n });
// ediciones de línea: se redacta el renglón y se reescribe  {p, y, x, w, t}
const line = (p, y, x, w, t) => ({ p, y, x, w, t });
const E = [
  line(56, 275, 121, 420, 'A3  Valor fuera del rango biológico: si el valor no se encuentra entre 0,1 y '),
  line(56, 290, 121, 417, '20,0, el sistema rechaza el registro con un mensaje de error y no persiste nada.'),
  line(56, 257, 121, 398, 'marca la medición y genera una alerta de tendencia (extensión hacia UC-08).'),
  line(64, 330, 121, 421, 'La medición queda clasificada con su nivel de criticidad (Baja, Media, '),
  line(64, 345, 121, 103, 'o Alta).'),
  line(79, 188, 85, 311, 'cuando una alerta acumula 3 intentos de contacto fallidos.'),
  line(79, 586, 121, 420, 'A1  Resolución a tiempo: la alerta se resuelve (UC-06 del médico o '),
  line(86, 203, 85, 219, 'marcarla, reclasificarla a Alta y alertar.'),
  line(86, 283, 121, 132, 'una medición previa).'),
  line(86, 571, 121, 430, '4. El sistema reclasifica la medición a criticidad Alta y genera la '),
  line(86, 586, 121, 35, 'alerta de tendencia peligrosa.'),
  line(121, 299, 121, 231, 'El historial queda consultado en pantalla.'),
  line(48, 588, 121, 421, '4. Si la gravedad es Moderada o Grave, el evento queda destacado en la historia '),
  line(48, 668, 121, 420, 'A1  Gravedad Moderada o Grave: el evento se destaca en la historia y en las vistas del '),
  line(128, 266, 85, 456, 'El sistema se organiza en cinco capas con dependencias unidireccionales: la '),
  line(132, 281, 85, 457, '11 tablas del negocio recalculando los dígitos verificadores (DVH por fila y DVV por '),
  line(146, 470, 85, 456, 'de las 11 tablas, incluso las vacías, para que el mecanismo jamás quede ')
];

function hitRects(page, q, n) {
  const h = page.search(q)[n];
  const x0 = Math.min(...h.map((z) => Math.min(z[0], z[4]))), x1 = Math.max(...h.map((z) => Math.max(z[2], z[6])));
  const y0 = Math.min(...h.map((z) => z[1])), y1 = Math.max(...h.map((z) => z[7]));
  return [[x0, y0, x1, y1]];
}
function pdfEsc(t){ let r=''; for (const ch of t) r += (ch===String.fromCharCode(92)||ch==='('||ch===')') ? String.fromCharCode(92)+ch : ch; return r; }
function strW(text, size) { let w = 0; for (const ch of text) w += font.advanceGlyph(font.encodeCharacter(ch.codePointAt(0))) * size; return w; }

const byPage = new Map();
for (const e of E) { if (!byPage.has(e.p)) byPage.set(e.p, []); byPage.get(e.p).push(e); }
let done = 0;
for (const [p, list] of byPage) {
  const page = pdf.loadPage(p - 1);
  const ops = [];
  for (const e of list) {
    let rect, text;
    if (e.t !== undefined) { rect = [e.x - 1, e.y + 1.5, e.x + e.w + 3, e.y + 14]; text = e.t; }
    else { const r = hitRects(page, e.q, e.n)[0]; rect = [r[0] - 0.5, r[1] + 1, r[2] + 0.5, r[3] - 0.5]; text = e.r; }
    ops.push({ rect, text, line: e.t !== undefined });
  }
  for (const o of ops) { const a = page.createAnnotation('Redact'); a.setRect(o.rect); }
  page.applyRedactions(false, 0);
  const PH = page.getBounds()[3];
  const csParts = [];
  for (const o of ops) {
    let size = SIZE; const maxW = o.line ? Math.max(o.rect[2] - o.rect[0], strW(o.text, SIZE) * 0 + 1) : o.rect[2] - o.rect[0] + 6;
    const natural = strW(o.text, SIZE);
    const limit = o.line ? 541 - o.rect[0] : maxW;
    if (natural > limit) size = Math.max(8, SIZE * limit / natural);
    const x = o.rect[0] + 1, y = PH - (o.rect[1] + 10.2);
    const esc = pdfEsc(o.text);
    csParts.push(Buffer.from(`q 0 0 0 rg BT /FixF ${size.toFixed(2)} Tf ${x.toFixed(2)} ${y.toFixed(2)} Td (${esc}) Tj ET Q
`, "latin1"));
    done++;
  }
  const pobj = page.getObject();
  let res = pobj.get('Resources'); if (res.isNull()) { res = pdf.newDictionary(); pobj.put('Resources', res); }
  let fd = res.get('Font'); if (fd.isNull()) { fd = pdf.newDictionary(); res.put('Font', fd); }
  fd.put('FixF', fontRef);
  const stream = pdf.addStream(Buffer.concat(csParts), pdf.newDictionary());
  const c = pobj.get('Contents');
  if (c.isArray()) c.push(stream); else { const arr = pdf.newArray(); arr.push(c); arr.push(stream); pobj.put('Contents', arr); }
}
fs.writeFileSync(OUT, pdf.saveToBuffer('compress,garbage').asUint8Array());
console.log('ediciones:', done, '->', OUT);
