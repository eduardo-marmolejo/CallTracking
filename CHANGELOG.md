# Changelog

Todos los cambios documentados del proyecto, ordenados cronológicamente. La versión actual del ensamblado es `1.0.0.0`.

## 2026-10-23

- Separación del HTML, CSS y JavaScript en archivos propios, sin Tailwind ni frameworks externos.
- Hoja de estilos `web/css/styles.css` pura y archivo único `web/js/script.js`.
- Estructura inicial de la interfaz web (proyectos y captura de registros).

## 2026-10-24 al 2026-10-26

- Creación de la capa `Datos\` y del esquema SQLite `Esquema.sql` (`Usuarios`, `Projects`, `RegistroTiempo`, `PRAGMA user_version = 1`).
- Base `TimeTracking.db` junto al `.exe`, con ruta basada en `AppContext.BaseDirectory` y apertura segura en transacciones.
- Implementación de CRUD de proyectos y registros, repositorios y convertidores.
- Copias de seguridad automáticas al arrancar; retención ajustada posteriormente a 6 copias.
- Exportación a PDF y, en su momento, importación desde Tekla (posteriormente eliminada).

## 2026-10-26 al 2026-11-11

- Internacionalización español/inglés en toda la interfaz.
- Temas claro y oscuro.
- Filtros, orden por fecha y edición en línea de registros.
- Formato de fecha `MM/dd/yyyy` y reloj de 12 horas (`08:00 AM` / `05:00 PM`), manteniendo las duraciones.
- Mejoras visuales: cabecera `#3DBF91`, filas cebra, fila de captura `#88DC65`, botones primarios y tooltip de selección de todo.
- Revisión y verificación del proyecto completo frente al historial exportado.

## 2026-11-11

- Publicación del proyecto en GitHub (`TimeTracking`) con README, CHANGELOG y LICENSE.
- Exclusión de `publish`, `backups`, `reports` y `TimeTracking.db` del repositorio.
- Creación del release `v1.0.0` con el ZIP portable como asset.
- Cambio de rama por defecto de `main` a `master` y eliminación de `main`.

## 2026-11-16

- Publicación del release `v1.0.1` (commit `3bf421b`, tag `v1.0.1`) con el ZIP de `publish`.

## 2026-11-18

- Renombrado integral de `TimeTacking` / `Time Tacking` / `Time_Tacking` a `CallTracking` / `Call Tracking`:
  - `TimeTacking.slnx` → `CallTracking.slnx` y carpeta `Time Tacking\` → `Call Tracking\`.
  - `.csproj`, namespaces C#, clase `frmTimeTracking` → `frmCallTracking`.
  - Metadatos de ensamblado, producto, título y ejecutable (`CallTracking.exe` / `.dll`).
  - `TimeTracking.db` y sus 6 backups → `CallTracking_*.db`, con rutas y patrón de rotación actualizados.
  - Títulos y textos de `index.html`, `registroProyecto.html` y `script.js`.
  - Informes en `reports\` y salidas de `publish\`.
- Verificación: `dotnet restore`, `build` y `publish` sin errores (única advertencia preexistente `MSB3277`).

## 2026-11-21

- Renombrado de `web/registroProyecto.html` a `web/registroLlamadas.html` y actualización de todas las referencias vivas en `web/js/script.js`.
- Compañía obligatoria y dirección opcional en el formulario de proyecto.
- Valores de proyecto mostrados sin etiquetas; los campos vacíos se visualizan como `-`.
- Se conservan íntegramente los datos existentes y la versión de esquema.

## 2026-11-23

- Eliminación completa de Tekla de HTML, JavaScript, `bridge.js` y `JsBridge.cs`.
- Reestructuración de la tabla de registros y de la base de datos:
  - `Final` → `Duracion`, `Descanso` → `Nombre`, `Tiempo Invertido` → `NumeroContacto`.
  - `Inicio` solo se captura y valida; `Duracion` es el único campo sumado en el total.
  - `Duracion INTEGER NOT NULL DEFAULT 0 CHECK (Duracion BETWEEN 0 AND 1439)`.
  - `Nombre TEXT NOT NULL COLLATE NOCASE CHECK (length(trim(Nombre)) > 0)`.
  - `NumeroContacto TEXT NOT NULL DEFAULT ''`.
  - Índice `IX_RegistroTiempo_Project_Fecha` → `IX_RegistroLlamadas_Project_Fecha`.
  - Migración de las 65 filas existentes con `Duracion = 0`, `Nombre = 'Sin nombre'`, `NumeroContacto = ''`; `user_version = 1` e `integrity_check = ok`.
  - Backup previo: `CallTracking.db.antes_cambio_columnas.bak`.
- Eliminación del badge de duración y de su lógica en JavaScript.
- Ajustes de presentación: tabla con ancho controlado, `Acciones` dentro del contenedor, observaciones con ajuste de línea y `textarea` solo en edición; fila de captura devuelta a un `input` de una línea.
- Renombrado de etiquetas de totales (`Tiempo Invertido` → `Número de Contacto` en la cabecera correspondiente) y búsqueda por nombre.
- Verificación final: builds Debug y Release sin errores, pruebas de repositorio y pruebas en navegador con puente simulado.
