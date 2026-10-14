# ALADDIN – Instructivo operativo: declaración de motos (CAPATAZ → Fusion)

**Versión:** 1 (borrador para pruebas) · 14/10/2026
**Basado en:** pruebas en el entorno de prueba de CAPATAZ. Los nombres de pantallas pueden variar levemente en la empresa de ALADDIN.

---

## Resumen del circuito

| # | Quién | Qué | Dónde |
|---|---|---|---|
| 1 | Planificación (Noelia) | Crear la OT por cada OP del Google Sheet y cargar la **OT de Fusion** en la Leyenda 1 | CAPATAZ – Gestión integral de OT |
| 2 | Planta | Declarar las motos terminadas. **La serie `073-…` se genera sola** | CAPATAZ – Terminados a Depósito |
| 3 | Planta | Imprimir la **etiqueta** de cada serie y pegarla en la moto | CAPATAZ – Movimiento de stock → Etiqueta |
| 4 | Planta | Cargar el **VIN** y el **número de motor** de cada serie (escaneando) | Tango – Mantenimiento de Series |
| 5 | Automático | El programa de integración envía a Fusion las motos completas | Programa (cada X minutos) |
| 6 | Responsable | Revisar las motos pendientes o con error | CAPATAZ – Gestión integral de series → "Integracion Fusion" |

**Regla de oro:** una moto **solo se envía a Fusion cuando tiene serie, VIN, número de motor y OT de Fusion**. Si falta algo, queda esperando, no se pierde.

---

## 1. Crear la OT y cargar la OT de Fusion (Planificación)

1. CAPATAZ → **Gestión integral de OT** → crear la OT:
   - **Artículo:** el de la moto (modelo y color de la OP).
   - **Cantidad:** la QTY de la OP.
2. Lanzar la OT.
3. **Modificar OT (ALT+M)** → solapa **"Leyendas y observación"** → **Leyenda 1** = número de la orden de **Fusion**.
   - Copiarlo **exacto**, sin espacios ni cambios. Una letra distinta hace que Fusion no encuentre la orden.
   - *(Pendiente de Newsan: confirmar si es el mismo código de la OP del Sheet o un número propio de Fusion.)*
4. Aceptar.

> Una OT = una OP del Sheet (un modelo y color).

---

## 2. Declarar las motos terminadas (Planta)

1. CAPATAZ → **Gestión integral de OT** → seleccionar la OT → **Terminados a Depósito (ALT+T)**.
2. **Agregar novedad:**
   - **Cantidad OK:** cantidad de motos terminadas.
   - Tildar **Pasa a Depósito** y elegir el depósito.
3. Botón **Series**: las filas aparecen con **"Se generará automáticamente"**. **No hay que escribir nada.**
4. Aceptar. CAPATAZ asigna los números de serie `073-NNNNNNNNN/AAAA` (correlativos).

---

## 3. Imprimir las etiquetas (Planta)

1. Desde el movimiento de stock de la declaración → opción **Etiqueta**.
2. Tildar **"Con detalle de números de series"**.
3. Imprimir: sale **una etiqueta por moto** con el número de serie y un código QR.
4. Pegar cada etiqueta en la moto que corresponde.

*(Pendiente: confirmar si el LPN debe ir impreso en la etiqueta.)*

---

## 4. Cargar VIN y número de motor (Planta)

Para **cada moto** declarada:

1. Tango → **Stock → Movimientos → Mantenimiento de Series → por Artículo**.
2. Buscar el artículo y la **serie** (la de la etiqueta).
3. **Modificar:**
   - **VIN:** escanear el número grabado en el chasis (17 caracteres).
   - **Nro. Motor:** escanear el número del motor.
4. Grabar.

**Importante:**
- El VIN tiene que tener **17 caracteres**. El programa lo controla, y del VIN saca el **año modelo**.
- Verificar que la serie buscada sea **la de la etiqueta pegada en esa moto**.

---

## 5. Envío a Fusion (automático)

El programa de integración corre solo cada X minutos y:

- envía las motos completas (una declaración por moto);
- **no envía dos veces** la misma moto;
- respeta la cantidad disponible de la orden en Fusion; lo que excede queda para más adelante;
- deja registro de todo en su log.

---

## 6. Control diario (Responsable)

CAPATAZ → **Gestión integral de series** → solapa **"Integracion Fusion"**. Muestra las motos declaradas en los últimos 30 días con su serie, VIN, motor y OT de Fusion.

| Situación | Qué significa | Qué hacer |
|---|---|---|
| VIN o motor vacíos | Falta el paso 4 | Cargarlos en Tango |
| OT de Fusion vacía | Falta la Leyenda 1 de la OT | Cargarla (paso 1.3) |
| Aviso "año modelo no coherente" o "VIN de N caracteres" | VIN mal escaneado | Corregir el VIN en Tango |
| Error "serie duplicada" | La misma serie quedó en dos motos | **Avisar a sistemas**. No se envía ninguna hasta corregir. |
| Moto que no aparece | Declarada hace más de 30 días, o no declarada | Avisar a sistemas |

Al corregir un dato, la moto se envía sola en la próxima corrida.

---

## Errores comunes a evitar

- Declarar en una OT **sin la Leyenda 1** cargada: la moto queda retenida hasta cargarla.
- Escribir a mano la serie: **no hace falta**, la genera CAPATAZ.
- Pegar la etiqueta en otra moto: la serie, el VIN y el motor quedarían cruzados en Fusion y en el DNRPA.
- Corregir el VIN **después** de enviada la moto: el cambio **no** se reenvía solo. Avisar a sistemas.

---

## Pendientes de definición

- Número de la orden en Fusion (Newsan).
- Si la numeración de serie se reinicia cada año (Newsan).
- LPN en la etiqueta.
- Cada cuánto corre el programa y cómo se avisan los errores.
- Procedimiento si se anula una declaración o hay que corregir una moto ya enviada.
