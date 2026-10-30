// Genera los DER (hojas A, B y C y los parciales de cada caso de uso) LEYENDO EL ESQUEMA REAL de las bases de datos.
// Así el dibujo no puede diferir de lo que existe: columnas, tipos, nulos, claves primarias y foráneas e índices únicos.
// Uso:  OUT_DIR=<carpeta Diagramas> node der.mjs [servidor]      (por defecto .\SQLEXPRESS, autenticación de Windows)
import { spawnSync } from 'child_process';
import { dot, tbl } from './lib.mjs';

const SERVIDOR = process.argv[2] || '.\\SQLEXPRESS';

function consultar(baseDatos, sql) {
  const r = spawnSync('sqlcmd', ['-S', SERVIDOR, '-E', '-C', '-d', baseDatos, '-y', '0', '-Q', 'SET NOCOUNT ON; ' + sql], { encoding: 'utf8', maxBuffer: 1 << 26 });
  if (r.status !== 0) throw new Error('sqlcmd falló: ' + r.stdout + r.stderr);
  return JSON.parse(r.stdout.replace(/\r?\n/g, ''));
}

const SQL_COLS = `SELECT t.name AS tabla, c.name AS columna, c.column_id AS n,
  CASE WHEN TYPE_NAME(c.user_type_id) IN ('nvarchar','varchar','char','nchar') THEN TYPE_NAME(c.user_type_id) + '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CAST(CASE WHEN TYPE_NAME(c.user_type_id) IN ('nvarchar','nchar') THEN c.max_length/2 ELSE c.max_length END AS varchar(10)) END + ')'
       WHEN TYPE_NAME(c.user_type_id)='decimal' THEN 'decimal(' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')' ELSE TYPE_NAME(c.user_type_id) END AS tipo,
  c.is_nullable AS nulo,
  CASE WHEN EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id=ic.object_id AND i.index_id=ic.index_id WHERE i.is_primary_key=1 AND ic.object_id=c.object_id AND ic.column_id=c.column_id) THEN 1 ELSE 0 END AS pk,
  CASE WHEN EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id=ic.object_id AND i.index_id=ic.index_id WHERE i.is_unique=1 AND i.is_primary_key=0 AND ic.object_id=c.object_id AND ic.column_id=c.column_id AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id=i.object_id AND x.index_id=i.index_id AND x.key_ordinal>0)=1) THEN 1 ELSE 0 END AS uq
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE t.name <> '__EFMigrationsHistory' ORDER BY t.name, c.column_id FOR JSON PATH`;
const SQL_FK = `SELECT OBJECT_NAME(fk.parent_object_id) AS hija, COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS col, OBJECT_NAME(fk.referenced_object_id) AS padre, COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS colpadre FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id ORDER BY 1,2 FOR JSON PATH`;

function leerEsquema(bd) {
  const cols = consultar(bd, SQL_COLS);
  const fks = consultar(bd, SQL_FK);
  const tablas = new Map();
  for (const c of cols) { if (!tablas.has(c.tabla)) tablas.set(c.tabla, []); tablas.get(c.tabla).push(c); }
  return { tablas, fks };
}

// Etiquetas de las relaciones (semántica de cada clave foránea)
const ETIQ = {
  'Pacientes.IdObraSocial': 'cobertura', 'Pacientes.IdUsuarioPortal': 'acceso al portal', 'HistoriasClinicas.IdPaciente': 'historia clínica',
  'HistoriasClinicas.IdDiagnostico': 'indicación', 'MedicionesRIN.IdPaciente': 'mediciones', 'MedicionesRIN.IdUsuarioRegistro': 'carga',
  'Alertas.IdPaciente': 'alertas', 'Alertas.IdMedicion': 'origina', 'Alertas.IdUsuarioResolucion': 'resuelve',
  'Turnos.IdPaciente': 'turnos', 'Turnos.IdUsuarioMedico': 'médico asignado', 'Seguimientos.IdPaciente': 'seguimientos',
  'Seguimientos.IdUsuarioRegistro': 'contacta', 'Seguimientos.IdUsuarioDecision': 'decide', 'EventosAdversos.IdPaciente': 'eventos',
  'EventosAdversos.IdUsuarioRegistro': 'registra', 'Permisos.CodigoPadre': 'agrupa (padre)', 'Textos.CodigoIdioma': 'leyendas',
};

