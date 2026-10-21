# Programa de integración CAPATAZ → Oracle Fusion (IntegracionNewsan)

**Cliente:** ALADDIN SRL · **Destino:** Newsan (Oracle Fusion) · **Desarrollo:** ULASOFT
**Versión del documento:** 21/10/2026 · **Versión del programa:** actualización 3b

---

## 1. Descripción general

### 1.1 Para qué existe

ALADDIN arma motos por cuenta de Newsan (fasón). Newsan administra su producción en Oracle Fusion, y cada moto que ALADDIN termina tiene que quedar **declarada como producto terminado en la orden de producción de Fusion**. Además, cada moto tiene que llevar sus datos de identificación: número de serie (dato fiscal con prefijo 073), VIN, número de motor y año modelo.

ALADDIN registra su producción en CAPATAZ. El programa **IntegracionNewsan** une los dos sistemas: toma de CAPATAZ las motos que ALADDIN declaró y las declara en Fusion, sin carga manual y sin duplicados.

El alcance acordado con Newsan es **solo la declaración de producto terminado**. Los materiales de cada moto los descuenta Fusion automáticamente al recibir la declaración, por lo que el programa no los informa.

### 1.2 Cómo funciona

El programa no tiene pantallas. Es un proceso que **se ejecuta, hace su trabajo y termina**. Está pensado para que el Programador de tareas de Windows lo ejecute cada pocos minutos, de modo que cada moto declarada en planta llegue a Fusion poco después.

En cada ejecución ("corrida"):

1. **Lee las motos declaradas en CAPATAZ.**
   - Se conecta a la API de CAPATAZ con un usuario de CAPATAZ y ejecuta una consulta preparada para esto, llamada "Integracion Fusion".
   - La consulta devuelve las motos declaradas en los últimos 30 días con sus datos: orden de trabajo, número de orden de Fusion (cargado en la Leyenda 1 de la OT), artículo, número de serie, VIN, número de motor, fecha y hora de la declaración.
   - El programa **no modifica nada** en CAPATAZ: solo lee.
2. **Descarta las motos que ya envió.** Cada vez que una declaración se envía con éxito, la registra en un archivo propio. Las que figuran ahí se ignoran, así ninguna moto se declara dos veces.
3. **Revisa que cada moto pendiente esté en condiciones de enviarse**: que tenga todos sus datos, que la serie tenga el formato correcto, que el VIN sea válido y que no haya series repetidas. Si algo falla, **no la envía**: deja un aviso en el log con el motivo exacto, y la moto queda esperando. Cuando alguien corrige el dato en CAPATAZ o en Tango, sale sola en la corrida siguiente.
4. **Agrupa las motos por orden de Fusion** y, por cada orden, **consulta en Fusion cuántas unidades se pueden declarar** (`ReadyQuantity`). Ese límite lo da la orden de compra del servicio de fasón: el programa **nunca declara más** que ese número, y lo que excede queda para cuando Newsan habilite más cantidad.
5. **Arma y envía una declaración por moto**, con el formato que definió Newsan. Agrega datos que calcula solo:
   - el **año modelo**, a partir del VIN;
   - el **LPN**, código de embalaje único por moto, a partir del número de serie.
6. **Registra cada resultado.** Las enviadas quedan en el registro de enviados. Cada mensaje enviado y cada respuesta de Fusion se guardan en archivos para auditoría. Todo queda en un log diario, con un resumen al final de la corrida.

Si en una corrida falla algo externo (Fusion no responde, error de conexión, orden inexistente), el programa no envía lo afectado y lo reintenta solo en la corrida siguiente.

### 1.3 Qué tiene que pasar en CAPATAZ para que una moto se envíe

1. La OT de CAPATAZ tiene en la **Leyenda 1** el número de la orden de Fusion (por ejemplo `PCANS-1042`). Lo carga Planificación al crear la OT.
2. La moto se declara en **Terminados a Depósito**. CAPATAZ le asigna automáticamente el número de serie `073-NNNNNNNNN/AAAA`, con una función de numeración desarrollada para Newsan.
3. Se cargan el **VIN** y el **número de motor** de la serie, escaneándolos en Tango → Mantenimiento de Series.

Con esos tres pasos completos, la moto se envía en la corrida siguiente.

---

## 2. Tecnología

