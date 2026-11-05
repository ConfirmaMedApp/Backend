# Pagos de citas con Wompi — Guía de integración Frontend

Guía para consumir el flujo de pagos del backend **ConfirmaMed**. Los pacientes
(incluidos los del extranjero: Aruba, Caribe, etc.) pagan su cita con un **enlace
de pago de Wompi** y su tarjeta internacional.

> **Moneda — importante.** El precio se define y se muestra en **USD**, pero Wompi
> **solo cobra en pesos colombianos (COP)**. Al generar el enlace, el backend
> convierte USD→COP con la **TRM oficial** del día. El paciente paga con su tarjeta
> internacional y **su banco hace la conversión** a su moneda local. El frontend no
> hace ninguna conversión: muestra lo que devuelve el backend.

---

## 0. Convenciones

**Base URL:** `/api`

**Autenticación:** todos los endpoints de esta guía requieren **JWT** en el header
`Authorization: Bearer <access_token>` (ver la guía de Refresh Token). Los llama el
frontend **admin/doctor** autenticado, no el paciente.

**Envelope de respuesta.** Las respuestas **exitosas** vienen en `camelCase` y los
datos útiles siempre están en `items`:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Recurso obtenido satisfactoriamente",
  "path": "/api/...",
  "responseTime": "2026-11-02T18:00:00Z",
  "items": { }
}
```

> ⚠️ **Ojo con los errores.** Las respuestas de **error** las arma otro componente
> y salen en **PascalCase** (`Success`, `StatusCode`, `Message`, `Path`,
> `ResponseTime`, `Items`), con `Items: null`. Maneja ambos casos al parsear.

```json
{
  "Success": false,
  "StatusCode": 400,
  "Message": "La cita no tiene precio configurado. Define el precio del doctor para esa especialidad primero.",
  "Path": "/api/appointments/123/payment-link",
  "ResponseTime": "2026-11-02T18:00:00Z",
  "Items": null
}
```

Códigos de error usados: `400` (datos inválidos / sin precio), `401` (sin JWT),
`404` (cita o relación no encontrada), `503` (error de base de datos).

---

## 1. Modelo de precios

- El precio vive en la combinación **doctor + especialidad** (un mismo doctor puede
  cobrar distinto según la especialidad, y dos doctores pueden cobrar distinto por
  la misma especialidad). Se expresa en **USD**.
- Ese precio se **copia a la cita** cuando se crea la agenda. Si cambias el precio
  después, las citas ya creadas conservan el valor con el que nacieron (y si una
  cita quedó sin precio, el backend usa como respaldo el precio actual del combo).
- Requisito: el doctor **ya debe tener asignada** la especialidad para poder ponerle
  precio (eso se hace con el `POST /api/DoctorsHasSpecialities` existente).

---

## 2. Administrar precios (admin)

### `PUT /api/DoctorsHasSpecialities/price`
Fija o actualiza el precio (USD) de una combinación doctor + especialidad existente.

**Request**
```json
{ "doctorId": 1, "specialityId": 1, "priceUsd": 200 }
```

**Response `200`**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Precio actualizado satisfactoriamente",
  "items": {
    "id": 10,
    "doctorId": 1,
    "specialityId": 1,
    "priceUsd": 200.00,
    "status": true
  }
}
```

**Errores**
- `400` — `priceUsd` negativo, ids inválidos, o *"El doctor no tiene asignada esa
  especialidad. Asígnala primero."*

### `GET /api/DoctorsHasSpecialities/prices?doctorId=&specialityId=`
Lista los precios. Ambos filtros son opcionales y combinables (sin filtros devuelve
todo; por `doctorId` devuelve todas las especialidades de ese doctor; etc.).

**Response `200`**
```json
{
  "success": true,
  "statusCode": 200,
  "items": [
    { "id": 10, "doctorId": 1, "specialityId": 1, "priceUsd": 200.00, "status": true },
    { "id": 11, "doctorId": 2, "specialityId": 1, "priceUsd": 100.00, "status": true }
  ]
}
```

