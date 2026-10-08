# IntegracionNewsan

Programa intermedio entre **CAPATAZ** (Aladdin) y **Oracle Fusion** (Newsan) para el negocio de ensamble de motos.

Lee de CAPATAZ las motos declaradas como producto terminado (con su número de serie, VIN y número de motor), arma la **declaración de producto terminado** de cada una con el formato que pide Newsan y la envía a Fusion.

- **Solo lee de CAPATAZ.** No modifica nada en CAPATAZ ni en Tango.
- Desarrollado en **C# / .NET 10** (aplicación de consola).
- Mantenimiento: consultores de ULASOFT. Toda la información necesaria está en este README y en [`docs/`](docs/).

> **Estado actual (oct-2026):** la lectura desde CAPATAZ y el armado del JSON están implementados. El **envío real a Fusion está pendiente** (falta el usuario técnico y la autenticación que debe definir Newsan). Mientras tanto se usa el **modo simulación**, que guarda cada declaración en un archivo JSON.

---

## 1. Cómo funciona

En cada corrida:

1. Lee la configuración (`appsettings.json` + `appsettings.Local.json`).
2. Inicia sesión en la API de CAPATAZ (`POST /Login`).
3. Ejecuta la consulta personalizada **"Integracion Fusion"** (`GET /ConsultasDeGrilla/Ejecutar`) y obtiene una fila por moto declarada.
4. Descarta las motos **ya enviadas** (registro en `estado\enviados.json`).
5. Descarta las motos **incompletas** (sin VIN, sin número de motor o sin OT de Fusion) y las informa en el log. Cuando alguien completa el dato, salen en la próxima corrida.
6. Por cada OT de Fusion, consulta cuánto se puede declarar (`ReadyQuantity`) — *solo en el envío real*.
7. Arma y envía la declaración de cada moto (una moto por declaración):
   - **Simulación:** guarda el JSON en la carpeta `salida\`.
   - **Fusion:** `POST /operationTransactions` *(pendiente)*.
8. Registra lo enviado y escribe el resumen en el log.

```
CAPATAZ (API) ──► IntegracionNewsan ──► Oracle Fusion (API de Newsan)
                         │
                         ├── salida\   JSON generados (simulación)
                         ├── estado\   motos ya enviadas
                         └── logs\     un archivo por día
```

## 2. Requisitos previos en CAPATAZ y Tango

Sin esto el programa no tiene datos para leer. Detalle completo en [`docs/registro-pruebas.md`](docs/registro-pruebas.md).

1. **Tango → Stock → Archivos → Actualizaciones → Parámetros de Stock → solapa Series:**
   Descripción adicional 1 = `VIN` · Descripción adicional 2 = `Nro. Motor` (sin los dos puntos).
2. **CAPATAZ → Gestión integral de series (PS078) → Grilla → Administrar Consultas de Grilla:**
   consulta personalizada **"Integracion Fusion"**, sin filtros, con el SQL de `docs/registro-pruebas.md`. Anotar su **id** (se obtiene con `GET /ConsultasDeGrilla/Consultas`, header `codigoForm = PS078`).
3. **Operación en planta:**
   - Al crear cada OT de motos, cargar la **OT de Fusion** (ej. `PCANS-1030`) en la **Leyenda 1** de la OT.
   - Al declarar cada moto (Terminados a Depósito → botón Series), cargar **número de serie, VIN y número de motor**.

## 3. Configuración

Hay dos archivos, junto al `.exe`:

| Archivo | Qué tiene | ¿Se sube al repo? |
|---|---|---|
| `appsettings.json` | Toda la configuración **menos las contraseñas** | Sí |
| `appsettings.Local.json` | Las contraseñas (CAPATAZ y Fusion). Sus valores pisan a los de `appsettings.json` | **No, nunca** |

Para crear `appsettings.Local.json`, copiar `appsettings.Local.example.json`, renombrarlo y completar la contraseña.

### Claves principales

| Clave | Para qué |
|---|---|
| `Capataz.UrlApi` | URL de la API de CAPATAZ (ej. `http://localhost:18001` o la IP del servidor) |
| `Capataz.Usuario` / `Password` | Usuario de CAPATAZ con el que se inicia sesión |
| `Capataz.IdEmpresa` | Id de la empresa en CAPATAZ (header `idEmp`) |
| `Capataz.IdConsulta` | Id de la consulta "Integracion Fusion". **Cambia en cada instalación** |
| `Declaracion.OrganizationCode` | Organización de Fusion (en el ejemplo de Newsan: `PCANS`) |
| `Declaracion.AnoModeloPorDefecto` | Año modelo si no viene en el comentario de la serie (provisorio) |
| `Declaracion.ExternalSystemPackingUnit` | Pendiente de definición de Newsan. Vacío = no se envía |
| `Ejecucion.Origen` | `Api` (lee de CAPATAZ) o `ArchivoPrueba` (lee `datos-prueba/consulta-ejemplo.json`, para probar sin la API) |
| `Ejecucion.ModoEnvio` | `Simulacion` (guarda los JSON) o `Fusion` (envío real, pendiente) |
| `Ejecucion.RegistrarEnviadosEnSimulacion` | Si es `true`, en simulación las motos quedan marcadas como enviadas |
| `Ejecucion.CarpetaSalida` / `CarpetaLogs` / `CarpetaEstado` | Carpetas de trabajo (relativas al `.exe`) |

