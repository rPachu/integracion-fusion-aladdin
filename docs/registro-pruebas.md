# ALADDIN – Registro de pruebas: integración CAPATAZ → Fusion

**Última actualización:** 14/10/2026
**Entorno:** CAPATAZ 26.01.007 · empresa de prueba "Muestra Comercial" (RA 11 25, `idEmp` 667) · API en `http://localhost:18001` · Tango 25.1
**Uso:** documento interno del consultor. Todas las pruebas se hicieron en el entorno de pruebas, no en la base de ALADDIN.

---

## 1. Alcance

- Única integración acordada (según Martín, oct-2026): **salida de producto terminado**, es decir, la declaración de PT en Fusion.
- Datos por moto a informar: **número de serie, VIN, número de motor, año modelo**, más la **OT de Fusion**.

## 2. Definiciones de Newsan (mail de Sofía, 08/10)

- **Declaración:** `POST /fscmRestApi/resources/11.13.18.05/operationTransactions` (Work Order Operation Transactions), con el payload "Declaración de Producto Terminado (PEX)".
- **Consulta previa de disponibilidad:** `GET /fscmRestApi/resources/11.13.18.05/workOrders?q=OrganizationCode='PCANS' and WorkOrderNumber='PCANS-1030'&expand=WorkOrderOperation&onlyData=true`. En la operación se leen `ReceivedQuantity`, `ReadyQuantity` y `CompletedQuantity`. **No se puede declarar más de `ReadyQuantity`** (lo que Newsan ya recibió de la OC de servicio).
- En el ejemplo de Sofía la organización es **`PCANS`** y la OT tiene formato **`PCANS-1030`** (en el JSON de mayo figuraba `PVM`).
- **Acceso a Fusion TEST:** no se da al proveedor CAPATAZ. Las pruebas se hacen en conjunto: nosotros generamos las declaraciones y Sofía valida. El usuario técnico del proceso se coordina al momento de probar.
- **OT de Fusion por lote:** llega a Noelia en un **Google Sheet** de OP.
- **Modelo y cantidad por embarque:** llegan en el **PPL** del embarque (CBU o IKD). El año modelo se toma de la columna **MODELO** del PPL.
- **OT de prueba en Fusion TEST:** la provee Sofía al momento de probar.

### Respuestas de Newsan (Noelia / Sofía, 09 al 13/10)

- **Google Sheet de OP:** una fila por OP (modelo y color): PO, EMB, Alterno (variante de BOM), OP, Ingreso a producción (lo estima Newsan y lo valida Aladdin), CODIGO (SKU), MODELO, QTY, Ritmo, Remanente y plan diario.
- **OP:** hoy se crea en **EBS** (pantalla "Resumen de Trabajos Discontinuos", organización NV1). Formato `OP-` + embarque + color, **con variantes** (`AZ`/`AZU`, `BL`/`BLA`/`BLANCO`). Si en Fusion tiene la misma nomenclatura o no, **está pendiente** (lo consulta Noelia).
- **Número de serie:** lo genera **Aladdin**. Formato `073-NNNNNNNNN/AAAA`: prefijo 073 (dato fiscal), **9 dígitos**, **año calendario**. **Una sola numeración correlativa**, no por modelo. Newsan informa el último número usado. Ejemplo real DNRPA: `073-000075035/2026`. **Pendiente:** si se reinicia el 1/1.
- **Año modelo:** distinto del año de la serie (hoy series /2026 con año modelo 2027). Lo define planta al grabar el chasis. Se toma del **carácter 10 del VIN** (ISO 3779; en el ejemplo DNRPA, `V` = 2027). A confirmar en UAT.
- **VIN y motor:** en KIT se generan en planta y se **escanean** al declarar. En CBU vienen en el PPL.
- **LPN (`ExternalSystemPackingUnit`):** obligatorio, lo genera nuestro sistema, empieza con "LPN", sin otro formato. Se admiten duplicados, pero lo ideal es **uno por moto**, **impreso en la moto**.
- **Pantalla de declaración de Fusion** (enviada por Noelia): LPN, Trabajo, Producto, Ctd Disponible, Ctd, Desde Sec, Desde Paso, Motor, VIN. Coincide con el payload.

### Payload de la declaración (por moto)