> `priceUsd` puede venir `null` si todavía no se le ha puesto precio a esa combinación.
> La lista trae `doctorId`/`specialityId`; los nombres los tienes del endpoint
> `GET /api/DoctorsHasSpecialities/specialities/{specialityId}`.

---

## 3. Generar el enlace de pago de una cita

### `POST /api/appointments/{id}/payment-link`
Crea (o regenera) el enlace de pago Wompi para la cita y, opcionalmente, lo envía por
correo al paciente.

**Request** (body opcional)
```json
{ "sendEmail": true }
```
- `sendEmail: false` (o sin body) → solo devuelve la URL para que la compartas tú
  (p. ej. por WhatsApp).
- `sendEmail: true` → además envía el enlace por email al paciente de la cita.

**Response `201`**
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Enlace de pago generado satisfactoriamente",
  "items": {
    "appointmentId": 123,
    "paymentLinkId": "3Z0Cfi",
    "checkoutUrl": "https://checkout.wompi.co/l/3Z0Cfi",
    "amountUsd": 200.00,
    "trm": 4100.50,
    "amountCop": 820100,
    "amountInCents": 82010000,
    "currency": "COP",
    "expiresAt": "2026-11-05T18:00:00+00:00",
    "emailSent": true
  }
}
```

| Campo | Significado |
|-------|-------------|
| `checkoutUrl` | **La URL que abre/comparte el paciente.** Es la pantalla de pago de Wompi. |
| `amountUsd` | Valor en USD (lo que se muestra). |
| `trm` | Tasa USD→COP aplicada. |
| `amountCop` | Equivalente cobrado en pesos. |
| `amountInCents` | Lo mismo en centavos (lo que recibe Wompi). |
| `expiresAt` | Vencimiento del enlace (UTC). |
| `emailSent` | `true` si se envió el correo (solo si pediste `sendEmail`). |

**Errores**
- `400` — *"La cita no tiene precio configurado…"* (ponle precio al combo primero, §2).
- `404` — la cita no existe.
- `503` — no se pudo obtener la TRM, o la BD falló.

---

## 4. Mostrar los valores de la cita

### `GET /api/appointments/{id}/pricing`
Devuelve el precio en USD y su conversión a COP con la TRM vigente. Útil para mostrar
el monto **antes** de generar el enlace.

**Response `200`**
```json
{
  "success": true,
  "statusCode": 200,
  "items": {
    "appointmentId": 123,
    "priceUsd": 200.00,
    "trm": 4100.50,
    "priceCop": 820100,
    "currency": "COP",
    "paymentStatus": "UNPAID"
  }
}
```

> Si la cita no tiene precio, `priceUsd` viene `null` y `trm`/`priceCop` en `0`.

---

## 5. Consultar el estado de pago

### `GET /api/appointments/{id}/payment`
Estado de pago de la cita + el último enlace/transacción generado.

**Response `200`**
```json
{
  "success": true,
  "statusCode": 200,
  "items": {
    "appointmentId": 123,
    "paymentStatus": "PAID",
    "paymentLinkId": "3Z0Cfi",
    "checkoutUrl": "https://checkout.wompi.co/l/3Z0Cfi",
    "transactionStatus": "APPROVED",
    "amountUsd": 200.00,
    "amountCop": 820100,
    "currency": "COP",
    "expiresAt": "2026-11-05T18:00:00+00:00",
    "paidAt": "2026-11-02T18:05:00+00:00"
  }
}
```

- `paymentStatus` → estado **de la cita**: `UNPAID` o `PAID`.
- `transactionStatus` → estado **de la transacción Wompi**: `PENDING`, `APPROVED`,
  `DECLINED`, `VOIDED`, `ERROR` (o `null` si aún no hay enlace generado).

---

## 6. Flujo recomendado en el frontend

1. (Admin, una vez por doctor/especialidad) Fijar el precio con `PUT .../price` (§2).
2. (Opcional) Mostrar el monto de la cita con `GET .../pricing` (§4).
3. Generar el enlace con `POST .../payment-link` (§3) y:
   - abrir/compartir `checkoutUrl` con el paciente, **o**
   - pasar `sendEmail: true` para que le llegue por correo.
4. El paciente paga en Wompi. Al terminar, Wompi lo **redirige** a la página que
   configuró el backend (`Wompi:RedirectUrl`).
5. **La confirmación del pago es asíncrona** (llega al backend por webhook). Por eso,
   para reflejar el pago en la UI:
   - al volver del redirect, o en la vista de la cita, haz **polling** a
     `GET .../payment` hasta que `paymentStatus` sea `PAID` (o `transactionStatus`
     sea `DECLINED`/`VOIDED`/`ERROR`).
   - sugerido: reintentar cada ~3–5 s por ~1–2 min; el webhook suele confirmar en
     segundos.

> **El frontend no llama al webhook.** `POST /api/webhooks/wompi` es interno
> (Wompi → backend). El frontend solo consulta `GET .../payment`.

---

## 7. Estados

| `paymentStatus` (cita) | Significado |
|------------------------|-------------|
| `UNPAID` | Sin pago aprobado aún. |
| `PAID` | Pago aprobado y confirmado por Wompi. |

| `transactionStatus` (Wompi) | Significado |
|-----------------------------|-------------|
| `PENDING` | Enlace generado, aún sin pagar / en proceso. |
| `APPROVED` | Pago aprobado → la cita pasa a `PAID`. |
| `DECLINED` | Pago rechazado. |
| `VOIDED` | Pago anulado. |
| `ERROR` | Error en el pago. |

---

## 8. Notas

- Los montos en pesos (`amountCop`, `priceCop`) van **sin decimales** (pesos enteros);
  `amountInCents` es ese valor ×100.
- El enlace vence según `expiresAt`; si venció, genera uno nuevo con `POST .../payment-link`.
- Un enlace es de **un solo uso** (acepta una sola transacción aprobada).

---

## 9. Widget / Checkout Web embebido (uso de la llave pública)

Hay **dos formas** de cobrar, y usan la llave pública de forma distinta:

- **Flujo de enlaces (§3–§6):** el frontend **no usa ninguna llave**. Solo abre el
  `checkoutUrl` que devuelve el backend. Es el más simple y el recomendado por defecto.
- **Flujo embebido (esta sección):** el pago se renderiza **dentro de tu propia página**
  con el Widget o el Checkout Web de Wompi. Aquí **sí** se usa la llave pública en el
  cliente. **Ya está soportado por el backend** vía `POST .../checkout-config` (§9.2).

> Los **cuatro** secretos de Wompi y dónde vive cada uno (no confundirlos):
> | Secreto | Dónde vive | Para qué |
> |---|---|---|
> | **Llave privada** (`prv_...`) | Solo backend | Crear enlaces de pago. **Nunca** al frontend. |
> | **Events secret** (`test_events_...`) | Solo backend | Verificar la firma de los webhooks. **Nunca** al frontend. |
> | **Integrity secret** (`test_integrity_...`) | Solo backend | Firmar el Widget/Checkout Web. **Nunca** al frontend. |
> | **Llave pública** (`pub_...`) | **Frontend** (segura de exponer) | Identificar el comercio en el Widget/Checkout Web. |
>
> La llave pública **está diseñada para ir en el cliente** (como la publishable key de
> Stripe). La firma de integridad la calcula **siempre el backend** con el integrity
> secret; el frontend solo la reenvía.

### 9.1 ¿Quién llama a este endpoint?

Igual que el resto de §1–§8, `POST .../checkout-config` requiere **JWT** (es parte del
panel admin/doctor autenticado). Es decir, hoy el flujo embebido asume que el Widget se
renderiza en una pantalla autenticada. Si en el futuro quieres embeber el Widget en una
página **pública del paciente** (sin login), hay que cambiar la autenticación de este
endpoint; avísanos y lo ajustamos.

### 9.2 Obtener la configuración del checkout

#### `POST /api/appointments/{id}/checkout-config`
Devuelve todo lo que el Widget/Checkout Web necesita para esa cita, con la **firma ya
calculada**. Genera una **referencia única** por llamada (si recargas, pide una nueva).

**Response `201`**
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Configuración de checkout generada satisfactoriamente",
  "items": {
    "appointmentId": 123,
    "publicKey": "pub_test_l7AOLC4njSFxrDc9ij1D2CV4hFUWFMI3",
    "currency": "COP",
    "amountInCents": 82010000,
    "reference": "CITA-123-9f2c8a1b4d7e4f0aa1b2c3d4e5f60718",
    "signatureIntegrity": "a1b2c3…（64 hex en minúscula）",
    "redirectUrl": "https://app.confirmamed.com/pago/resultado",
    "amountUsd": 200.00,
    "trm": 4100.50,
    "amountCop": 820100
  }
}
```