## 4. Desarrollo

Requisitos: **Visual Studio 2026** (carga "Desarrollo de escritorio de .NET") o el **SDK de .NET 10**.

```
cd IntegracionNewsan
dotnet build
dotnet run --project IntegracionNewsan
```

Con `Origen = ArchivoPrueba` se puede correr en cualquier PC, sin la API. Resultado esperado con el archivo de ejemplo:

- Serie `124` → aviso: no se envía por falta de VIN y número de motor.
- Serie `TEST-0002` → se genera su JSON en `salida\`.
- En una segunda corrida, `TEST-0002` figura como ya enviada. Para repetir la prueba, borrar `estado\enviados.json`.

## 5. Publicación e instalación en el servidor del cliente

1. Publicar como ejecutable autocontenido (no requiere instalar .NET en el servidor):

   ```
   dotnet publish IntegracionNewsan/IntegracionNewsan -c Release -r win-x64 --self-contained true -o publicar
   ```

2. Copiar la carpeta `publicar` al servidor, por ejemplo a `C:\IntegracionNewsan\`.
3. Crear `appsettings.Local.json` con la contraseña y revisar `appsettings.json` (`UrlApi`, `IdEmpresa`, `IdConsulta`, `Origen = Api`).
4. Restringir los permisos de la carpeta (tiene contraseñas).
5. Probar una corrida a mano y revisar el log.
6. Programarlo con el **Programador de tareas de Windows**: acción = ejecutar `IntegracionNewsan.exe`, "Iniciar en" = la carpeta del programa, repetir cada X minutos.

El programa necesita acceso a la API de CAPATAZ y, para el envío real, salida a internet (HTTPS) hacia Oracle Fusion.

## 6. Diagnóstico

Primero mirar el log del día en `logs\AAAA-MM-DD.log`.

| Mensaje | Causa probable | Qué hacer |
|---|---|---|
| `No se pudo iniciar sesión en CAPATAZ (HTTP 401)` | Usuario o contraseña incorrectos | Revisar `appsettings.Local.json` |
| `La consulta ... devolvió HTTP 409` | La consulta usa filtros con variables `@@...@@` | Revisar que "Integracion Fusion" no tenga filtros seleccionados |
| `La consulta ... devolvió HTTP 404` o resultado vacío inesperado | `IdConsulta` o `IdEmpresa` incorrectos | Obtener el id con `GET /ConsultasDeGrilla/Consultas` |
| `Falta: VIN` / `número de motor` | La moto se declaró sin esos datos | Completarlos en Tango → Mantenimiento de Series → por Artículo |
| `Falta: OT de Fusion (Leyenda 1 de la OT)` | La OT de CAPATAZ no tiene la OT de Fusion | Cargarla en Modificación de OT → Leyendas y observación → Leyenda 1 |
| `No se encontró el archivo de prueba` | `Origen = ArchivoPrueba` sin el archivo | Usar `Origen = Api` o revisar `ArchivoPrueba` |
| Errores de conexión / timeout | La API de CAPATAZ no responde o la URL es incorrecta | Revisar `UrlApi` y que el servicio de la API esté corriendo |

Código de salida del programa: `0` = sin errores de envío; `1` = hubo errores (ver log).

## 7. Estructura del código

```
IntegracionNewsan/IntegracionNewsan/
├── Program.cs                  Orquesta la corrida
├── appsettings.json            Configuración (sin contraseñas)
├── appsettings.Local.example.json
├── Configuracion/Opciones.cs   Clases de configuración
├── Capataz/CapatazClient.cs    Login y consulta a la API de CAPATAZ (reintenta ante 401)
├── Origen/                     De dónde se leen las motos: API o archivo de prueba
├── Modelos/                    MotoDeclarada (fila de la consulta) y DeclaracionPT (JSON para Fusion)
├── Fusion/DeclaracionBuilder.cs Arma el JSON de declaración de cada moto
├── Envio/                      Simulación (archivo) y Fusion (pendiente)
├── Infra/                      Log y registro de motos enviadas
└── datos-prueba/               Respuesta real de la consulta, para probar sin la API
```

## 8. Pendientes

- **Envío real a Fusion** (`Envio/EnvioFusion.cs`): implementar cuando Newsan defina el usuario técnico y la autenticación.
- Confirmar con Newsan: origen del **número de serie** (`073-…/2026`), el campo **`ExternalSystemPackingUnit`** y dónde queda el **año modelo** en CAPATAZ.