```json
{
  "SourceSystemCode": "FUSION_MOBILE",
  "SourceSystemType": "EXTERNAL",
  "OperationTransactionDetail": [{
    "SourceSystemCode": "FUSION_MOBILE",
    "OrganizationCode": "PVM",
    "WorkOrderNumber": "1000000727",
    "WoOperationSequenceNumber": 10,
    "TransactionDate": "2024-10-10T15:20",
    "TransactionNote": "REST MFG Txn",
    "TransactionQuantity": 1,
    "TransactionUnitOfMeasure": "EA",
    "FromDispatchState": "READY",
    "ToDispatchState": "COMPLETE",
    "ExternalSystemPackingUnit": "LPNF-3000001",
    "TransactionSerial": [{
      "SerialNumber": "073-12000020/2026",
      "TransactionSerialDFF": [{
        "__FLEX_Context": "MOTOS",
        "vin": "8BFYCK4D5TM010772",
        "numeroDeMotor": "157FMJ-3 S1361377",
        "anoModelo": "2026",
        "numeroDnrpa": "",
        "bloqueadoDnrpa": "No"
      }]
    }]
  }]
}
```

## 3. Hallazgos clave (CAPATAZ / Tango)

- Cada serie tiene **N_SERIE (30 caracteres)**, **DESC1 (25)**, **DESC2 (25)** y **COMENTARIO (25)**. Un VIN y un número de motor tienen 17: entran.
- **DESC1 y DESC2 no se muestran en ninguna pantalla hasta que se les pone nombre** en Tango → Parámetros de Stock → Series. Con el nombre cargado, aparecen en la ventana de series de Terminados a Depósito.
- Tablas de Tango:
  - **STA06** = ficha de la serie.
  - **STA07** = movimientos de la serie.
  - Si el VIN y el motor se cargan **al declarar**, quedan en **las dos**. Si se corrigen después en Tango (por artículo), solo cambia **STA06**.
- Vistas de CAPATAZ usadas: **CZSERIES_PRODUCIDAS_OT** (serie ↔ OT), **CZSTA11** (artículos), **CZSTA14** (movimientos), **CZCABECERA_OT** (cabecera de OT, con `leyenda_1` a `leyenda_5`).
- La **OT de Fusion** se guarda en la **Leyenda 1** de la OT de CAPATAZ (Modificación de OT → solapa "Leyendas y observación") y queda en `CZCABECERA_OT.leyenda_1`.
- Las consultas de grilla predefinidas con filtros (variables `@@...@@`) **dan error 409 por API**. Hay que usar **consultas personalizadas sin filtros**.
- La API **no tiene campos de descripción** en `PP006/Declarar` ni en el CSV de QR (`ConfigCsvICA`). La carga del VIN y el motor se hace por pantalla al declarar.
- CAPATAZ permite ejecutar código propio (Visual FoxPro, archivos `.PRG`) en **eventos de los procesos** (`Tratamiento_ANT`, `Actualiza_ANT`, `Actualiza_DES`, `Post_Actualiza_DES`, `Actualiza_Fallido_DES`, `Tratamiento_Fallido_DES`, `Ejecutables`). Ejemplo de Cesar: `entregarinsumos_post_actualiza_des.PRG`. No se usa para el envío; queda como posible mejora (validar VIN/motor antes de grabar, o disparar el programa al declarar).

## 4. Configuración realizada (a replicar en la empresa de motos)

1. **Tango → Stock → Archivos → Actualizaciones → Parámetros de Stock → solapa Series:**
   - Descripción adicional 1 = `VIN`
   - Descripción adicional 2 = `Nro. Motor`
   - Sin los dos puntos: Tango los agrega solo.
   - Opcional: tildar "Usa comentario de la serie".
2. **CAPATAZ → Stock → Movimientos → Gestión integral de series (PS078) → Grilla → Administrar Consultas de Grilla → Agregar:**
   - Nombre: `Integracion Fusion` · Orden: `1000` · Habilitada: sí · Predeterminada: no · Personalizada: sí
   - Filtros: **ninguno** · Usuarios: Todos · Parámetros: ninguno
   - SQL: ver sección 6.
3. **Operación:** al crear cada OT de motos, cargar la **OT de Fusion** (ej. `PCANS-1030`) en la **Leyenda 1**.