| Aspecto | Detalle |
|---|---|
| Lenguaje | **C#** |
| Plataforma | **.NET 10** (versión LTS, soporte de Microsoft hasta noviembre de 2028) |
| Tipo de aplicación | Aplicación de consola, sin interfaz gráfica |
| Librerías | Solo las de .NET (`HttpClient` para las llamadas web, `System.Text.Json` para JSON) y `Microsoft.Extensions.Configuration` 10.0 (Json y Binder) para leer la configuración. Sin componentes de terceros. |
| Entorno de desarrollo | Visual Studio Community 2026 |
| Control de versiones | Git, repositorio privado en GitHub (a transferir a ULASOFT) |
| Origen de datos | **API Web de CAPATAZ** (REST/JSON, autenticación con token JWT) |
| Destino | **API REST de Oracle Fusion SCM**, versión `11.13.18.05`, por HTTPS con autenticación básica (usuario técnico) |
| Distribución | Ejecutable **autocontenido**: incluye .NET, no requiere instalar nada en el servidor |
| Licencias | .NET y el programa publicado no requieren licencias |

### Piezas dentro de CAPATAZ / Tango

| Pieza | Qué es |
|---|---|
| Consulta personalizada **"Integracion Fusion"** | Consulta SQL en Gestión integral de series (PS078). Reúne los datos de cada moto. Es lo que el programa ejecuta por API. |
| Función **`dbo.CZFN_N_SERIE_NSAN`** (código NSAN) | Función SQL de numeración de series, asignada al artículo en Extensión de artículos → Calidad. Genera `073-` + 9 dígitos + `/` + año, continuando la numeración. |
| Parámetros de Stock → Series | Descripción adicional 1 = VIN, 2 = Nro. Motor |
| Leyenda 1 de la OT | Número de la orden de Fusion |

---

## 3. Cómo se planea que corra

