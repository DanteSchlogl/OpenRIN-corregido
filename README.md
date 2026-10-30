# OpenRIN — Plataforma Integral de Alertas y Monitoreo para Pacientes Anticoagulados

**Autor:** Carlos Santiago Muñiz Cortés
**Profesor:** Gastón Matías Weingand
**Institución:** Colegio Leonardo Da Vinci
**Materia:** Prácticas Profesionalizantes III · **Año:** 2026

[Español] · [English](README.en.md) · [中文](README.zh-CN.md)

## Descripción

OpenRIN es una plataforma de escritorio para consultorios hematológicos, orientada al seguimiento de pacientes anticoagulados: centraliza la gestión de pacientes e historias clínicas, la recepción de mediciones de RIN, la clasificación automática de criticidad con generación de alertas, la agenda de turnos con reasignación inteligente, el seguimiento clínico con protocolo de contacto y escalada, los reportes estadísticos de eficacia, el portal del paciente y las herramientas de administración y seguridad del sistema.

La solución está construida en **C# / .NET 8 (Windows Forms)** sobre **SQL Server**, con arquitectura multicapa, y cubre de punta a punta el circuito del consultorio: paciente → control de RIN → alerta → turno → seguimiento → reporte.

## Novedades de esta versión (v1.0)

- **Integridad proactiva**: dígitos verificadores por fila y por tabla (DVH/DVV), firmados en el punto único de guardado y **verificados al iniciar la aplicación, antes del inicio de sesión**.
- **Permisos con patrón Composite**: árbol de permisos atómicos y compuestos con códigos únicos, editable desde el sistema (marcar/desmarcar grupos) y aplicado al gating de módulos.
- **Internacionalización dinámica**: español (Latinoamérica), inglés y chino simplificado, con los idiomas y sus leyendas **en la base de datos** (sin hojas de recursos en tiempo de ejecución); cambio de idioma en caliente mediante patrón observer.
- **Control de cambios con restauración**: historial de auditoría con estado anterior/nuevo y la acción *Restaurar estado anterior* (recompone y vuelve a firmar).
- **PDF con librería de terceros** (QuestPDF, licencia comunitaria) para reportes e historiales, junto con exportación a **Excel** y **JSON**.
- **Respaldos automáticos**: respaldo diario de ambas bases (03:00) con verificación de integridad de cada copia.
- **Carpeta de Proyecto completa** (documentación entregable, ~150 páginas) en [`Documentacion/`](Documentacion/).

## Estructura del repositorio

- `Codigo/` — solución .NET: `Negocio.UI` (Windows Forms), `Negocio.BLL`, `Negocio.DAL`, `Negocio.DomainModel` y `Services` (DomainModel, BLL, DAL, Facade), más las pruebas de humo (`PPDD/smoke` fuera del árbol de la solución).
- `Documentacion/` — **Carpeta de Proyecto (PDF)**: descripción global, diagramas de clases y de datos, procesos y casos de uso, y aspectos técnicos.
- `Views/` — entregables de análisis y diseño (histórico).
- `Propuesta Corregida/` — propuesta de proyecto (TP 1).
- `Diagramas/` — diagramas corregidos (draw.io y PNG) y su fuente para regenerarlos.
- `ops/` — scripts del respaldo automático diario (03:00): SQL, tarea programada y LEEME.

## Arquitectura

- **Capas**: Presentación (WinForms) → Lógica de Negocio (BLL) y Fachada de Servicios → Acceso a Datos (DAL) → Bases de datos.
- **Dos vías de persistencia**: **Entity Framework Core** para el dominio del negocio (contexto con configuración Fluent y punto único de guardado) y **ADO.NET** para la base de servicios (seguridad, idiomas, permisos, bitácora).
- **Bases**: `OpenRIN_Negocio` (13 tablas con firmas de integridad y auditoría de cambios) y `OpenRIN_Services` (seguridad, idiomas, permisos y bitácora).
- **Patrones**: Singleton (sesión), Composite (permisos), Observer (idiomas), Repository (pacientes) y Facade (servicios técnicos).

## Requisitos

- Windows 10/11.
- .NET 8 (Desktop).
- SQL Server 2019 o superior (la edición Express es suficiente).

> Las credenciales y configuraciones locales **no se versionan** en este repositorio.

## Ejecución

1. Crear las bases `OpenRIN_Negocio` y `OpenRIN_Services` y aplicar los scripts de estructura y datos iniciales (carpeta `Codigo/`).
2. Configurar la cadena de conexión local (archivo de configuración no versionado).
3. Compilar y ejecutar: `dotnet build` y luego el proyecto `Negocio.UI`.

Al iniciar, la aplicación verifica la integridad de la base **antes** de mostrar el inicio de sesión.

## Documentación completa

La **[Carpeta de Proyecto](Documentacion/)** contiene el documento entregable completo: descripción global del producto, diagramas de clases por capa, modelo de datos, especificación de los cinco procesos de negocio y de los **trece casos de uso** (con secuencias, clases afectadas y pantallas reales del sistema), y los aspectos técnicos (arquitectura, sesión, encriptación, perfiles, multiidioma, bitácora, control de cambios, respaldos e integridad).

## Estado del proyecto

Entregable de Noviembre 2026. Próximos pasos previstos: instalador, manuales de usuario y ayuda en línea.
