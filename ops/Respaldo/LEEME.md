# Respaldo nocturno de OpenRIN — Programador de tareas de Windows

Mecanismo recurrente de respaldo para puesta en producción: una tarea diaria del
**Programador de tareas de Windows** ejecuta el respaldo FULL de `OpenRIN_Services` y
`OpenRIN_Negocio` (con verificación `RESTORE VERIFYONLY`) todos los días a las **03:00**.
No depende de que ninguna aplicación esté abierta ni de Hermes: es el mecanismo estándar
de Windows y es el que se documenta para el consultorio.

## Archivos

| Archivo | Rol |
|---|---|
| `01_respaldo_nocturno.sql` | Respaldo FULL de ambas bases + verificación, con marca de tiempo en el nombre. |
| `ejecutar_respaldo.ps1` | Wrapper: ejecuta el SQL con `sqlcmd -E` (autenticación de Windows) y escribe un log. |
| `registrar_tarea.ps1` | Registra/re-registra la tarea diaria (idempotente) en el Programador de tareas. |
| `LEEME.md` | Este documento. |

## Instalación

```powershell
# Desde esta carpeta (una sola vez, o cada vez que se actualice la configuracion):
powershell -NoProfile -ExecutionPolicy Bypass -File .\registrar_tarea.ps1
```

La tarea queda como **"OpenRIN - Respaldo Nocturno"**, diaria a las 03:00, ejecutándose
como el usuario actual (**token interactivo, sin contraseñas almacenadas**): se dispara
siempre que el usuario tenga sesión iniciada (aunque esté bloqueada). Al canjear por una
cuenta de servicio en producción, usar `schtasks /Create ... /RU <cuenta> /RP` o
`Register-ScheduledTask` con las credenciales correspondientes.

## Verificación manual

```powershell
schtasks /Run /TN "OpenRIN - Respaldo Nocturno"   # dispara ahora
schtasks /Query /TN "OpenRIN - Respaldo Nocturno" /V /FO LIST   # ver Ultimo resultado
```

- Respaldos: `C:\OpenRIN\Backups\OpenRIN_<Base>_<fecha>_<hora>_FULL.bak`
- Logs: `C:\OpenRIN\Backups\logs\respaldo_<fecha>.log` (`sqlcmd` devuelve 0 = éxito).
- El historial de la tarea muestra "0x0" cuando corrió bien.

## Notas de producción

- La autenticación usada es **Windows integrada** (`-E`): no hay credenciales en archivos
  ni en la línea de comandos de la tarea. El usuario que corre la tarea necesita permiso
  de respaldo sobre ambas bases (`db_backupoperator` o superior; en el laboratorio el
  administrador local ya es `sysadmin`).
- Retención: los `.bak` se acumulan; definir rotación (p. ej. conservar 30 días) en la
  puesta en producción.
- Complemento en la aplicación: la bitácora interna registra los respaldos lanzados desde
  la propia plataforma (REQ-ARQ-005); este mecanismo cubre el caso "sin aplicación abierta".