| Aspecto | Plan |
|---|---|
| **Dónde** | En **un solo equipo**, siempre el mismo. Idealmente el servidor donde está la API de CAPATAZ de ALADDIN, que está siempre encendido. A definir con Martín. |
| **Por qué en uno solo** | El registro de enviados es local. Dos equipos corriéndolo podrían declarar dos veces la misma moto. |
| **Cómo se ejecuta** | **Programador de tareas de Windows**, cada X minutos (a definir, por ejemplo 5 o 10) |
| **Requisitos del equipo** | Acceso a la API de CAPATAZ y **salida a internet por HTTPS** hacia Fusion (Newsan podría requerir habilitar la IP) |
| **Instalación** | Se publica desde Visual Studio (o `dotnet publish -c Release -r win-x64 --self-contained true`) y se copia la carpeta resultante, por ejemplo a `C:\IntegracionNewsan\` |
| **Carpetas que genera** | `logs\` (un log por día), `estado\` (registro de enviados), `salida\` (mensajes y respuestas) |
| **Código de salida** | `0` = sin problemas; `1` = hubo errores de envío o series duplicadas. El Programador de tareas lo muestra como "resultado de la última ejecución". |
| **Reloj** | El año de la serie lo pone el **servidor SQL** y la hora de la declaración la registra CAPATAZ. Conviene que IT sincronice la hora de los equipos. |

---

## 4. Validaciones (en el orden en que se aplican)

Para cada moto leída:

| # | Control | Si falla | Nivel en el log |
|---|---|---|---|
| 1 | **¿Ya se envió?** (artículo + serie en el registro) | Se saltea en silencio. Se cuenta como "ya enviada". | — |
| 2 | **¿La misma serie aparece en otra moto leída** (otro artículo)? | **No se envía ninguna** de las dos | ERROR |
| 3 | **¿La serie ya se envió antes con otro artículo?** | No se envía | ERROR |
| 4 | **Datos obligatorios:** serie, VIN, número de motor y orden de Fusion (Leyenda 1) | No se envía. Indica qué falta. | AVISO |
| 5 | **Formato de la serie:** `073-NNNNNNNNN/AAAA` (configurable) | No se envía | AVISO |
| 6 | **VIN de 17 caracteres** | No se envía | AVISO |
| 7 | **Carácter 10 del VIN** es un código de año válido (ISO 3779; no se usan I, O, Q, U, Z ni 0) | No se envía | AVISO |
| 8 | **Año modelo coherente:** no puede diferir más de 1 año (configurable) del año de la declaración | No se envía ("revisar el VIN") | AVISO |

Los controles 2 y 3 cubren un riesgo concreto: si dos puestos declaran al mismo tiempo, la función de numeración podría darles el mismo número (calcula el próximo, no lo reserva). Con estos controles, **un número fiscal duplicado nunca llega a Fusion**.

Por cada orden de Fusion, antes de enviar:

| # | Control | Si falla | Nivel |
|---|---|---|---|
| 9 | **La orden existe en Fusion** para la organización configurada | No se envía ninguna moto de esa orden. Se reintenta en la próxima corrida. | ERROR |
| 10 | **Cantidad disponible** (`ReadyQuantity` de la operación) | Las motos que exceden quedan pendientes | INFO |

Al enviar:

| # | Control | Si falla | Nivel |
|---|---|---|---|
| 11 | **Fusion responde con éxito** (HTTP 2xx) | No se registra como enviada. Se guarda la respuesta y se reintenta en la próxima corrida. | ERROR |

---

## 5. Datos calculados

| Dato | Regla | Ejemplo |
|---|---|---|
| **Año modelo** (`anoModelo`) | Carácter 10 del VIN según ISO 3779 (S = 2025, T = 2026, V = 2027, W = 2028…). Como el código se repite cada 30 años, se toma el año más cercano a la declaración. | `8BFSKP80S`**`V`**`C000058` → **2027** |
| **LPN** (`ExternalSystemPackingUnit`) | Prefijo `LPN` + serie, con `/` reemplazado por `-`. Único porque la serie es única. | `LPN073-000000008-2026` |
| **Fecha de la transacción** (`TransactionDate`) | Fecha (`fecha_mov`) + hora (`hora_comp`) del comprobante de declaración en CAPATAZ. Zona horaria opcional por configuración. | `2026-10-21T13:55` o `2026-10-21T13:55:00-03:00` |
| **Secuencia de la operación** | La lee de la orden en Fusion. Si no viene, usa la configurada (10). | `10` |

---

## 6. Filtros y controles de ejecución

| Filtro / control | Dónde | Qué hace | Uso |
|---|---|---|---|
| **Ventana de 30 días** | SQL de la consulta "Integracion Fusion" | Solo trae motos declaradas en los últimos 30 días | Siempre. Una moto que espere su VIN más de 30 días deja de leerse (a revisar). |
| **Registro de enviados** | `estado\enviados.json` | Excluye lo ya enviado | Siempre |
| **`FiltroOtFusion`** | Configuración | Procesa solo las motos de una orden de Fusion | **Solo pruebas.** En producción, vacío. |
| **`MaximoEnvios`** | Configuración | Máximo de motos a enviar por corrida (0 = sin límite) | **Solo pruebas** (por ejemplo, 1 para mandar de a una). En producción, 0. |
| **`ControlarDisponible`** | Configuración | `true` respeta `ReadyQuantity`; `false` envía igual | `false` **solo** para ver cómo rechaza Fusion. En producción, siempre `true`. |
| **`ModoEnvio`** | Configuración | `Fusion` (envío real) o `Simulacion` (guarda los mensajes en archivos, sin enviar) | Simulación para ensayos |
| **`Origen`** | Configuración | `Api` (CAPATAZ) o `ArchivoPrueba` (lee un JSON de ejemplo) | `ArchivoPrueba` para probar sin CAPATAZ |
| Orden de envío | Programa | Dentro de cada orden: por fecha, hora y serie de la declaración | Siempre |

---

## 7. Conexiones

### 7.1 CAPATAZ (lectura)

| Paso | Llamada |
|---|---|
| Inicio de sesión | `POST /Login` con usuario y contraseña → token JWT (dura unas 8 h) |
| Lectura de motos | `GET /ConsultasDeGrilla/Ejecutar`, headers `idEmp` (empresa) e `idConsulta` (consulta "Integracion Fusion") |
| Token vencido (401) | Vuelve a iniciar sesión una vez y reintenta |

### 7.2 Oracle Fusion

| Paso | Llamada |
|---|---|
| Consulta de la orden | `GET /fscmRestApi/resources/11.13.18.05/workOrders?q=OrganizationCode='PCANS';WorkOrderNumber='PCANS-1042'&expand=WorkOrderOperation&onlyData=true` → de la operación toma `ReadyQuantity`, `OperationSequenceNumber`, `CompletedQuantity`, `ReceivedQuantity` |
| Declaración | `POST /fscmRestApi/resources/11.13.18.05/operationTransactions` con el mensaje de la moto |
| Autenticación | Básica (usuario técnico de Newsan), por HTTPS |

Nota: en las pruebas se verificó que las condiciones de la consulta se separan con `;`. Con `and`, Fusion devuelve vacío.

### 7.3 Mensaje de declaración (una moto)

```json
{
  "SourceSystemCode": "FUSION_MOBILE",
  "SourceSystemType": "EXTERNAL",
  "OperationTransactionDetail": [{
    "SourceSystemCode": "FUSION_MOBILE",
    "OrganizationCode": "PCANS",
    "WorkOrderNumber": "PCANS-1042",
    "WoOperationSequenceNumber": 10,
    "TransactionDate": "2026-10-21T13:55",
    "TransactionNote": "Declaracion CAPATAZ Aladdin",
    "TransactionQuantity": 1,
    "TransactionUnitOfMeasure": "EA",
    "FromDispatchState": "READY",
    "ToDispatchState": "COMPLETE",
    "ExternalSystemPackingUnit": "LPN073-000000008-2026",
    "TransactionSerial": [{
      "SerialNumber": "073-000000008/2026",
      "TransactionSerialDFF": [{
        "__FLEX_Context": "MOTOS",
        "vin": "8BFSKP80SVC000058",
        "numeroDeMotor": "2500413812",
        "anoModelo": "2027",
        "numeroDnrpa": "",
        "bloqueadoDnrpa": "No"
      }]
    }]
  }]
}
```

| Campo | Origen |
|---|---|
| `WorkOrderNumber` | Leyenda 1 de la OT de CAPATAZ |
| `OrganizationCode` | Configuración (`PCANS`) |
| `WoOperationSequenceNumber` | Orden en Fusion (o configuración) |
| `TransactionDate` | Fecha y hora de la declaración en CAPATAZ |
| `ExternalSystemPackingUnit` | LPN calculado |
| `SerialNumber` | Serie generada por CAPATAZ (función NSAN) |
| `vin` / `numeroDeMotor` | Descripciones adicionales 1 y 2 de la serie (STA06) |
| `anoModelo` | Calculado del VIN |
| Resto | Valores fijos configurables |

---

## 8. Registros que deja

| Archivo | Contenido |
|---|---|
| `logs\AAAA-MM-DD.log` | Una línea por evento (INFO / AVISO / ERROR) y un resumen por corrida: leídas, ya enviadas, incompletas, duplicadas, enviadas, sin disponible, errores |
| `estado\enviados.json` | Cada moto enviada: clave, OT de CAPATAZ, orden de Fusion, serie, VIN, motor, fecha de envío y resultado. Se graba de forma segura (archivo temporal + reemplazo). |
| `salida\fusion\..._orden_..._respuesta.json` | Respuesta de Fusion a cada consulta de orden |
| `salida\fusion\..._envio.json` / `..._respuesta.json` | Mensaje enviado y respuesta de Fusion por cada moto |
| `salida\*.json` | En modo simulación: el mensaje que se habría enviado |

Ejemplo de log:

```
[INFO ] === Inicio de la corrida · Origen: Api · Modo de envío: Fusion ===
[INFO ] Sesión iniciada en la API de CAPATAZ.
[INFO ] Motos leídas: 13 (API de CAPATAZ).
[AVISO] Serie '073-000000010/2026' (OT CAPATAZ 000000000085): no se envía. Falta: VIN, número de motor.
[INFO ] Orden PCANS-1042: operación 10 · ReadyQuantity 100 · CompletedQuantity ? · ReceivedQuantity 100
[INFO ] Serie '073-000000008/2026' (OT Fusion PCANS-1042): enviada. Fusion respondió HTTP 201. ...
[INFO ] Resumen: leídas 13 · ya enviadas 10 · incompletas 1 · series duplicadas 0 · enviadas 1 · sin disponible 0 · errores de envío 0.
```

---

## 9. Configuración

Archivo `appsettings.json`, junto al programa. Las contraseñas van en `appsettings.Local.json`, que **no se sube al repositorio**.

| Sección | Parámetro | Valor en pruebas | Descripción |
|---|---|---|---|
| Capataz | `UrlApi` | `http://localhost:18001` | API de CAPATAZ |
| | `Usuario` / `Password` | SUPERVISOR / (local) | Usuario de CAPATAZ |
| | `IdEmpresa` | 667 | Empresa (header `idEmp`) |
| | `IdConsulta` | 42296 | Id de "Integracion Fusion" (**cambia en cada instalación**) |
| Fusion | `UrlBase` | `https://hdqc-dev1.fa.ocs.oraclecloud.com` | Fusion TEST |
| | `Usuario` / `Password` | REST_API_EXT / (local) | Usuario técnico |
| | `ControlarDisponible` | true | Respetar `ReadyQuantity` |
| Declaracion | `OrganizationCode` | PCANS | Organización |
| | `WoOperationSequenceNumber` | 10 | Secuencia si Fusion no la informa |
| | `ZonaHoraria` | (vacío) | Por ejemplo `-03:00`. A confirmar con Newsan. |
| | `PrefijoLpn` | LPN | Prefijo del LPN |
| | `FormatoSerie` | `^073-\d{9}/\d{4}$` | Formato exigido de la serie |
| | `ToleranciaAnoModelo` | 1 | Años de diferencia admitidos |
| | `SourceSystemCode`, `TransactionNote`, etc. | Ver ejemplo | Valores fijos del mensaje |
| Ejecucion | `Origen` | Api | Api / ArchivoPrueba |
| | `ModoEnvio` | Fusion / Simulacion | Envío real o simulado |
| | `FiltroOtFusion` | PCANS-1042 | Solo pruebas |
| | `MaximoEnvios` | 1 | Solo pruebas |

