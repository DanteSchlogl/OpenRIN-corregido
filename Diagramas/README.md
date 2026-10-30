# Diagramas corregidos – OpenRIN

Diagramas redibujados a partir del código fuente, que es la referencia de las reglas de negocio. Están hechos en **draw.io**: cada uno es un archivo `.drawio` editable (se abre en <https://app.diagrams.net> o en draw.io Desktop) y su exportación en **PNG**, que se inserta en la Carpeta de Proyecto.

## Contenido

- `UC-01` … `UC-13`: gráfico del caso de uso, clases afectadas y diagrama de secuencia de cada uno.
- `Arquitectura/`:
  - Clases de dominio, lógica de negocio, presentación, acceso a datos y servicios (seguridad y localización).
  - DER hoja B (operación y sistema).
- `Procesos/`: diagramas de actividad P01 a P05.
- `Tecnicos/`: secuencias T02 (integridad al iniciar e inicio de sesión).

Todos llevan el rótulo institucional (materia, docente, alumno, legajo, comisión, sistema, fecha y nombre del diagrama).

## Cambios respecto de los diagramas del PDF

- Sin líneas que atraviesan cajas ni etiquetas pisadas; texto de los métodos completo.
- Los fragmentos `alt` de las secuencias abarcan los mensajes que corresponden y tienen su rama `else`.
- Niveles de criticidad: Baja / Media / Alta (en lugar de Óptimo / Subóptimo / Deficiente / Crítico).
- UC-08: la tendencia peligrosa reclasifica la medición a Alta y genera una alerta `TendenciaPeligrosa`.
- UC-07: la escalada al médico ocurre tras 3 intentos de contacto fallidos (`SeguimientoLogic`).
- UC-04 y UC-05: `MedicionRINLogic` lee el contexto directamente; no usa `HistoriaClinicaLogic`.
- UC-04: UC-05 es `include`; UC-07 y UC-08 son `extend`.
- Presentación: se dibujan las relaciones reales (qué formulario abre a cuál) tomadas del código.

## No incluidos (sin cambios en el PDF)

Clases de infraestructura, DER hojas A y C, diagrama de componentes y mapa de navegación (T01) y las secuencias T03 a T08.

## Cómo editarlos

Abrir el `.drawio`, modificarlo y exportar a PNG (Archivo → Exportar como → PNG, escala 2). Luego reemplazar la imagen en la Carpeta.

## Regenerarlos desde la fuente

`_fuente/` genera los `.drawio` y los exporta con draw.io Desktop (instalado en `C:\Program Files\draw.io`). Se necesita Node.js 18 o superior.

```bash
cd Diagramas/_fuente
npm install
node uc.mjs && node arch.mjs && node arch2.mjs && node extra.mjs && node proc.mjs   # genera los .drawio
node export.mjs                                  # exporta los PNG con draw.io
node insert.mjs "Carpeta de Proyecto - OpenRIN.pdf"   # inserta los diagramas en una copia del PDF
```

`fix.mjs` corrige en el PDF los textos con errores (línea por línea). `insert.mjs` reemplaza, en una copia del PDF, la zona de cada diagrama (52 páginas) por la imagen nueva, y conserva cabecera, pie de figura y numeración de las 150 páginas.
