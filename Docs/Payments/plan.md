# Documentación técnica — Pagos de citas con Wompi

> Cobro de las citas mediante **enlaces de pago de Wompi**, pensado para pacientes del
> extranjero (Aruba, Caribe, etc.) que pagan con su tarjeta internacional. El precio se
> administra en **USD** por combinación doctor+especialidad y se cobra en **COP** usando
> la **TRM oficial** del día.

Guía para el frontend: [`payments-frontend.md`](./payments-frontend.md).
Referencia de Wompi: [enlaces de pago](https://docs.wompi.co/en/docs/colombia/links-de-pago/) ·
[eventos/webhooks](https://docs.wompi.co/en/docs/colombia/eventos/).

---

## 1. Contexto

- **Actor con plataforma:** admin/doctor (genera enlaces y administra precios desde el panel).
- **Actor sin plataforma:** el paciente, que solo abre el `checkoutUrl` (por email o el canal que el admin prefiera) y paga en la pantalla de Wompi.
- **Pasarela:** [Wompi Colombia](https://wompi.com/es/co/) (Bancolombia) vía REST API. No hay SDK oficial .NET → se consume por `HttpClient`, igual que Daily.
- **Restricción de moneda (clave):** Wompi acepta tarjetas internacionales pero **solo liquida en COP**. No se puede cobrar en USD de forma nativa. El precio se piensa/muestra en USD y se convierte a COP al generar el enlace; el banco del paciente hace la conversión a su moneda.

### Decisiones de diseño
- Precio por **doctor + especialidad** en USD (dos doctores pueden cobrar distinto por la misma especialidad).
- Conversión USD→COP con la **TRM oficial vía API** (datos.gov.co, dataset Superfinanciera), cacheada a diario.
- Entrega del enlace: **endpoint que devuelve la URL** + **envío opcional por email** (Bird).
- Confirmación: **webhook** de Wompi con **verificación de firma**.

### Fuera de alcance en esta iteración
- Reembolsos/anulaciones desde el panel, cuotas, impuestos (IVA) y multi-moneda nativa (requeriría otra pasarela como Stripe/PayU).

---

## 2. Requerimientos funcionales

| # | Requerimiento | Notas técnicas |
|---|--------------|----------------|
| RF-1 | Cada doctor define un precio por especialidad (USD). | Columna `price_usd` en `doctors_has_specialities`; endpoint propio `PUT /price`. |
| RF-2 | Al crear la agenda, la cita hereda el precio de su combo doctor+especialidad. | `UPDATE` de `appointments.price_usd` tras `create_diary` (defensivo). |
| RF-3 | Generar un enlace de pago Wompi para una cita. | `POST /appointments/{id}/payment-link`; `single_use=true`, `currency=COP`. |
| RF-4 | El monto se cobra en COP convertido desde USD con la TRM del día. | `amount_in_cents = round(price_usd × TRM) × 100`. |
| RF-5 | El enlace se puede enviar por correo al paciente. | `sendEmail: true` → plantilla `payment-email.html` + Bird. |
| RF-6 | Confirmar el pago automáticamente. | Webhook `POST /webhooks/wompi`; al `APPROVED` la cita pasa a `PAID`. |
| RF-7 | Consultar valores y estado de pago de la cita. | `GET /appointments/{id}/pricing` y `GET /appointments/{id}/payment`. |

### Requerimientos no funcionales
- La asignación del precio al crear la agenda **no debe romper** la creación si la columna aún no existe → `try/catch` defensivo.
- Las llaves de Wompi (privada, pública, events secret) **nunca** se exponen en frontend ni en respuestas HTTP.
- El webhook **verifica la firma** de cada evento y responde `200` para no disparar reintentos innecesarios.
- La TRM se cachea para no depender de datos.gov.co en cada cobro.

---

## 3. Diseño

### 3.1 Modelo de datos

Script idempotente: [`Persistence/Sql/001_wompi_payments.sql`](../../Persistence/Sql/001_wompi_payments.sql).

**`doctors_has_specialities`:** `price_usd NUMERIC(12,2)` — precio de la consulta (USD) de ese doctor en esa especialidad.

**`appointments`:**
- `price_usd NUMERIC(12,2)` — foto del precio al crear la agenda.
- `payment_status VARCHAR(20) DEFAULT 'UNPAID'` — `UNPAID` | `PAID`.

**Nueva tabla `appointment_payments`** — un registro por enlace generado + resultado de la transacción:

```sql
id, appointment_id, wompi_payment_link_id, checkout_url,
amount_usd, trm_used, amount_in_cents, currency,
wompi_transaction_id, wompi_reference, status,     -- PENDING/APPROVED/DECLINED/VOIDED/ERROR
expires_at, paid_at, created_at, updated_at
```
Índice único parcial sobre `wompi_payment_link_id` (correlaciona el webhook con el pago).

### 3.2 Componentes

| Pieza | Archivo | Responsabilidad |
|-------|---------|-----------------|
| Wompi API | [`Services/Wompi/WompiService.cs`](../../Services/Wompi/WompiService.cs) | Crear enlace (llave privada), armar `checkoutUrl`, verificar firma del webhook. |
| TRM | [`Services/Trm/TrmService.cs`](../../Services/Trm/TrmService.cs) | TRM oficial USD→COP con `IMemoryCache` + tasa de respaldo opcional. |
| Orquestación | [`Services/Payments/AppointmentPaymentService.cs`](../../Services/Payments/AppointmentPaymentService.cs) | Precio→TRM→COP→Wompi→persistir→(email); procesar webhook; pricing/estado. |
| Persistencia | [`Repositories/Payments/AppointmentPaymentRepository.cs`](../../Repositories/Payments/AppointmentPaymentRepository.cs) | CRUD de pagos; actualizar estado y marcar cita `PAID`. |
| Precios | [`...DoctorsHasSpecialities/DoctorHasSpecialityRepository.cs`](../../Repositories/DoctorsHasSpecialities/DoctorHasSpecialityRepository.cs) | `SetPrice` / `GetPrices` / `ExistsRelation`. |
| Endpoints cita | [`Controllers/Appointments/AppointmentsController.cs`](../../Controllers/Appointments/AppointmentsController.cs) | `payment-link`, `pricing`, `payment`. |
| Endpoints precio | [`...DoctorsHasSpecialitiesController.cs`](../../Controllers/DoctorsHasSpecialities/DoctorsHasSpecialitiesController.cs) | `PUT /price`, `GET /prices`. |
| Webhook | [`Controllers/Webhooks/WebhooksController.cs`](../../Controllers/Webhooks/WebhooksController.cs) | `POST /webhooks/wompi` (anónimo, lee body crudo). |
| Email | [`Helpers/PaymentEmailTemplateHelper.cs`](../../Helpers/PaymentEmailTemplateHelper.cs) + `Templates/payment-email.html` | Correo con el enlace de pago. |
| Config | [`Entities/Wompi/WompiSettings.cs`](../../Entities/Wompi/WompiSettings.cs), [`Entities/Trm/TrmSettings.cs`](../../Entities/Trm/TrmSettings.cs) + `Program.cs` | `HttpClient` tipados + `IOptions` + `AddMemoryCache`. |

### 3.3 Flujo: generar enlace (`POST /appointments/{id}/payment-link`)

1. Cargar la cita y su precio (`price_usd` de la cita, o del combo doctor+especialidad como respaldo). Sin precio → `400`.
2. Obtener la TRM del día (`TrmService`).
3. `pesos = round(price_usd × TRM)`; `amount_in_cents = pesos × 100`.
4. `WompiService.CreatePaymentLinkAsync(...)` con la llave privada → `paymentLinkId`.
5. `checkoutUrl = https://checkout.wompi.co/l/{paymentLinkId}`.
6. Persistir el pago en `appointment_payments` (estado `PENDING`, guardando `amount_usd`, `trm_used`, `amount_in_cents`).
7. Si `sendEmail` → render de `payment-email.html` + envío por Bird.

### 3.4 Flujo: webhook + verificación de firma

1. Wompi hace `POST` al events URL (`/api/webhooks/wompi`) con el evento `transaction.updated`.
2. Se lee el **body crudo** y el header `X-Event-Checksum`.
3. **Verificación:** `SHA256( concat(valores de signature.properties desde data, en orden) + timestamp + EventsSecret )` y se compara con el checksum (comparación en tiempo constante). Firma inválida → se ignora (se responde `200`).
4. Si `transaction.updated` trae `payment_link_id`, se busca el pago y se actualiza su `status`; si es `APPROVED`, se marca la cita como `PAID` y se guarda `paid_at` (en una transacción).
5. Se responde `200` siempre que el evento se reciba (para no provocar reintentos).

### 3.5 Conversión USD→COP (TRM)

- Fuente: dataset oficial de la TRM en datos.gov.co (`32sa-8pi3`), último registro vigente.
- Se cachea `CacheHours` (por defecto 12h) en `IMemoryCache`.
- `FallbackRate` opcional: si la API falla y hay respaldo configurado, se usa; si no, `503`.

---

## 4. Configuración y secretos

`appsettings` (con fallback a variables de entorno `Wompi__*` / `Trm__*`):

```jsonc
"Wompi": {
  "BaseUrl": "https://sandbox.wompi.co/v1/",   // prod: https://production.wompi.co/v1/
  "PublicKey": "",
  "PrivateKey": "",                             // crea enlaces
  "EventsSecret": "",                           // verifica webhooks
  "RedirectUrl": "https://app.confirmamed.com/pago/resultado",
  "ExpirationHours": 72,
  "CheckoutBaseUrl": "https://checkout.wompi.co/l/"
},
"Trm": {
  "BaseUrl": "https://www.datos.gov.co/resource/32sa-8pi3.json",
  "CacheHours": 12,
  "FallbackRate": 0
}
```

> Las llaves van en `appsettings.Development.json` (local) o variables de entorno en Cloud Run (prod). **Nunca** en el repo ni en el chat. El `EventsSecret` es distinto de las llaves pública/privada.

---

## 5. Puesta en marcha (checklist)

1. **Aplicar el SQL** [`001_wompi_payments.sql`](../../Persistence/Sql/001_wompi_payments.sql) al branch `develop` de Neon (y luego a `staging`/`production`).
2. **Cargar las llaves** de Wompi (`PublicKey`, `PrivateKey`, `EventsSecret`) y ajustar `RedirectUrl` al frontend real. Empezar en **sandbox**.
3. En el **dashboard de Wompi** → configurar la **URL de eventos** apuntando a `https://<tu-dominio>/api/webhooks/wompi` (sandbox y producción por separado).
4. **Fijar precios** por doctor+especialidad (`PUT /api/DoctorsHasSpecialities/price`).
5. Probar el flujo completo en sandbox y luego cambiar `BaseUrl` + llaves a producción.

---

## 6. Endpoints (resumen)

| Método | Ruta | Uso |
|--------|------|-----|
| `PUT` | `/api/DoctorsHasSpecialities/price` | Fijar precio USD de un combo doctor+especialidad. |
| `GET` | `/api/DoctorsHasSpecialities/prices` | Listar precios (filtros `doctorId`, `specialityId`). |
| `POST` | `/api/appointments/{id}/payment-link` | Generar enlace (`{ sendEmail }`). |
| `GET` | `/api/appointments/{id}/pricing` | Valores USD + COP (TRM). |
| `GET` | `/api/appointments/{id}/payment` | Estado de pago. |
| `POST` | `/api/webhooks/wompi` | Webhook de Wompi (interno, anónimo). |

Detalle de request/response: [`payments-frontend.md`](./payments-frontend.md).

---

## 7. Pruebas (sandbox)

- Usar `BaseUrl` sandbox + llaves de prueba de Wompi.
- Generar un enlace para una cita con precio y abrir el `checkoutUrl`.
- Pagar con las **tarjetas de prueba** de Wompi y verificar que llega el webhook y la cita pasa a `PAID` (`GET /payment`).
- El webhook debe ser **públicamente accesible** (URL de Cloud Run) y estar registrado en el dashboard.

---

## 8. Estado actual / pendientes (al 2026-11-02)

- Código completo y **compila** (net10.0). Sin probar end-to-end (requiere BD + llaves).
- **SQL sin aplicar**: el proyecto Neon `ConfirmaMed` estuvo sobre cuota; aplicar `001_wompi_payments.sql` al branch `develop`.
- Faltan las llaves de Wompi y la configuración de la URL de eventos en el dashboard.
