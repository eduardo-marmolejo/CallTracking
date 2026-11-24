# Call Tracking

Aplicación de escritorio para Windows que permite registrar y controlar el tiempo invertido en llamadas por proyecto. Es portable: la base de datos se crea junto al ejecutable y no requiere instalador ni servidor.

## Descripción

`Call Tracking` es una app .NET 10 con interfaz WebView2. Cada proyecto puede tener nombre, compañía (obligatoria en la captura) y dirección opcional. Dentro de cada proyecto se capturan llamadas con fecha, hora de inicio, duración en `HH:MM`, nombre de la persona contactada, número de contacto y observaciones. La duración es el único valor que se suma para obtener el total de tiempo invertido.

La información se guarda en `CallTracking.db` (SQLite) en la misma carpeta del ejecutable. Antes de inicializar la base, la aplicación crea una copia de seguridad automática y conserva las 6 más recientes.

## Funcionalidades

- Alta, edición y borrado de proyectos.
- Alta, edición y borrado de registros de llamadas.
- Captura de duración en formato `HH:MM`; `Inicio` se captura y valida, pero no interviene en los cálculos.
- Totales por proyecto y selección múltiple de registros para sumar duraciones.
- Búsqueda de registros por nombre.
- Exportación de la vista a PDF.
- Interfaz en español e inglés.
- Temas claro y oscuro.
- Edición de observaciones en un modal con `textarea` (`Enter` agrega salto de línea, `Escape` cancela).
- Tabla adaptable: `Acciones` permanece dentro del ancho de la tabla y las observaciones largas se ajustan sin desbordar.
- Copias de seguridad automáticas de la base (6 rotaciones).
- Arquitectura desacoplada: la interfaz web nunca ejecuta SQL ni conoce rutas; solicita operaciones permitidas al puente C#.

## Requisitos

- Windows 10 o 11 (x64).
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (para ejecutar el ZIP portable).
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
- Para compilar: .NET 10 SDK.

## Instalación

1. Descarga `CallTracking-1.0.0-win-x64.zip` desde la sección *Releases*.
2. Descomprime el contenido en una carpeta escribible (por ejemplo `C:\Users\<tu usuario>\Apps\CallTracking`). No lo ejecutes desde `Program Files` ni desde la raíz de `C:`.
3. Ejecuta `CallTracking.exe`.

En el primer arranque se crea `CallTracking.db`. No hay instalador ni servicios adicionales.

## Compilación

```powershell
dotnet restore CallTracking.slnx
dotnet build CallTracking.slnx -c Release
dotnet publish "Call Tracking\CallTracking.csproj" -c Release -r win-x64 --self-contained false -o publish
```

El resultado portable queda en `publish\`. El ZIP de distribución se genera a partir de esa carpeta.

## Uso

1. En la pantalla principal, crea un proyecto con su nombre y compañía.
2. Abre el proyecto para ir a `registroLlamadas.html`.
3. Captura la fecha, el inicio, la duración, el nombre, el número de contacto y las observaciones; pulsa *Agregar registro*.
4. Usa la búsqueda por nombre, selecciona registros para ver el total, edita o elimina según necesites.
5. Cambia tema e idioma desde la interfaz; exporta a PDF cuando lo requieras.

## Arquitectura

- `Call Tracking\Form1.cs` / `Form1.Designer.cs`: host WinForms que aloja WebView2.
- `Call Tracking\Program.cs`: arranque, copia de seguridad y validación de la base.
- `Call Tracking\WebView\JsBridge.cs`: puente con lista blanca de operaciones; traduce mensajes JSON del front-end a llamadas a la capa de datos.
- `Call Tracking\Datos\`: `BaseDatos` (ruta/versión), `Esquema.sql` (embebido), `Modelos`, `Convertidores` y repositorios (`RepoProyectos`, `RepoRegistros`, `RepoUsuario`).
- `Call Tracking\web\`: `index.html` (proyectos), `registroLlamadas.html` (llamadas), `css\styles.css`, `js\script.js` y `js\bridge.js`. HTML/CSS/JavaScript puro, sin frameworks.
- `CallTracking.db`: SQLite `STRICT`, versión de esquema 1, junto al ejecutable.

El contrato del puente es `{ op, requestId, payload } -> { requestId, ok, data | error }` y solo responde al origen virtual `app.local`.

## Autor

Eduardo Marmolejo Ornelas.

## Licencia

MIT. Consulta el archivo `LICENSE` incluido en el repositorio.