| Campo | Uso |
|-------|-----|
| `publicKey` | `data-public-key` / `public-key`. |
| `currency` | Siempre `COP`. |
| `amountInCents` | Monto a cobrar (COP ×100). |
| `reference` | Referencia única del pago; va tal cual al Widget. |
| `signatureIntegrity` | `signature:integrity`. **No la recalcules en el frontend.** |
| `redirectUrl` | A dónde vuelve el paciente tras pagar. |
| `amountUsd` / `trm` / `amountCop` | Solo para mostrar en la UI. |

**Errores**
- `400` — *"La cita no tiene precio configurado…"* (ponle precio al combo primero, §2),
  o Wompi sin configurar (falta la llave pública o el integrity secret en el backend).
- `404` — la cita no existe.
- `503` — no se pudo obtener la TRM, o la BD falló.

### 9.3 Renderizar el pago con esos valores

**Opción A — Widget (botón que abre un modal):**
```html
<form>
  <script
    src="https://checkout.wompi.co/widget.js"
    data-render="button"
    data-public-key="{publicKey}"
    data-currency="{currency}"
    data-amount-in-cents="{amountInCents}"
    data-reference="{reference}"
    data-signature:integrity="{signatureIntegrity}"
    data-redirect-url="{redirectUrl}">
  </script>
</form>
```