## 5. Operación probada

- **Cargar la OT de Fusion:** Gestión integral de OT → Modificar OT (ALT+M) → solapa "Leyendas y observación" → Leyenda 1.
- **Declarar una moto:** Gestión integral de OT → seleccionar la OT lanzada → Terminados a Depósito (ALT+T) → agregar novedad (Cantidad OK = 1, Pasa a depósito) → botón **Series** → cargar **N_SERIE, VIN y Nro. Motor** → completar la partida → Aceptar.
- **Corregir VIN/motor después:** Tango → Stock → Movimientos → Mantenimiento de Series → **por Artículo** → buscar la serie → Modificar → grabar (tilde verde).
- **Ver los datos:** CAPATAZ → Gestión integral de series → solapa **"Integracion Fusion"**.

## 6. SQL de la consulta "Integracion Fusion"

```sql
SELECT
    (ROW_NUMBER() OVER (ORDER BY SPT.n_of, SPT.n_serie)) AS id,
    SPT.t_comp,
    SPT.n_of            AS nro_ot,
    OT.leyenda_1        AS ot_fusion,
    SPT.cod_articu,
    A.descripcio,
    SPT.n_serie,
    S6.desc1            AS vin,
    S6.desc2            AS nro_motor,
    S6.comentario,
    SPT.n_partida,
    S6.cod_deposi,
    EM.fecha_mov        AS fecha_declaracion,
    EM.hora_comp        AS hora_declaracion,
    EM.t_comp           AS tipo_comprob,
    EM.n_comp           AS nro_comprob
FROM CZSERIES_PRODUCIDAS_OT SPT
INNER JOIN CZSTA11 A
        ON A.cod_articu = SPT.cod_articu
LEFT JOIN STA06 S6
        ON S6.n_serie = SPT.n_serie
       AND S6.cod_articu = SPT.cod_articu
LEFT JOIN CZSTA14 EM
        ON EM.tcomp_in_s = SPT.tcomp_in_s
       AND EM.ncomp_in_s = SPT.ncomp_in_s
LEFT JOIN CZCABECERA_OT OT
        ON OT.t_comp = SPT.t_comp
       AND LTRIM(RTRIM(OT.n_of)) = LTRIM(RTRIM(SPT.n_of))
WHERE EM.fecha_mov >= DATEADD(DAY, -30, CAST(GETDATE() AS DATE))
```

Notas:
- PS078 exige como columnas obligatorias `COD_ARTICU`, `COD_DEPOSI`, `N_PARTIDA`, `N_SERIE` (por eso está `cod_deposi`).
- El VIN y el motor se leen de **STA06** para tomar siempre el valor corregido.
- La unión con la OT compara el número sin espacios, porque en algunas vistas viene con un espacio adelante.
- `hora_declaracion` sale de `CZSTA14.hora_comp` (texto `HHmmss`, ej. `120256`). El programa la combina con la fecha para armar `TransactionDate` (ej. `2026-10-07T12:02`).
- Probado en SSMS el 08/10: devuelve `ot_fusion = PCANS-1030` para las motos de la OT 75. **Pendiente:** actualizar la consulta en CAPATAZ y volver a probar por API.

## 7. Lectura por API (probada)

1. `POST /Login` (módulo Acceso) → devuelve el token JWT. Dura ~8 h; un **401** con *"The token expired"* significa que venció → volver a loguearse.
2. `GET /ConsultasDeGrilla/Consultas` (módulo Procesos) con `idEmp = 667` y `codigoForm = PS078` → la consulta "Integracion Fusion" tiene **id 42296** (en pruebas).
3. `GET /ConsultasDeGrilla/Ejecutar` con `idEmp = 667` e `idConsulta = 42296` → devuelve un JSON con una fila por moto (versión anterior, sin `ot_fusion`):

```json
[
  { "id": 2, "t_comp": "O/T", "nro_ot": " 000000000075", "cod_articu": "0100100134",
    "descripcio": "TABLET 10'' MARCA PHANTOM", "n_serie": "TEST-0002", "vin": "8BFYCK4D5TM010773",
    "nro_motor": "157FMJ-3 S1361378", "comentario": "", "n_partida": "157", "cod_deposi": "1",
    "fecha_declaracion": "2026-10-07T00:00:00", "tipo_comprob": "ICA", "nro_comprob": " 0000100000002" }
]
```