function der(esq, nombres, relPath, extra = '') {
  const set = new Set(nombres);
  let nodos = '';
  for (const n of nombres) {
    const cols = esq.tablas.get(n);
    if (!cols) throw new Error('No existe la tabla ' + n);
    const fkCols = new Set(esq.fks.filter((f) => f.hija === n).map((f) => f.col));
    const filas = cols.map((c) => `${c.pk ? 'PK ' : fkCols.has(c.columna) ? 'FK ' : c.uq ? 'UQ ' : '   '}${c.columna} : ${c.tipo}${c.nulo ? '  (nulo)' : ''}`.replace(/^ {3}/, '    '));
    nodos += tbl('t_' + n, n, filas) + '\n';
  }
  let aristas = '';
  for (const f of esq.fks) {
    if (!set.has(f.hija) || !set.has(f.padre)) continue;
    const col = esq.tablas.get(f.hija).find((c) => c.columna === f.col);
    const padreEnd = col.nulo ? 'teeodot' : 'teetee';
    const hijaEnd = col.uq ? 'teeodot' : 'crowodot';
    const etiq = ETIQ[`${f.hija}.${f.col}`] || f.col;
    aristas += `t_${f.padre} -> t_${f.hija} [dir=both arrowtail=${padreEnd} arrowhead=${hijaEnd} label="${etiq}"];\n`;
  }
  return dot(`rankdir=LR; nodesep=0.7; ranksep=1.7; ${nodos} ${aristas} ${extra}`, relPath);
}

// ------------------------------------------------------------------ hojas del DER
const neg = leerEsquema('OpenRIN_Negocio');
const srv = leerEsquema('OpenRIN_Services');
console.log('Esquema leído: negocio', neg.tablas.size, 'tablas /', neg.fks.length, 'FK; servicios', srv.tablas.size, 'tablas /', srv.fks.length, 'FK');

await der(neg, ['ObrasSociales', 'Pacientes', 'Usuarios', 'HistoriasClinicas', 'Diagnosticos', 'MedicionesRIN', 'Alertas'], 'Arquitectura/G07 - DER hoja A (nucleo clinico)');
await der(neg, ['Pacientes', 'Usuarios', 'Turnos', 'Seguimientos', 'EventosAdversos', 'ReportesEstadisticos', 'DigitosVerificadores', 'AuditoriaCambios'], 'Arquitectura/G07 - DER hoja B (operacion y sistema)');
await der(srv, ['Usuarios', 'Idiomas', 'Textos', 'Permisos', 'PerfilesPermisos', 'Logs'], 'Arquitectura/G07 - DER hoja C (servicios)',
  "t_Usuarios -> t_PerfilesPermisos [dir=none style=dashed label=\"perfil (ref. lógica, sin FK)\"]; t_Permisos -> t_PerfilesPermisos [dir=none style=dashed label=\"permiso (ref. lógica, sin FK)\"]; t_Idiomas -> t_Usuarios [dir=none style=dashed label=\"idioma preferido (ref. lógica, sin FK)\"]; t_Usuarios -> t_Logs [style=invis];");

// ------------------------------------------------------------------ DER parciales de los casos de uso
const UC = {
  '01': ['Pacientes', 'ObrasSociales', 'Usuarios'],
  '02': ['Pacientes', 'HistoriasClinicas', 'Diagnosticos'],
  '03': ['Pacientes', 'EventosAdversos', 'Usuarios'],
  '04': ['Pacientes', 'HistoriasClinicas', 'MedicionesRIN', 'Alertas'],
  '05': ['Pacientes', 'HistoriasClinicas', 'MedicionesRIN'],
  '06': ['Pacientes', 'MedicionesRIN', 'Alertas'],
  '07': ['Pacientes', 'MedicionesRIN', 'Alertas', 'Usuarios'],
  '08': ['Pacientes', 'HistoriasClinicas', 'MedicionesRIN', 'Alertas'],
  '09': ['Pacientes', 'Turnos', 'Usuarios', 'Alertas'],
  '10': ['Pacientes', 'HistoriasClinicas', 'Seguimientos', 'Alertas', 'Usuarios'],
  '11': ['Pacientes', 'MedicionesRIN', 'ReportesEstadisticos'],
  '12': ['Pacientes', 'HistoriasClinicas', 'Diagnosticos', 'MedicionesRIN'],
  '13': ['Pacientes', 'MedicionesRIN', 'Alertas', 'Seguimientos', 'EventosAdversos'],
};
for (const [n, tablas] of Object.entries(UC)) await der(neg, tablas, `UC-${n}/UC-${n} - DER parcial`);
console.log('DER generados');
