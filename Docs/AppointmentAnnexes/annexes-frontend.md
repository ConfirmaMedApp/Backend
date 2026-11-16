# Anexos de cita — Guía de integración Frontend

Los **anexos** son **documentos** (exámenes, órdenes, resultados…) que se adjuntan a
una cita. Los puede subir el **doctor** desde el panel o el **paciente** desde la
teleconsulta. Cada anexo se guarda en Cloudinary y queda asociado a la cita (al
paciente de esa cita y a quién lo subió).

> ✅ **Verificado end-to-end** contra `develop` (subida real de paciente y de doctor +
> listado). Ver §6.

---

## 0. Los dos escenarios (lo importante)

El `appointmentId` siempre se conoce desde el **link de la consulta**, así que el anexo
**siempre se guarda con el id de la cita**. Lo que cambia es **quién lo sube**:

| Escenario | ¿Manda `Authorization: Bearer`? | `userId` guardado |
|-----------|:---:|:---:|
| **Paciente** — desde la teleconsulta (no tiene cuenta) | **No** | **`null`** |
| **Doctor / staff** — desde el panel (autenticado) | **Sí** | su `userId` (del token) |

> 🔑 **El `userId` se toma del JWT, NO se manda como campo del formulario.** Si la
> petición lleva token, se guarda el `userId` de ese usuario; si no lleva, queda `null`.
> Esto es a propósito: en un endpoint público no se podría confiar en un `userId` que
> mande el cliente. **Conclusión para el front:** el paciente sube sin hacer nada
> especial; para que quede registrado el doctor, el panel debe mandar el header
> `Authorization: Bearer <token>` en el `POST` (como en el resto de llamadas del panel).

---

## 1. Convenciones

**Autenticación:** estos endpoints son **públicos** (no requieren JWT) — tanto subir
como listar. El `POST` es `[AllowAnonymous]` a propósito, porque el **paciente** sube
documentos desde la teleconsulta. Enviar el token es **opcional** y solo sirve para
registrar el `userId` del que sube. Rate limit: `ClientPolicy`.

**Envelope de respuesta.** Éxito en `camelCase`, datos en `items`:

```json
{ "success": true, "statusCode": 200, "message": "...", "path": "...", "responseTime": "...", "items": {} }
```

> ⚠️ Los **errores** salen en `PascalCase` (`Success`, `StatusCode`, `Message`,
> `Items: null`). Maneja ambos casos.

> ⚠️ **Rutas (ojo, son distintas entre sí):**
> - Subir: `POST /api/AppointmentAnnexes/appointments/{appointmentId}/annexes`
> - Listar: `GET /appointments/{appointmentId}` — **ruta absoluta, sin el prefijo `/api`**.
>
> Son las rutas efectivas tal como están hoy en el backend; úsalas literalmente.

---

## 2. Subir anexos

### `POST /api/AppointmentAnnexes/appointments/{appointmentId}/annexes`

Sube **uno o varios** documentos a la cita. `Content-Type: multipart/form-data`, con el
campo **`files`** (repetido por cada archivo).

**Request (multipart/form-data)**

| Campo | Tipo | Notas |
|-------|------|-------|
| `files` | archivo(s) | Uno o más; el mismo nombre de campo `files` por cada archivo. |

**Caso paciente — teleconsulta (sin token):**

```js
const fd = new FormData();
for (const f of fileList) fd.append("files", f);   // mismo nombre "files"

await fetch(`/api/AppointmentAnnexes/appointments/${appointmentId}/annexes`, {
  method: "POST",
  body: fd,            // NO pongas Content-Type a mano; el navegador lo setea con el boundary
});                    // sin Authorization -> el anexo queda con userId: null
```

**Caso doctor — panel (con token):**

```js
const fd = new FormData();
for (const f of fileList) fd.append("files", f);

await fetch(`/api/AppointmentAnnexes/appointments/${appointmentId}/annexes`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },   // <- así queda registrado su userId
  body: fd,
});
```

```bash
# Equivalente con curl (paciente, sin token)
curl -X POST "https://<host>/api/AppointmentAnnexes/appointments/123/annexes" \
  -F "files=@examen-sangre.pdf" \
  -F "files=@orden-medica.pdf"
```

**Tipos de archivo permitidos** (se valida por `Content-Type`; archivo vacío → error):

| Tipo | Content-Type |
|------|--------------|
| PDF | `application/pdf` |
| Word | `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` |
| Excel | `application/vnd.ms-excel`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| Texto | `text/plain` |

> No se aceptan imágenes en este endpoint; son documentos. Un tipo no permitido o un
> archivo vacío devuelve `400` con el mensaje correspondiente.

**Response `201`** — ⚠️ devuelve **la cita** (no la lista de anexos ni el anexo creado).
Tras subir, para mostrar los anexos actualizados, llama al `GET` (§3).

```json
{
  "success": true,
  "statusCode": 201,
  "message": "Recurso creado satisfactoriamente",
  "items": {
    "id": 1174,
    "dateAppointment": "2026-11-10",
    "startHour": "17:30:00",
    "endHour": "18:00:00",
    "doctor":     { "id": 3, "name": "...", "lastName": "...", "document": "..." },
    "speciality": { "id": 2, "name": "...", "code": "..." },
    "patient":    { "id": 6, "name": "...", "lastname": "...", "document": "..." },
    "state":      { "id": 5, "code": "cancelada", "name": "Cancelada", "color": "#ef4444" },
    "userId": 2
  }
}
```