> Leer siempre la sección **"Server response"** del Swagger. El "200 OK – Example Value" de abajo es documentación, no la respuesta real.

## 8. El programa de integración

- **Lenguaje:** C# con **.NET 10** (LTS, soporte hasta nov-2028; .NET 8 termina el 10/11/2026). Cesar confirmó que se puede usar el lenguaje que se quiera.
- **Entorno:** Visual Studio Community 2026 (estable, no Insiders), carga "Desarrollo de escritorio de .NET". Se programa en la PC personal y se lleva compilado (`dotnet publish -c Release -r win-x64 --self-contained true`) a la PC de pruebas.
- **Forma:** aplicación de consola, ejecutada por el Programador de tareas de Windows (más adelante, posiblemente disparada desde el evento `Post_Actualiza_DES` de Terminados a Depósito).
- **Carpeta:** `C:\IntegracionNewsan\` con `appsettings.json`, `salida\`, `logs\` y `estado\`.

**Flujo de cada corrida:**
1. Leer la configuración.
2. Login en la API de CAPATAZ.
3. Ejecutar la consulta "Integracion Fusion".
4. Descartar lo ya enviado y lo que no tiene VIN, motor u OT de Fusion (queda como error en el log).
5. Por cada OT de Fusion con motos pendientes: `GET /workOrders` → `ReadyQuantity`, organización y secuencia de la operación.
6. Declarar hasta la cantidad disponible: `POST /operationTransactions`, una moto por renglón.
7. Lo que no entra en `ReadyQuantity` queda para la próxima corrida.
8. Registrar lo enviado y escribir el log.

**Reglas:**
- **Recortar espacios** en `nro_ot` y `nro_comprob`.
- **No enviar motos sin VIN, sin motor o sin OT de Fusion.**
- **Configuración fuera del código:** URL de la API, usuario, `idEmp`, id de la consulta, valores fijos del JSON. En el servidor de ALADDIN el id de la consulta va a ser otro.
- **Modo simulación** (hasta tener credenciales): genera los JSON en `salida\` en lugar de enviarlos. Opción para leer las motos desde un archivo JSON local, para probar sin la API.
- **Registro de enviados** para no declarar dos veces la misma moto.
- **401 → volver a loguearse y reintentar.**

## 9. Datos de prueba generados

| Dato | Detalle |
|---|---|
| OT 75 | Artículo 0100100134 (TABLET 10"), cantidad 20, lanzada el 06/10. **Leyenda 1 = `PCANS-1030`** |
| Serie `124` | Declarada antes de configurar los parámetros → sin VIN ni motor (partida 155) |
| Serie `TEST-0002` | Declarada con VIN `8BFYCK4D5TM010773` y motor `157FMJ-3 S1361378` (partida 157) |
| Serie `Test_ing_01` | Ingreso de Stock; VIN, motor y comentario corregidos desde Tango (partida 156) |
| Parámetros de Stock → Series | Descripción adicional 1 = VIN, 2 = Nro. Motor |
| Consulta | "Integracion Fusion" en PS078 (id 42296) |

Otras pruebas, sin resultado útil:
- Transferencia de series: muestra DESC1/DESC2 pero no los deja editar; dio error SQL 109 al grabar sin depósito destino y con un renglón adicional (F3).
- Comentario de la serie en Ingreso de Stock: se guarda en STA07, no en DESC1/DESC2.
- `UB201/TomaInventario`: no se probó (ya no forma parte del alcance).

## 9.1 Pruebas del programa (08/10/2026, PC de la oficina)

| # | Origen | Resultado |
|---|---|---|
| 1 | ArchivoPrueba | TEST-0002 genera el JSON; `124` queda rechazada (falta VIN y número de motor). |
| 2 | ArchivoPrueba | TEST-0002 no se repite: figura en `estado\enviados.json`. |
| 3 | ArchivoPrueba | Después de borrar `enviados.json`, se vuelve a generar. |
| 4 | ArchivoPrueba (actualización 1) | `TransactionDate` = `2026-10-07T12:02` (antes `T00:00`). `enviados.json` muestra "Simulación" con tilde. |
| 5 | **Api** | "Sesión iniciada en la API de CAPATAZ." Lee 2 motos; `124` rechazada; TEST-0002 genera `20261008_115958_PCANS-1030_TEST-0002.json` con `TransactionDate` `2026-10-07T12:02`, tomado de `hora_declaracion` de la consulta. Código de salida 0. |

El JSON generado respeta la estructura del payload de Sofía (sección 2). Versiones en el repo: "Proyecto inicial del programa" y "Hora en la declaracion y tildes en el registro".

## 9.2 Relojes y horas

Visto en las pruebas: la PC de la oficina está unos 3 minutos desfasada de la PC personal y unos 5 o 6 minutos del servidor de base de datos (estimación del consultor, no medida).

| Hora | De dónde sale | Para qué sirve |
|---|---|---|
| `TransactionDate` (lo que recibe Fusion) | `CZSTA14.hora_comp`, grabada por CAPATAZ al declarar | **Es la hora oficial de la declaración.** |
| Nombre de los archivos en `salida\`, log, `FechaEnvio` en `enviados.json` | Reloj de la PC donde corre el programa | Solo trazabilidad. No se usa para decidir nada (el control de duplicados es por artículo + serie). |

En la serie TEST-0002: `hora_comp` = 12:02:56 y `hora_ingreso` = 11:56:48 (unos 6 minutos de diferencia). **No está verificado** qué reloj usa cada campo (la terminal o el servidor SQL).

**A verificar:**
- Declarar una moto anotando la hora de la terminal y la del servidor, y comparar con `hora_comp` y `hora_ingreso`.
- En las pruebas con Sofía: si Fusion rechaza una `TransactionDate` posterior a su hora actual.

**Recomendación para producción:** que IT (Martín) sincronice la hora de las terminales donde se declara, del servidor SQL y de la PC donde corra el programa (dominio o servidor de hora).

## 9.3 Numeración automática de series (13–14/10)

- **No hay** numeración automática en Parámetros de Stock (Tango) ni en la ficha del artículo.
- **Sí existe en CAPATAZ:** Extensión de artículos → solapa **Calidad** → "Código de función para números de series". Trae **COMP** (`dbo.CZFN_N_SERIE_COMP`: comprobante + correlativo) y **NUME**.
- Las funciones se ven en el **Editor SQL** de CAPATAZ (Funciones de Nro. Serie). **No hay botón "Nuevo": se crea con "Copiar".** El nombre queda `CZFN_N_SERIE_` + código, se muestra en negrita (personalizada) y aparece en el desplegable del artículo. "Eliminar Personalización" dio error en la copia; no hizo falta borrarla.
- Firma de la función (script exportado): parámetros `@cod_articu varchar(15)`, `@cant decimal(16,6)`, `@t_comp varchar(3)`, `@n_comp varchar(14)`, `@comentario varchar(15)`; devuelve `@series (id, n_serie varchar(20), desc1 varchar(15), desc2 varchar(15), comentario varchar(25))`.
- **Función propia `dbo.CZFN_N_SERIE_NSAN` (código NSAN):** `073-` + 9 dígitos + `/` + año del servidor SQL. Toma el `073-` más alto existente en CZSTA07 (de cualquier artículo) y suma 1. Parámetros internos: `@ultimo_newsan` (número inicial) y `@reinicia_anual` (0/1). Saltea números existentes.
  - Prueba por SQL (`SELECT * FROM dbo.CZFN_N_SERIE_NSAN('MOTOQUBASEAZ', 2, 'ICA', '0000100000099', '')`): devolvió 006 y 007. ✔
  - Prueba declarando (OT 77, 2 unidades): generó `073-000000006/2026` y `/007`. ✔
- **Con función asignada, la fila de Series queda bloqueada:** no se puede cargar N_SERIE, VIN ni motor al declarar. La Gestión integral de series y Transferir series tampoco permiten editar DESC1/DESC2.
- **Decisión:** declarar con serie automática y **cargar VIN y motor después en Tango → Mantenimiento de Series (por Artículo)**. Consulta enviada a ULASOFT por si se pueden cargar al declarar.
- **Etiquetas:** desde el movimiento de stock → Etiqueta, "Con detalle de números de series": una etiqueta por serie con QR, artículo, OT, comprobante y partida.
- **Parámetros RANGO_DE_SERIES** (indicador `-`, relleno `/`): se probó que `073-000000001/2026` se graba como una sola serie. ✔
- **Riesgo abierto:** dos puestos declarando al mismo tiempo podrían recibir el mismo número (la función calcula, no reserva). Mitigación: confirmar cantidad de puestos con Martín y detección de duplicados en el programa (hecha en la actualización 2).

## 9.4 Prueba con artículo moto (13–14/10)

- Artículo de prueba **`MOTOQUBASEAZ`** (copia de la tablet). Observación: el SKU de Newsan `91CUBQU110BASEAZUL` tiene **18 caracteres** y el código de artículo admite **15**. Hay que definir la codificación en la puesta en marcha.
- **OT 76:** 3 motos con serie manual 003, 004 y 005 (la 005 sin VIN ni motor).
  - Corrida 1: 003 y 004 enviadas; 005 retenida. ✔
  - Se cargaron VIN y motor de la 005 en Tango (Mantenimiento de Series). Corrida 2: 005 enviada; el resto como ya enviadas. ✔ **El flujo "carga posterior en Tango" funciona.**
- **OT 77:** 2 motos con función NSAN (006 y 007).
  - Corrida sin Leyenda 1: retenidas por "Falta OT de Fusion". ✔
  - Con Leyenda: enviadas. ✔
- Los JSON salieron con `anoModelo` 2026 cuando el VIN indicaba 2027 (valor por defecto). Se corrigió en la actualización 2.

## 9.5 Actualización 2 del programa (14/10)

- **Formato de la serie:** se exige `073-NNNNNNNNN/AAAA` (configurable en `FormatoSerie`). Si no cumple, no se envía.
- **Año modelo desde el VIN:** carácter 10 (ISO 3779), eligiendo el año más cercano a la declaración. El VIN debe tener 17 caracteres y el año modelo no puede diferir más de `ToleranciaAnoModelo` (1) del año de la declaración. Se eliminó el año por defecto.
- **LPN:** `PrefijoLpn` + serie con `/` → `-` (ej. `LPN073-000000006-2026`).
- **Series duplicadas:** si la misma serie aparece en dos motos distintas, o ya se envió con otro artículo, **no se envía** y queda un ERROR (código de salida 1).
- Pendiente de compilar y probar en la PC de la oficina.

## 10. Pendientes

**De Newsan:**
- Número de la orden de trabajo en **Fusion** (¿igual a la OP de EBS?).
- Si la numeración de serie **se reinicia el 1/1**.
- Confirmar que el año modelo sale del carácter 10 del VIN.
- URL de TEST, usuario técnico, autenticación, OT de prueba, IP a habilitar, contacto técnico directo (reunión de UAT 14/10).

**De Martín:**
- Cantidad de puestos que declaran.
- Empresa de prueba de motos en el servidor de ALADDIN, API de CAPATAZ instalada, equipo donde corre el programa y su salida a internet.
- Codificación de artículos (SKU de 18 caracteres frente a 15).

**De ULASOFT:**
- ¿Se pueden cargar DESC1/DESC2 al declarar con función de numeración asignada?
- ¿Se puede modificar el diseño de la etiqueta de series (para agregar el LPN)?

**Nuestros – pruebas en CAPATAZ:**
- Misma serie en dos artículos distintos (¿Tango lo acepta?).
- Anular una declaración (qué pasa con la serie y con lo ya enviado).
- Corregir el VIN después de enviada (definir procedimiento).
- Sacar la moto del depósito (¿se mantienen VIN y motor en STA06?).
- Función NSAN con número inicial (`@ultimo_newsan`).
- Ventana de 30 días de la consulta.

**Nuestros – programa:**
- Compilar y probar la actualización 2.
- Envío real a Fusion (cuando haya credenciales) y "Fusion falso" para probarlo.
- Aviso de errores (mail u otro medio): a definir.
- Publicarlo y correrlo con el Programador de tareas.

**Nuestros – documentación:**
- Guía de replicación para ALADDIN.
- Instructivo operativo (versión 1 hecha el 14/10).
