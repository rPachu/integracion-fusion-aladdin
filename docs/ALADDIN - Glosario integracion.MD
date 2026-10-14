# ALADDIN – Glosario de la integración CAPATAZ → Fusion

**Última actualización:** 13/10/2026
**Uso:** documento interno del consultor. Reúne las siglas y términos usados en el proyecto.

**Convenciones:**
- Los términos están **agrupados por tema** y, dentro de cada tema, **en orden alfabético**.
- Al final hay un **índice alfabético general** que indica en qué sección está cada término.
- Lo marcado **(no confirmado)** es una interpretación razonable que todavía no se verificó con Newsan, ULASOFT o ALADDIN.

---

## Índice de secciones

0. [Mapa rápido: cómo se conecta todo](#0-mapa-rápido-cómo-se-conecta-todo)
1. [Empresas, personas y sistemas](#1-empresas-personas-y-sistemas)
2. [Negocio y producción](#2-negocio-y-producción)
3. [La moto: identificación y datos fiscales](#3-la-moto-identificación-y-datos-fiscales)
4. [Tango y CAPATAZ](#4-tango-y-capataz)
5. [Tablas y vistas de base de datos](#5-tablas-y-vistas-de-base-de-datos)
6. [Oracle Fusion y la API de Newsan](#6-oracle-fusion-y-la-api-de-newsan)
7. [Campos del JSON de declaración](#7-campos-del-json-de-declaración)
8. [APIs y conceptos técnicos generales](#8-apis-y-conceptos-técnicos-generales)
9. [El programa de integración](#9-el-programa-de-integración)
10. [Git y GitHub](#10-git-y-github)
11. [Gestión del proyecto](#11-gestión-del-proyecto)
12. [Índice alfabético general](#12-índice-alfabético-general)

---

## 0. Mapa rápido: cómo se conecta todo

```
NEWSAN (cliente)                          ALADDIN (fasonier)
───────────────                           ──────────────────
Embarque llega con kits (PPL)  ───────►   Recibe los kits
Google Sheet de OP  ──────────────────►   Noelia crea la OT en CAPATAZ
                                           (OT de Fusion en la Leyenda 1)
                                          Planta arma la moto, graba el VIN
                                          Declara "Terminados a Depósito":
                                            serie 073-…/AAAA + VIN + motor
                                                    │
                                                    ▼
                                          API de CAPATAZ (consulta
                                          "Integracion Fusion")
                                                    │
                                                    ▼
                                          Programa de integración (C#)
                                           arma el JSON por moto
                                                    │
Oracle Fusion  ◄────────── GET workOrders / POST operationTransactions
(declaración de producto terminado)
```

---

## 1. Empresas, personas y sistemas

| Término | Qué es |
|---|---|
| **ALADDIN SRL** | Cliente de ULASOFT. Arma motos para Newsan (servicio de fasón). Usa Tango y CAPATAZ. |
| **CAPATAZ** | Sistema de producción y depósito de ULASOFT que trabaja sobre la base de Tango: órdenes de trabajo, series, partidas, etiquetas, API. Versión del entorno de prueba: 26.01.007. |
| **Cesar** | Desarrollador de ULASOFT. Confirmó que el programa intermedio puede hacerse en cualquier lenguaje (se eligió C#). |
| **DNRPA** | Dirección Nacional de los Registros Nacionales de la Propiedad del Automotor. Organismo donde se registra cada moto fabricada (certificado de fabricación). |
| **EBS** | Oracle E-Business Suite. Sistema de gestión de Oracle donde **hoy** Newsan crea las OP (pantalla "Resumen de Trabajos Discontinuos"). |
| **Fusion (Oracle Fusion)** | Oracle Fusion Cloud, el sistema de gestión de Newsan en la nube. Ahí se hace la **declaración de producto terminado** que vamos a integrar. |
| **Fusion TEST** | Ambiente de pruebas de Fusion. Newsan no da acceso directo al proveedor: las pruebas se hacen en conjunto y Sofía valida. |
| **Martín Zunino** | IT externo de ALADDIN. Intermediario con Newsan. Definió que el alcance es **solo producto terminado**. |
| **Newsan** | Empresa cliente de ALADDIN. Le encarga el armado de motos y le provee los kits. |
| **Noelia** | Persona de ALADDIN que recibe el Google Sheet de OP y responde las consultas operativas de Newsan. |
| **PCA** | Según Martín: el **depósito**. Figuraba en la tabla de integraciones descartada. |
| **Sofía** | Persona de Newsan (sistemas/Fusion). Definió el payload y valida las pruebas (UAT). |
| **Tango (Tango Gestión)** | ERP de Axoft sobre el que trabaja CAPATAZ. Versión del entorno de prueba: 25.1 (Tango.AI, RA 11 25). Allí están artículos, stock, series y parámetros. |
| **ULASOFT** | Empresa del consultor. Desarrolla e implementa CAPATAZ. |
| **WMS** | Warehouse Management System (sistema de gestión de depósito). Martín aclaró que en la tabla de Newsan no siempre era CAPATAZ. Ya no forma parte del alcance. |

---

## 2. Negocio y producción

| Término | Qué es |
|---|---|
| **Alterno** | Columna del Google Sheet (por ejemplo `CENAC13`). Indica qué **variante de estructura de producto (BOM)** se usa para producir: define los insumos a consumir. |
| **BOM** | Bill of Materials: lista de materiales o estructura del producto (qué insumos lleva una moto). |
| **CBU** | Completely Built Up. Moto que llega **casi armada** (cerca del 90 %). Su PPL ya trae número de serie, VIN, motor y modelo. |
| **Declaración de producto terminado (PT)** | Registrar que una unidad quedó terminada. En CAPATAZ es "Terminados a Depósito"; en Fusion es el `POST /operationTransactions`. **Es la única integración del alcance.** |
| **EMB / Embarque** | Envío de kits que llega de Newsan (por ejemplo `1016997`). La cantidad de kits del embarque define el lote de producción. |
| **Fasón** | Servicio de producción por cuenta de terceros: ALADDIN arma con materiales de Newsan y cobra el servicio. |
| **Google Sheet de OP** | Planilla que Newsan comparte con Noelia. Columnas: PO, EMB, Alterno, OP, Ingreso a producción, CODIGO, MODELO, QTY, Ritmo, Remanente y plan diario. |
| **IKD / KIT** | Incompletely Knocked Down. La moto llega **en partes** (kit) y se arma en ALADDIN. El PPL no trae serie, VIN ni motor: se generan en planta. |
| **Ingreso a producción** | Columna del Sheet. Fecha estimada por Newsan, que **valida ALADDIN** según su capacidad de producción. |
| **Insumo** | Material que se consume para armar la moto. La mayoría los aporta Newsan; algunos los pone ALADDIN (precintos, nafta, loctite). |
| **Lote de producción** | Cantidad a producir de una OP. La define el embarque (QTY). |
| **MODELO** | Columna del Sheet o del PPL con el modelo comercial (por ejemplo `QU BASE`, `QU FULL`). Del PPL se toma el año modelo. |
| **OC** | Orden de Compra. Newsan emite una OC de servicio; lo recibido de esa OC define la `ReadyQuantity` en Fusion. |
| **OP** | Orden de Producción de Newsan (por ejemplo `OP-1016997AZ`). Hoy se crea en EBS. Formato observado: `OP-` + embarque + abreviatura de color, **con variantes** (`AZ`/`AZU`, `BL`/`BLA`/`BLANCO`). Hay que copiarla exacta. |
| **OT** | Orden de Trabajo de **CAPATAZ** (por ejemplo `000000000075`). Una OT por cada OP del Sheet. |
| **OT de Fusion** | Número de la orden de trabajo **en Fusion** (ejemplo de Sofía: `PCANS-1030`). Va en la Leyenda 1 de la OT de CAPATAZ y en `WorkOrderNumber`. **Pendiente:** confirmar si es igual a la OP de EBS o tiene otra nomenclatura. |
| **PO** | Purchase Order (orden de compra) en el Sheet (por ejemplo `298938`). |
| **PPL** | Archivo del embarque con el detalle de lo que llega (modelo, cantidades y, en CBU, serie, VIN y motor). Hay versión KIT y CBU. **(Sigla no confirmada; se usa como "packing list" del embarque.)** |
| **Producto terminado (PT)** | La moto armada lista para entregar. |
| **QTY** | Cantidad (por ejemplo `245`). Define el lote. |
| **Remanente** | Columna del Sheet: cantidad que queda pendiente de producir. |
| **Ritmo** | Columna del Sheet: producción esperada por día. |
| **SKU / CODIGO** | Código de producto de Newsan (por ejemplo `91CUBQU110BASEAZUL`). Incluye modelo y color. |
| **Stock de terceros** | Mercadería en poder de ALADDIN que es propiedad de Newsan. |

---

## 3. La moto: identificación y datos fiscales

| Término | Qué es |
|---|---|
| **073** | Código de fábrica de **Newsan** ante el DNRPA. Es el prefijo del número de serie y del certificado. |
| **Año calendario** | Año que va **después de la barra** del número de serie (`/2026`). No es el año modelo. |
| **Año modelo** | Año del modelo de la moto (por ejemplo 2027). Se informa en `anoModelo`. Lo define planta y cambia normalmente alrededor del 1° de abril. Se puede obtener del **carácter 10 del VIN**. |
| **Bloqueado DNRPA** | Campo del JSON (`bloqueadoDnrpa`). Se envía `"No"`, como en el ejemplo de Sofía. |
| **Certificado de fabricación** | Documento del DNRPA por moto. Su "Número de Certificado" es el número de serie `073-…/AAAA`. |
| **Chasis / Cuadro** | Estructura de la moto. Su número grabado es el **VIN** ("Número de Cuadro" en el DNRPA). |
| **Código del año en el VIN** | Carácter 10 del VIN, según norma ISO 3779: `S` = 2025, `T` = 2026, `V` = 2027, `W` = 2028 (la norma no usa I, O, Q, U, Z ni 0). Comprobado con el ejemplo del DNRPA (`V` → 2027). |
| **Etiqueta** | Rótulo que se pega en la moto. Hoy la imprime Newsan al declarar; con la integración la imprime CAPATAZ (etiqueta de serie con QR). El LPN también debe ir impreso en la moto. |
| **ISO 3779** | Norma internacional que define la estructura del VIN (17 caracteres). |
| **LCM** | Licencia de Configuración de Modelo: habilitación del modelo de vehículo, que figura en el certificado DNRPA. **(No confirmado en el proyecto; no interviene en la integración.)** |
| **LPN** | License Plate Number: código de **embalaje o unidad**. En el JSON va en `ExternalSystemPackingUnit`. Según Newsan es obligatorio, debe empezar con "LPN", sin otro formato; ideal uno distinto por moto e impreso en la moto. Propuesta: generarlo a partir del número de serie. |
| **Número de motor** | Número grabado en el motor (por ejemplo `157FMJ-3 S1361378`). Se escanea al declarar. En Tango/CAPATAZ va en la **Descripción adicional 2 (DESC2)**. |
| **Número de serie (Newsan)** | Número fiscal de la moto: `073-NNNNNNNNN/AAAA` (9 dígitos y año calendario, 18 caracteres). **Lo genera ALADDIN**, correlativo único, continuando desde el último que informe Newsan. En CAPATAZ es el **N_SERIE**. **Pendiente:** si se reinicia cada año. |
| **Número DNRPA** | Número que asigna el DNRPA (por ejemplo `M-0000833780/2026`). En el JSON (`numeroDnrpa`) se envía vacío. |
| **VIN** | Vehicle Identification Number: número de chasis de 17 caracteres (por ejemplo `8BFYCK4D5TM010773`). Se graba en planta y se escanea al declarar. En Tango/CAPATAZ va en la **Descripción adicional 1 (DESC1)**. |

---

## 4. Tango y CAPATAZ

| Término | Qué es |
|---|---|
| **Administrar Consultas de Grilla** | Opción del menú Grilla (por ejemplo en PS078) para crear o editar consultas propias. Ahí está "Integracion Fusion". |
| **Artículo** | Producto en Tango (por ejemplo `0100100134` en prueba). Para motos debe tener "Lleva series". |
| **Collector** | Aplicación y módulo de CAPATAZ para operar con lectores o dispositivos móviles. Se analizó y quedó fuera del alcance. |
| **COMP / NUME** | Funciones de numeración de series que trae CAPATAZ. **COMP**: comprobante + correlativo de 6 dígitos (`dbo.CZFN_N_SERIE_COMP`). **NUME**: numérica (`dbo.CZFN_N_SERIE_NUME`). |
| **Comentario de la serie** | Tercer dato libre de la serie (25 caracteres). Hoy se usa como respaldo para el año modelo. |
| **Consulta de grilla** | Consulta SQL que alimenta una solapa de una pantalla de CAPATAZ y que también puede ejecutarse por API. |
| **Consulta integral de series** | Pantalla de solo consulta de una serie: artículos, movimientos, OT, rastreabilidad. |
| **Consulta personalizada** | Consulta de grilla creada por el usuario. **Sin filtros** se puede ejecutar por API. "Integracion Fusion" es una. |
| **Depósito** | Lugar de stock (en prueba: `1` – Depósito Casa Central). |
| **Descripción adicional 1 / 2 (DESC1 / DESC2)** | Dos datos extra de cada serie (25 caracteres cada uno). Se nombraron **VIN** y **Nro. Motor** en Parámetros de Stock. Recién con nombre aparecen en pantalla. |
| **Editor SQL de CAPATAZ** | Herramienta de CAPATAZ para ver y crear componentes SQL, como las funciones de número de serie. |
| **Empresa** | Base de datos de una compañía en Tango/CAPATAZ. Prueba: "Muestra Comercial" (`idEmp` 667). ALADDIN tendrá una empresa de motos. |
| **Etiquetas (de series)** | Impresión desde el Movimiento de Stock → "Etiqueta", con detalle de números de series. Una etiqueta por serie, con QR, artículo, OT, comprobante y partida. |
| **Eventos de procesos** | Puntos donde CAPATAZ ejecuta código propio (`.PRG`) antes o después de grabar: `Tratamiento_ANT`, `Actualiza_ANT`, `Actualiza_DES`, `Post_Actualiza_DES`, `Actualiza_Fallido_DES`, `Tratamiento_Fallido_DES`, `Ejecutables`. No se usan por ahora (posible mejora). |
| **Extensión de artículos** | Pantalla de CAPATAZ con datos extra del artículo. En la solapa **Calidad** está "Código de función para números de series". |
| **Función de número de serie** | Función SQL asignada al artículo que **genera automáticamente** las series al declarar. Bloquea la carga de VIN y motor en esa pantalla (probado el 13/10). Se planea una propia con el formato `073-`. |
| **Gestión integral de OT** | Pantalla principal de órdenes de trabajo: crear, modificar, lanzar, declarar. |
| **Gestión integral de series (PS078)** | Pantalla de series en CAPATAZ. Tiene las solapas de consultas, incluida "Integracion Fusion". No permite editar DESC1/DESC2. |
| **ICA** | Tipo de comprobante con que CAPATAZ registró las declaraciones de Terminados a Depósito en las pruebas (por ejemplo `ICA 0000100000003`). |
| **Leyenda 1 … 5** | Campos libres de la cabecera de la OT (Modificar OT → "Leyendas y observación"). **Leyenda 1 = OT de Fusion.** |
| **Mantenimiento de Series (Tango)** | Tango → Stock → Movimientos → Mantenimiento de Series → **por Artículo**. Permite modificar VIN, motor y comentario de una serie ya existente (actualiza STA06). **Es la forma elegida para cargar VIN y motor cuando la serie es automática.** |
| **Novedad** | Cada registro de avance de la OT (Agregar Novedad: cantidad OK, defectuosa, operación, "Pasa a Depósito"). |
| **N_PARTIDA / Partida** | Lote de stock (por ejemplo `158`). Se asigna al declarar. |
| **N_SERIE / Serie** | Número de serie de la unidad en Tango/CAPATAZ (hasta 30 caracteres). Para las motos va a ser el número `073-…/AAAA`. |
| **Parámetros de CAPATAZ** | Tabla de configuración por sección (`RANGO_DE_SERIES`, `SERIES`, `SEGURIDAD`, etc.). |
| **Parámetros de Stock (Tango)** | Tango → Stock → Archivos → Actualizaciones → Parámetros de Stock. En la solapa **Series** se nombran las descripciones adicionales (VIN, Nro. Motor). |
| **PP006** | Código del proceso **Terminados de OT a depósito**. |
| **PP078** | Reporte de números de serie asociado a producción (aparece en parámetros de impresora). |
| **PRG / Visual FoxPro** | Lenguaje en que está hecho CAPATAZ. Los eventos de procesos se programan en archivos `.PRG`. |
| **PS078** | Código del proceso **Gestión integral de series**. |
| **RANGO_DE_SERIES** | Parámetros para escribir rangos de series en un texto (indicador `-`, relleno `/`, separador `,`). Se probó que la serie `073-000000001/2026` se graba bien igual. |
| **Terminados a Depósito** | Proceso donde se declara la producción terminada de la OT (botón **Series**: N_SERIE, VIN, Nro. Motor). |
| **Transferir series** | Pantalla para mover series entre depósitos. Muestra DESC1/DESC2 pero no los deja editar. |
| **UB201** | Endpoint de la API de CAPATAZ para toma de inventario. Se analizó y quedó fuera del alcance. |

---

## 5. Tablas y vistas de base de datos

Las tablas que empiezan con **STA** son de Tango; las que empiezan con **CZ** son vistas de CAPATAZ.

| Nombre | Qué contiene |
|---|---|
| **CZCABECERA_OT** | Cabecera de las OT de CAPATAZ, con `leyenda_1` a `leyenda_5`. |
| **CZFN_N_SERIE_COMP / _NUME** | Funciones SQL de numeración de series (ver COMP / NUME). |
| **CZSERIES_PRODUCIDAS_OT** | Relaciona cada serie producida con su OT. |
| **CZSTA07** | Vista de movimientos de series (la usa la función COMP para no repetir números). |
| **CZSTA11** | Vista de artículos (`cod_articu`, `descripcio`). |
| **CZSTA14** | Vista de cabeceras de movimientos de stock: `fecha_mov` (fecha), `hora_comp` (hora de la declaración, formato `HHmmss`), `hora_ingreso`. |
| **STA06** | **Ficha de la serie**: `N_SERIE`, `DESC1` (VIN), `DESC2` (motor), `COMENTARIO`, depósito. **La consulta lee de acá.** |
| **STA07** | **Movimientos de la serie**. Si VIN y motor se cargan al declarar quedan acá y en STA06; si se corrigen después en Tango, solo cambia STA06. |

---

## 6. Oracle Fusion y la API de Newsan

| Término | Qué es |
|---|---|
| **CompletedQuantity** | Cantidad ya declarada como terminada en la operación de la orden. |
| **DFF** | Descriptive Flexfield: campos adicionales configurables de Oracle. Los datos de la moto (VIN, motor, año, DNRPA) van en el DFF de la serie, contexto `MOTOS`. |
| **FUSION_MOBILE** | Valor de `SourceSystemCode` del ejemplo de Sofía. |
| **NV1** | Organización de Newsan en **EBS** (pantalla de trabajos discontinuos, clase `STD 01 NV1`). |
| **operationTransactions** | Endpoint de Fusion para **declarar** (`POST /fscmRestApi/resources/11.13.18.05/operationTransactions`). |
| **PCANS** | Código de organización en **Fusion** del ejemplo de Sofía (`OrganizationCode`). En un ejemplo anterior era `PVM`. |
| **PEX** | Nombre que Newsan usa para el payload "Declaración de Producto Terminado (PEX)". **(Significado de la sigla no confirmado.)** |
| **ReadyQuantity** | Cantidad **disponible para declarar** en la operación de la orden (lo que Newsan ya recibió de la OC de servicio). **Nunca se declara más que esto.** |
| **ReceivedQuantity** | Cantidad recibida en la operación de la orden. |
| **Usuario técnico** | Usuario de Fusion para que el programa se conecte. Se coordina con Newsan. |
| **workOrders** | Endpoint de Fusion para **consultar la orden** antes de declarar (`GET …/workOrders?q=OrganizationCode='PCANS' and WorkOrderNumber='…'&expand=WorkOrderOperation&onlyData=true`). |
| **WorkOrderOperation** | Operación (paso) de la orden de Fusion. De ahí se leen las cantidades y la secuencia. |

---

## 7. Campos del JSON de declaración

Payload de Sofía, una moto por renglón. Entre paréntesis, de dónde lo toma el programa.

| Campo | Qué es (origen) |
|---|---|
| **SourceSystemCode** | Sistema origen: `FUSION_MOBILE` (configuración). Va en la cabecera y en el detalle. |
| **SourceSystemType** | Tipo de sistema origen: `EXTERNAL` (configuración). |
| **OperationTransactionDetail** | Lista de declaraciones. |
| **OrganizationCode** | Organización en Fusion: `PCANS` (configuración). |
| **WorkOrderNumber** | Orden de Fusion (**Leyenda 1** de la OT). |
| **WoOperationSequenceNumber** | Secuencia de la operación: `10` (configuración; idealmente se lee del GET). |
| **TransactionDate** | Fecha y hora de la declaración, `AAAA-MM-DDTHH:MM` (`fecha_mov` + `hora_comp` de CZSTA14). |
| **TransactionNote** | Nota libre. Ejemplo de Sofía: `REST MFG Txn`; nuestra: `Declaracion CAPATAZ Aladdin` (a confirmar). |
| **TransactionQuantity** | Cantidad: siempre `1` (una moto por declaración). |
| **TransactionUnitOfMeasure** | Unidad de medida: `EA` = *each* (unidad). |
| **FromDispatchState / ToDispatchState** | Estado desde/hasta: `READY` → `COMPLETE` (pasa de listo a completo). |
| **ExternalSystemPackingUnit** | **LPN** de la moto. |
| **TransactionSerial** | Lista de números de serie de la declaración. |
| **SerialNumber** | Número de serie `073-…/AAAA` (**N_SERIE**). |
| **TransactionSerialDFF** | Datos adicionales de la serie. |
| **__FLEX_Context** | Contexto del DFF: `MOTOS`. |
| **vin** | VIN (**DESC1**). |
| **numeroDeMotor** | Número de motor (**DESC2**). |
| **anoModelo** | Año modelo (carácter 10 del VIN; hoy provisorio). |
| **numeroDnrpa** | Vacío. |
| **bloqueadoDnrpa** | `No`. |

---

## 8. APIs y conceptos técnicos generales

| Término | Qué es |
|---|---|
| **401** | Error HTTP "no autorizado". En la API de CAPATAZ suele ser *token expired*: hay que volver a hacer Login. |
| **409** | Error HTTP "conflicto". En CAPATAZ aparece al ejecutar por API una consulta con filtros `@@…@@`. |
| **API** | Interfaz para que un programa use otro sistema sin pantallas. |
| **API de CAPATAZ (Web API)** | Servicio web de CAPATAZ (en prueba, `http://localhost:18001`; la aplicación web en `:18002`). |
| **Bearer** | Forma de enviar el token en cada llamada (`Authorization: Bearer <token>`). |
| **ConsultasDeGrilla/Ejecutar** | Endpoint de CAPATAZ que ejecuta una consulta de grilla por su id (header `idConsulta`). |
| **Endpoint** | Dirección puntual de una API (por ejemplo `/Login`). |
| **Filtros `@@…@@`** | Variables de las consultas de grilla predefinidas. Por API dan error 409. |
| **GET / POST** | Tipos de llamada HTTP: GET **consulta**, POST **envía o crea**. |
| **Header** | Dato que viaja con la llamada, fuera del cuerpo (por ejemplo `idEmp`, `idConsulta`). |
| **HTTP / HTTPS** | Protocolo de comunicación web (HTTPS = cifrado). |
| **idConsulta** | Id de la consulta de grilla (en prueba, "Integracion Fusion" = `42296`; en ALADDIN será otro). |
| **idEmp** | Id de la empresa en la API de CAPATAZ (en prueba, `667`). |
| **JSON** | Formato de texto para intercambiar datos entre sistemas. |
| **JWT / Token** | Credencial temporal que devuelve el Login (dura unas 8 horas). |
| **localhost** | "Esta misma PC". La API en `localhost` significa que corre en el mismo equipo. |
| **Login** | `POST /Login` de CAPATAZ: usuario y contraseña → token. |
| **Mock / "Fusion falso"** | Servicio de prueba local que responde como Fusion, para probar el envío sin credenciales. |
| **OAS / OpenAPI** | Estándar para describir una API (la de CAPATAZ es OAS 3.1). |
| **Payload** | El contenido (JSON) que se envía en una llamada. |
| **QR** | Código de barras bidimensional. Las etiquetas de series de CAPATAZ lo incluyen. |
| **REST** | Estilo de API web basado en GET/POST sobre URLs. La de Fusion es REST. |
| **SQL** | Lenguaje de consultas a la base de datos (SQL Server). |
| **Swagger** | Página web que documenta una API y permite probarla. |
| **Vista (SQL)** | Consulta guardada que se usa como si fuera una tabla (las CZ…). |

---

## 9. El programa de integración

| Término | Qué es |
|---|---|
| **.NET 10** | Plataforma de Microsoft sobre la que corre el programa. Es LTS, con soporte hasta noviembre de 2028. |
| **appsettings.json** | Configuración del programa (URLs, ids, valores fijos, modo). Va en el repositorio. |
| **appsettings.Local.json** | Configuración con **contraseñas**. Solo local: **nunca** se sube al repositorio. |
| **ArchivoPrueba** | Valor de `Origen`: lee las motos de un archivo JSON (`datos-prueba\consulta-ejemplo.json`) en lugar de la API. |
| **C#** | Lenguaje del programa (el lenguaje de ULASOFT). |
| **Clave** | Identificador de una moto en el registro: `artículo|serie`. Evita declarar dos veces. |
| **Código de salida** | Número que devuelve el programa al terminar: `0` = OK, `1` = hubo errores de envío. |
| **Consola (aplicación de)** | Programa sin ventanas: corre, escribe en pantalla y en el log, y termina. |
| **EnvioFusion / EnvioSimulacion** | Las dos formas de "enviar". Simulación escribe el JSON en `salida\`; Fusion hace el GET y el POST reales (**pendiente**). |
| **estado\enviados.json** | Registro de motos ya enviadas. Si se borra, se reenvían. |
| **Integracion Fusion** | La consulta personalizada de CAPATAZ (en PS078) que el programa lee por API. |
| **IntegracionNewsan** | Nombre de la solución y del proyecto en Visual Studio. |
| **LTS** | Long Term Support: versión con soporte extendido. |
| **logs\AAAA-MM-DD.log** | Log diario (INFO, AVISO, ERROR). |
| **ModoEnvio** | Configuración: `Simulacion` o `Fusion`. |
| **Origen** | Configuración: `Api` (lee de CAPATAZ) o `ArchivoPrueba`. |
| **Programador de tareas** | Herramienta de Windows que ejecutará el programa cada cierto tiempo. |
| **Publicar (self-contained)** | Compilar el programa con .NET incluido para copiarlo a otra PC sin instalar nada: `dotnet publish -c Release -r win-x64 --self-contained true`. |
| **salida\** | Carpeta donde la simulación guarda un JSON por moto (`AAAAMMDD_HHmmss_OT_serie.json`). |
| **Solución / Proyecto (.slnx / .csproj)** | Archivos que agrupan el código en Visual Studio. |
| **Validación** | Antes de enviar, el programa exige serie, VIN, motor y OT de Fusion. Si falta algo, la moto queda como AVISO y no se envía. |
| **Visual Studio Community 2026** | Programa para escribir y compilar el código (carga de trabajo "Desarrollo de escritorio de .NET"). |

---

## 10. Git y GitHub

| Término | Qué es |
|---|---|
| **.gitignore** | Lista de archivos que Git no sube (`bin`, `obj`, `salida`, `logs`, `estado`, `appsettings.Local.json`). |
| **Clonar (clone)** | Bajar una copia del repositorio a la PC (en `C:\Dev\aladdin-integracion-fusion`). |
| **Commit** | Guardar una versión de los cambios con un mensaje. |
| **Git** | Sistema de control de versiones: guarda la historia del código. |
| **Git Changes** | Panel de Visual Studio para ver cambios, hacer commit y push. |
| **GitHub** | Sitio donde está guardado el repositorio (privado; se transferirá a ULASOFT). |
| **Push** | Subir los commits locales a GitHub. |
| **Repositorio (repo)** | Carpeta del proyecto con toda su historia de versiones. |

---

## 11. Gestión del proyecto

| Término | Qué es |
|---|---|
| **Alcance** | Lo acordado a desarrollar: **solo declaración de producto terminado** CAPATAZ → Fusion. |
| **Entorno de prueba** | CAPATAZ y Tango de ULASOFT, empresa "Muestra Comercial" (667). Ahí se desarrolló y probó todo. |
| **Empresa de prueba de ALADDIN** | Copia o empresa aparte de motos en el servidor de ALADDIN, para hacer las UAT sin tocar la base real (**a confirmar con Martín**). |
| **Etapas de UAT** | Propuesta: 0) validación del JSON sin conexión; 1) conectividad (GET); 2) primera declaración (POST); 3) casos de prueba; cierre. |
| **Guía de replicación** | Lista paso a paso de lo que hay que configurar en la empresa de ALADDIN. |
| **Puesta en marcha** | Configuración y arranque de Tango/CAPATAZ en ALADDIN (artículos, estructuras, depósitos, carga inicial). |
| **Registro de pruebas** | Documento interno (`docs/registro-pruebas.md`) con todas las pruebas, definiciones y pendientes. |
| **Relevamiento** | Análisis inicial de cómo trabaja la empresa y qué necesita. |
| **UAT** | User Acceptance Testing: pruebas de aceptación con el usuario (Newsan valida que la integración funciona). Según el plan: 13 al 24/10/2026. |

---

## 12. Índice alfabético general

| Término | Sección |
|---|---|
| 073 | 3 |
| 401 / 409 | 8 |
| .NET 10 | 9 |
| .gitignore | 10 |
| __FLEX_Context | 7 |
| Administrar Consultas de Grilla | 4 |
| Alcance | 11 |
| ALADDIN SRL | 1 |
| Alterno | 2 |
| anoModelo | 7 |
| Año calendario / Año modelo | 3 |
| API / API de CAPATAZ | 8 |
| appsettings.json / appsettings.Local.json | 9 |
| ArchivoPrueba | 9 |
| Artículo | 4 |
| Bearer | 8 |
| Bloqueado DNRPA / bloqueadoDnrpa | 3, 7 |
| BOM | 2 |
| C# | 9 |
| CAPATAZ | 1 |
| CBU | 2 |
| Certificado de fabricación | 3 |
| Cesar | 1 |
| Chasis / Cuadro | 3 |
| Clave | 9 |
| Clonar | 10 |
| Código de salida | 9 |
| Código del año en el VIN | 3 |
| Collector | 4 |
| Comentario de la serie | 4 |
| Commit | 10 |
| COMP / NUME | 4 |
| CompletedQuantity | 6 |
| Consola | 9 |
| Consulta de grilla / personalizada / integral de series | 4 |
| ConsultasDeGrilla/Ejecutar | 8 |
| CZCABECERA_OT, CZSERIES_PRODUCIDAS_OT, CZSTA07, CZSTA11, CZSTA14 | 5 |
| CZFN_N_SERIE_COMP / _NUME | 5 |
| Declaración de producto terminado | 2 |
| Depósito | 4 |
| DESC1 / DESC2 | 4 |
| DFF | 6 |
| DNRPA | 1 |
| EBS | 1 |
| Editor SQL | 4 |
| EMB / Embarque | 2 |
| Empresa / Empresa de prueba de ALADDIN | 4, 11 |
| Endpoint | 8 |
| Entorno de prueba | 11 |
| EnvioFusion / EnvioSimulacion | 9 |
| estado\enviados.json | 9 |
| Etapas de UAT | 11 |
| Etiqueta / Etiquetas de series | 3, 4 |
| Eventos de procesos | 4 |
| ExternalSystemPackingUnit | 7 |
| Extensión de artículos | 4 |
| Fasón | 2 |
| Filtros @@…@@ | 8 |
| FromDispatchState / ToDispatchState | 7 |
| Función de número de serie | 4 |
| Fusion / Fusion TEST | 1 |
| FUSION_MOBILE | 6 |
| GET / POST | 8 |
| Gestión integral de OT / de series | 4 |
| Git / Git Changes / GitHub | 10 |
| Google Sheet de OP | 2 |
| Guía de replicación | 11 |
| Header | 8 |
| HTTP / HTTPS | 8 |
| ICA | 4 |
| idConsulta / idEmp | 8 |
| IKD / KIT | 2 |
| Ingreso a producción | 2 |
| Insumo | 2 |
| Integracion Fusion | 9 |
| IntegracionNewsan | 9 |
| ISO 3779 | 3 |
| JSON | 8 |
| JWT / Token | 8 |
| LCM | 3 |
| Leyenda 1 … 5 | 4 |
| localhost | 8 |
| Login | 8 |
| logs | 9 |
| Lote de producción | 2 |
| LPN | 3 |
| LTS | 9 |
| Mantenimiento de Series | 4 |
| Martín Zunino | 1 |
| Mock / "Fusion falso" | 8 |
| MODELO | 2 |
| ModoEnvio | 9 |
| N_PARTIDA / Partida | 4 |
| N_SERIE / Serie | 4 |
| Newsan | 1 |
| Noelia | 1 |
| Novedad | 4 |
| Número de motor | 3 |
| Número de serie (Newsan) | 3 |
| Número DNRPA / numeroDnrpa | 3, 7 |
| numeroDeMotor | 7 |
| NV1 | 6 |
| OAS / OpenAPI | 8 |
| OC | 2 |
| OP | 2 |
| operationTransactions | 6 |
| OperationTransactionDetail | 7 |
| OrganizationCode | 7 |
| Origen | 9 |
| OT / OT de Fusion | 2 |
| Parámetros de CAPATAZ / Parámetros de Stock | 4 |
| Payload | 8 |
| PCA | 1 |
| PCANS | 6 |
| PEX | 6 |
| PO | 2 |
| PP006 / PP078 / PS078 | 4 |
| PPL | 2 |
| PRG / Visual FoxPro | 4 |
| Producto terminado (PT) | 2 |
| Programador de tareas | 9 |
| Publicar (self-contained) | 9 |
| Puesta en marcha | 11 |
| Push | 10 |
| QR | 8 |
| QTY | 2 |
| RANGO_DE_SERIES | 4 |
| ReadyQuantity / ReceivedQuantity | 6 |
| Registro de pruebas | 11 |
| Relevamiento | 11 |
| Remanente / Ritmo | 2 |
| Repositorio | 10 |
| REST | 8 |
| salida\ | 9 |
| SerialNumber | 7 |
| SKU / CODIGO | 2 |
| Sofía | 1 |
| Solución / Proyecto | 9 |
| SourceSystemCode / SourceSystemType | 7 |
| SQL | 8 |
| STA06 / STA07 | 5 |
| Stock de terceros | 2 |
| Swagger | 8 |
| Tango | 1 |
| Terminados a Depósito | 4 |
| TransactionDate / Note / Quantity / UnitOfMeasure | 7 |
| TransactionSerial / TransactionSerialDFF | 7 |
| Transferir series | 4 |
| UAT | 11 |
| UB201 | 4 |
| ULASOFT | 1 |
| Usuario técnico | 6 |
| Validación | 9 |
| VIN | 3 |
| vin | 7 |
| Visual Studio Community 2026 | 9 |
| Vista (SQL) | 8 |
| WMS | 1 |
| WoOperationSequenceNumber | 7 |
| WorkOrderNumber | 7 |
| workOrders / WorkOrderOperation | 6 |