> ⚠️ Ojo: ese `userId` del `items` es el usuario **de la cita** (su dueño/creador), **no**
> el que subió el anexo. El `userId` del anexo se ve en el `GET` (§3).

---

## 3. Listar anexos de una cita

### `GET /appointments/{appointmentId}`  *(ruta absoluta, sin `/api`)*

Público. El token es opcional (no cambia el resultado).

**Response `200`** — arreglo de anexos:

```json
{
  "success": true,
  "statusCode": 200,
  "items": [
    {
      "id": 6,
      "file": "https://res.cloudinary.com/.../appointments/annexes/8786105/1174/examen-sangre_uxfdhw.pdf",
      "name": "examen-sangre.pdf",
      "patientId": 6,
      "userId": null,
      "appointment": { "id": 1174, "dateAppointment": "2026-11-10", "startHour": "17:30" }
    },
    {
      "id": 7,
      "file": "https://res.cloudinary.com/.../appointments/annexes/8786105/1174/orden-medica_ubok00.pdf",
      "name": "orden-medica.pdf",
      "patientId": 6,
      "userId": 2,
      "appointment": { "id": 1174, "dateAppointment": "2026-11-10", "startHour": "17:30" }
    }
  ]
}
```

| Campo | Significado |
|-------|-------------|
| `id` | Id del anexo. |
| `file` | URL (segura) del documento en Cloudinary. Es el enlace para ver/descargar. |
| `name` | Nombre original del archivo subido. |
| `patientId` | Paciente de la cita (puede ser `null` si la cita aún no tenía paciente). |
| `userId` | Quién subió el anexo. `null` → lo subió el **paciente** desde la teleconsulta; un número → el `userId` del **doctor/staff** que lo subió con token. |
| `appointment` | Datos mínimos de la cita (`id`, `dateAppointment`, `startHour` en formato `HH:MM`). |

> Idea de UI: si `userId == null` puedes mostrar "Subido por el paciente"; si trae número,
> "Subido por el equipo médico" (o resolver el nombre del usuario con ese id).

---

## 4. Flujo recomendado en el frontend

1. En la vista de la cita / teleconsulta, subir documentos con el `POST` (§2), permitiendo
   selección múltiple. El panel del doctor envía el token; la teleconsulta del paciente no.
2. Al terminar, refrescar la lista con el `GET` (§3) para mostrar los anexos (con su enlace `file`).
3. Para ver/descargar un anexo, abrir directamente la URL `file` (Cloudinary).

---

## 5. Detalles técnicos (referencia)

- **Almacenamiento:** cada archivo se sube a Cloudinary como *raw document*, en la carpeta
  `appointments/annexes/{documentoPaciente}/{appointmentId}` (si la cita no tiene paciente,
  usa `sin-documento`). El nombre se hace único (`UniqueFilename`), por eso el `file` trae un sufijo.
- **`patient_id`:** al crear el anexo se guarda el `patient_id` de la cita (lo suba el doctor o el
  paciente). Columna nullable en `appointments_annexes` (ver [`01-patient-id.sql`](./01-patient-id.sql)).
- **`user_id`:** quién subió el anexo. Se toma de `ICurrentUserService` (el claim del JWT): el
  doctor/staff autenticado guarda su `user_id`; el paciente anónimo queda en `NULL`. Columna nullable
  en `appointments_annexes` (ver [`02-user-id.sql`](./02-user-id.sql)).
- **Base de datos:** funciones `create_appointment_annex(file, name, appointment_id, patient_id, user_id)` y
  `get_all_appointments_annexes(appointment_id)` (ver [`01-patient-id.sql`](./01-patient-id.sql) y
  [`02-user-id.sql`](./02-user-id.sql)).
- **Código:** [`Controllers/AppointmentsAnnexes/AppointmentAnnexesController.cs`](../../Controllers/AppointmentsAnnexes/AppointmentAnnexesController.cs),
  [`Services/AppointmentsAnnexes/AppointmentAnnexService.cs`](../../Services/AppointmentsAnnexes/AppointmentAnnexService.cs).

---

## 6. Estado / verificación

- La migración [`02-user-id.sql`](./02-user-id.sql) **ya está aplicada** en la BD `develop`.
- Prueba real hecha sobre la cita `1174` (paciente 6):
  - Paciente (POST **sin** token) → anexo con `userId: null`. ✅
  - Doctor (POST **con** token) → anexo con `userId: 2`. ✅
  - `GET /appointments/1174` devolvió ambos con su `userId` correcto. ✅
- ⏳ **Pendiente de backend:** redeploy del servicio (Railway) para que el ambiente
  desplegado use la función de 5 argumentos. Hasta ese redeploy, en el ambiente ya
  desplegado el `POST` puede fallar aunque la BD `develop` ya esté migrada.

---

## 7. Notas / gotchas

- Las dos rutas **no comparten prefijo**: subir va bajo `api/AppointmentAnnexes/...` y listar es
  `/appointments/{id}` (sin `/api`). Úsalas tal cual.
- El `POST` responde con **la cita**, no con los anexos recién creados → usa el `GET` para refrescar.
- El `userId` del cuerpo del `POST` es el de la **cita**, no el del anexo. El del anexo está en el `GET`.
- No existe (todavía) endpoint para **eliminar** un anexo; si se necesita, hay que agregarlo.