---

## 10. Seguridad

- **CAPATAZ:** el programa solo **lee**, a través de la API oficial. No escribe en la base.
- **Credenciales:** fuera del código y del repositorio, en `appsettings.Local.json` en el equipo donde corre.
- **Fusion:** comunicación cifrada por HTTPS, con un usuario técnico provisto por Newsan.
- **Código:** repositorio privado; las versiones quedan registradas.
- **Pruebas:** en entornos de prueba (CAPATAZ de prueba y Fusion TEST), sin tocar datos reales de ALADDIN ni de Newsan.

---

## 11. Estado actual (21/10/2026)

| Funcionalidad | Estado |
|---|---|
| Configuración de CAPATAZ/Tango: VIN, motor, Leyenda 1, consulta | Hecha y probada en el entorno de prueba |
| Numeración automática de series `073-` (función NSAN) | Hecha y probada |
| Lectura por API, validaciones, duplicados, año modelo, LPN | Hechos y probados |
| Modo simulación | Probado |
| Conexión a Fusion TEST y consulta de la orden | Probada (navegador y programa) |
| **Envío real de declaraciones** | Implementado. **Primera prueba con Newsan el 21/10.** |
| Registro, log, archivos de auditoría | Implementados |

---

## 12. Próximos pasos y mejoras previstas

