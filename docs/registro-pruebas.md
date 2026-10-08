# ALADDIN – Registro de pruebas: integración CAPATAZ → Fusion

**Última actualización:** 08/10/2026
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

## 10. Pendientes

**De Newsan (consultado a Martín el 08/10):**
- Quién genera el **número de serie** (`073-…/2026`) de las motos que arma Aladdin (IKD). En el PPL de KIT la hoja IDENTIFICACION viene vacía; en el de CBU trae serie, VIN, motor y modelo.
- **Ejemplo real del Google Sheet** de OP.
- Qué es el campo **`ExternalSystemPackingUnit`** (`LPNF-…`), si es obligatorio y de dónde sale.
- Usuario técnico para la conexión del proceso (se coordina al probar).

**Nuestros:**
- Actualizar la consulta "Integracion Fusion" en CAPATAZ (con `ot_fusion`) y probarla por API.
- Desarrollar el programa en C#.
- Replicar la configuración (sección 4) en la empresa de motos del servidor de ALADDIN.
- Verificar qué pasa con la serie en STA06 cuando la moto sale del depósito (transferencia a PCA).