**Opción B — Checkout Web (redirección por URL):**
```
https://checkout.wompi.co/p/?public-key={publicKey}
  &currency={currency}
  &amount-in-cents={amountInCents}
  &reference={reference}
  &signature:integrity={signatureIntegrity}
  &redirect-url={redirectUrl}
```

> Todos los `{...}` salen tal cual de la respuesta de §9.2. El frontend **no** calcula
> ni modifica `amountInCents`, `reference` ni `signatureIntegrity` (si cambias cualquiera
> de esos tres, Wompi rechaza la transacción por firma inválida).

### 9.4 Confirmación (igual que el flujo de enlaces)

La confirmación sigue siendo **asíncrona por webhook**. El backend correlaciona la
transacción por la `reference` (no por `payment_link_id`) y, al quedar `APPROVED`, marca
la cita como `PAID`. Para reflejarlo en la UI, haz **polling** a `GET .../payment` (§5)
igual que en el flujo de enlaces.

### Recomendación

Para pacientes del extranjero y por simplicidad, el **flujo de enlaces** (§3–§6) sigue
siendo el camino más sencillo (cero llaves en el frontend). Usa el **flujo embebido**
(§9) cuando quieras el formulario de pago dentro de la propia app; el backend ya entrega
todo firmado, así que el frontend solo pinta el Widget con los valores de §9.2.
