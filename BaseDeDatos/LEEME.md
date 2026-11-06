# Base de datos

OpenRIN usa dos bases de datos en SQL Server 2019 o superior:

- `OpenRIN_Services`: usuarios, permisos, idiomas, textos corregidos, bitácora y registro de respaldos (7 tablas).
- `OpenRIN_Negocio`: pacientes, historias clínicas, mediciones de RIN, alertas, turnos, seguimientos, eventos adversos, reportes, dígitos verificadores y auditoría de cambios (13 tablas, 16 claves foráneas).

## Crear las bases con los scripts

```
sqlcmd -S .\SQLEXPRESS -E -Q "CREATE DATABASE OpenRIN_Services; CREATE DATABASE OpenRIN_Negocio;"
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_01_bitacora.sql
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_02_seguridad.sql
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_03_permisos.sql
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_04_localizacion.sql
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_05_localizacion_limpiar.sql
sqlcmd -S .\SQLEXPRESS -E -d OpenRIN_Services -f 65001 -i services_06_respaldos.sql
sqlcmd -S .\SQLEXPRESS -E -I -d OpenRIN_Negocio -f 65001 -i negocio_esquema.sql
```

`-f 65001` es obligatorio (acentos) y `-I` es obligatorio para el script de negocio. Todos los scripts son idempotentes.

Los textos de los idiomas español, inglés y chino ya no están en la base: viajan dentro del programa en archivos `.resx` (ensamblados satélite, una carpeta por cultura). La tabla `Textos` solo guarda las leyendas que el administrador corrige o agrega. `services_05_localizacion_limpiar.sql` retira, una sola vez, la semilla de textos de versiones anteriores.

## Restaurar los datos de demostración

`OpenRIN_Negocio_demo.bak` y `OpenRIN_Services_demo.bak` son respaldos completos de SQL Server 2022 con los usuarios y los cinco pacientes de demostración. Cópielos a una carpeta que lea el servicio de SQL Server (por ejemplo `C:\OpenRIN\Backups`) y restaure:

```
RESTORE DATABASE OpenRIN_Negocio  FROM DISK = 'C:\OpenRIN\Backups\OpenRIN_Negocio_demo.bak'  WITH REPLACE;
RESTORE DATABASE OpenRIN_Services FROM DISK = 'C:\OpenRIN\Backups\OpenRIN_Services_demo.bak' WITH REPLACE;
```

Los respaldos se restauran en SQL Server 2022 o superior. Más detalle en el Manual de instalación, sección 6.
