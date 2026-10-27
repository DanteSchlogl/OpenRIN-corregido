# Diagramas corregidos – OpenRIN

Diagramas redibujados a partir del código fuente, que es la referencia de las reglas de negocio. Cada uno está en **PNG** (para insertar en la Carpeta) y en **SVG** (vectorial).

## Contenido

- `UC-01` … `UC-13`: gráfico del caso de uso, clases afectadas y diagrama de secuencia de cada uno.
- `Arquitectura/`:
  - Clases de dominio, de lógica de negocio, de presentación y de servicios (seguridad y localización).
  - DER hoja B (operación y sistema).

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

Clases de acceso a datos e infraestructura, DER hojas A y C, diagramas de actividad P01–P05, T01 y las secuencias T02–T08.

## Regenerarlos

Los diagramas se generan con Graphviz (WASM) y resvg desde `_fuente/`. Se necesita Node.js 18 o superior.

```bash
cd Diagramas/_fuente
npm install
node uc.mjs && node arch.mjs && node arch2.mjs
```

Para cambiar un diagrama se edita el dato correspondiente en `uc.mjs`, `arch.mjs` o `arch2.mjs` y se vuelve a ejecutar.
