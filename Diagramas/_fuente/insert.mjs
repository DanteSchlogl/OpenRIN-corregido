import * as mupdf from 'mupdf';
import fs from 'fs';

const SRC = '../../Documentacion/Carpeta de Proyecto - OpenRIN.pdf';
const OUTPDF = process.argv[2] || 'Carpeta de Proyecto - OpenRIN (diagramas corregidos).pdf';
const D = '..';
const only = process.argv.slice(3).map(Number);

const map = [
  [12, 'Arquitectura/G06 - Dominio (clases)'],
  [13, 'Arquitectura/G06 - Logica de negocio (clases)'],
  [15, 'Arquitectura/G06 - Presentacion (clases)'],
  [16, 'Arquitectura/G06 - Servicios seguridad y localizacion (clases)'],
  [20, 'Arquitectura/G07 - DER hoja B (operacion y sistema)'],
];
const ucPages = { '01': [35, 36, 37], '02': [42, 43, 44], '03': [49, 50, 51], '04': [57, 58, 59], '05': [66, 67, 68], '06': [73, 74, 75], '07': [80, 81, 82], '08': [87, 88, 89], '09': [94, 95, 96], '10': [101, 102, 103], '11': [108, 109, 110], '12': [115, 116, 117], '13': [122, 123, 124] };
for (const [n, pg] of Object.entries(ucPages)) {
  map.push([pg[0], `UC-${n}/UC-${n} - Gráfico`], [pg[1], `UC-${n}/UC-${n} - Clases afectadas`], [pg[2], `UC-${n}/UC-${n} - Secuencia`]);
}

const doc = mupdf.Document.openDocument(fs.readFileSync(SRC), 'application/pdf');
const pdf = doc.asPDF();
// Zona del diagrama (en puntos, origen arriba-izquierda): debajo de la cabecera, encima del pie de figura
const AREA = { x0: 34, y0: 65, x1: 808, y1: 516 };
const BOX = { x0: 40, y0: 76, x1: 802, y1: 510 };
let done = 0;
for (const [pn, name] of map) {
  if (only.length && !only.includes(pn)) continue;
  const png = fs.readFileSync(`${D}/${name}.png`);
  const iw = png.readUInt32BE(16), ih = png.readUInt32BE(20);
  const page = pdf.loadPage(pn - 1);
  const [pl, pt, pr, pb] = page.getBounds();
  const PH = pb - pt;
  const bw = BOX.x1 - BOX.x0, bh = BOX.y1 - BOX.y0;
  const sc = Math.min(bw / iw, bh / ih);
  const w = iw * sc, h = ih * sc;
  const x = BOX.x0 + (bw - w) / 2, y = BOX.y0 + (bh - h) / 2;
  const img = new mupdf.Image(png);
  const imgRef = pdf.addImage(img);
  const pobj = page.getObject();
  let res = pobj.get('Resources');
  if (!res || res.isNull()) { res = pdf.newDictionary(); pobj.put('Resources', res); }
  let xo = res.get('XObject');
  if (!xo || xo.isNull()) { xo = pdf.newDictionary(); res.put('XObject', xo); }
  const key = 'OrDiag' + pn;
  xo.put(key, imgRef);
  const cs = `q 1 1 1 rg ${AREA.x0} ${PH - AREA.y1} ${AREA.x1 - AREA.x0} ${AREA.y1 - AREA.y0} re f Q q ${w.toFixed(2)} 0 0 ${h.toFixed(2)} ${x.toFixed(2)} ${(PH - y - h).toFixed(2)} cm /${key} Do Q`;
  const stream = pdf.addStream(cs, pdf.newDictionary());
  const c = pobj.get('Contents');
  if (c.isArray()) c.push(stream);
  else { const arr = pdf.newArray(); arr.push(c); arr.push(stream); pobj.put('Contents', arr); }
  done++;
}
fs.writeFileSync(OUTPDF, pdf.saveToBuffer('compress,garbage').asUint8Array());
console.log('páginas reemplazadas:', done, '->', OUTPDF, (fs.statSync(OUTPDF).size / 1e6).toFixed(1) + ' MB');