### Para cerrar las pruebas con Newsan

- Validar en Fusion las primeras declaraciones: serie, VIN, motor, año modelo, LPN, hora y descuento de materiales.
- Confirmar la **zona horaria** de la fecha.
- Aclarar el **límite de la OC**: ReadyQuantity 100 frente a la OC de 51 comentada por Newsan.
- Confirmar el **control de serie** del artículo en Fusion (hoy `SerialTrackingFlag = false` en TEST).
- Probar **superar la cantidad disponible** con una OC chica.
- Definir los valores de `SourceSystemCode` y `TransactionNote`.
- Si la serie **se reinicia cada año** (ajuste de un parámetro de la función NSAN).

### Para producción

- Replicar la configuración en la empresa de motos de ALADDIN (guía de replicación).
- Definir el equipo donde corre, la frecuencia y la salida a internet.
- Definir la **codificación de artículos** (los SKU de Newsan tienen 18–21 caracteres y Tango admite 15).
- Cargar en la función NSAN el **último número** que informe Newsan.
- Publicar el programa y programarlo en el Programador de tareas.
- Cambiar a la URL y credenciales de producción de Fusion.

### Mejoras previstas

- **Aviso de errores:** que alguien se entere sin revisar el log (por ejemplo, un mail con el resumen cuando hay avisos o errores).
- **Simulador de Fusion** para probar errores y casos límite sin depender de TEST.
- **Procedimientos** para anular una declaración y para corregir una moto ya enviada (en CAPATAZ y en Fusion).
- Revisar la **ventana de 30 días** de la consulta.
- Si ULASOFT lo habilita: cargar **VIN y motor al declarar** (hoy se cargan después en Tango).
- Opcional: **disparar el programa al declarar**, desde los eventos de CAPATAZ, en lugar de esperar al Programador de tareas.
