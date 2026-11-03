# Anexos de cita — Guía de integración Frontend

Los **anexos** son **documentos** (exámenes, órdenes, resultados…) que se adjuntan a
una cita. Los puede subir el **doctor** desde el panel o el **paciente** desde la
teleconsulta. Cada anexo se guarda en Cloudinary y queda asociado a la cita (y al
paciente de esa cita).

> Migración de BD relacionada: [`01-patient-id.sql`](./01-patient-id.sql) (agrega
> `patient_id` a los anexos). Aplicada en el branch `develop`.

---

## 0. Convenciones

**Autenticación:** estos endpoints son **públicos** (no requieren JWT). El `POST` es
`[AllowAnonymous]` a propósito, porque el **paciente** (que no tiene plataforma) sube
documentos desde la teleconsulta. Rate limit: `ClientPolicy`.

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

## 1. Subir anexos

### `POST /api/AppointmentAnnexes/appointments/{appointmentId}/annexes`

Sube **uno o varios** documentos a la cita. `Content-Type: multipart/form-data`, con el
campo **`files`** (repetido por cada archivo).

**Request (multipart/form-data)**

| Campo | Tipo | Notas |
|-------|------|-------|
| `files` | archivo(s) | Uno o más; el mismo nombre de campo `files` por cada archivo. |

```bash
curl -X POST "https://<host>/api/AppointmentAnnexes/appointments/123/annexes" \
  -F "files=@examen-sangre.pdf" \
  -F "files=@orden-medica.pdf"
```

```js
const fd = new FormData();
for (const f of fileList) fd.append("files", f);   // mismo nombre "files"
await fetch(`/api/AppointmentAnnexes/appointments/${appointmentId}/annexes`, {
  method: "POST",
  body: fd,            // NO pongas Content-Type a mano; el navegador lo setea con el boundary
});
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

**Response `201`** — ⚠️ devuelve **la cita** (no la lista de anexos). Tras subir, si
necesitas mostrar los anexos actualizados, llama al `GET` (§2).

```json
{
  "success": true,
  "statusCode": 201,
  "message": "Recurso creado satisfactoriamente",
  "items": {
    "id": 123,
    "dateAppointment": "2026-11-05",
    "startHour": "09:00:00",
    "endHour": "09:30:00",
    "doctor": { "...": "..." },
    "speciality": { "...": "..." },
    "patient": { "...": "..." },
    "state": { "...": "..." }
  }
}
```

---

## 2. Listar anexos de una cita

### `GET /appointments/{appointmentId}`  *(ruta absoluta, sin `/api`)*

**Response `200`** — arreglo de anexos:

```json
{
  "success": true,
  "statusCode": 200,
  "items": [
    {
      "id": 7,
      "file": "https://res.cloudinary.com/.../appointments/annexes/CC123/123/examen-sangre_ab12.pdf",
      "name": "examen-sangre.pdf",
      "patientId": 45,
      "appointment": {
        "id": 123,
        "dateAppointment": "2026-11-05",
        "startHour": "09:00:00"
      }
    }
  ]
}
```

| Campo | Significado |
|-------|-------------|
| `file` | URL (segura) del documento en Cloudinary. Es el enlace para ver/descargar. |
| `name` | Nombre original del archivo subido. |
| `patientId` | Paciente de la cita (puede ser `null` si la cita aún no tenía paciente). |
| `appointment` | Datos mínimos de la cita (`id`, `dateAppointment`, `startHour`). |

---

## 3. Flujo recomendado en el frontend

1. En la vista de la cita, subir documentos con el `POST` (§1), permitiendo selección múltiple.
2. Al terminar, refrescar la lista con el `GET` (§2) para mostrar los anexos (con su enlace `file`).
3. Para ver/descargar un anexo, abrir directamente la URL `file` (Cloudinary).

---

## 4. Detalles técnicos (referencia)

- **Almacenamiento:** cada archivo se sube a Cloudinary como *raw document*, en la carpeta
  `appointments/annexes/{documentoPaciente}/{appointmentId}` (si la cita no tiene paciente,
  usa `sin-documento`). El nombre se hace único (`UniqueFilename`), por eso el `file` trae un sufijo.
- **`patient_id`:** al crear el anexo se guarda el `patient_id` de la cita (lo suba el doctor o el
  paciente). Columna nullable en `appointments_annexes`.
- **Base de datos:** funciones `create_appointment_annex(file, name, appointment_id, patient_id)` y
  `get_all_appointments_annexes(appointment_id)` (ver [`01-patient-id.sql`](./01-patient-id.sql)).
- **Código:** [`Controllers/AppointmentsAnnexes/AppointmentAnnexesController.cs`](../../Controllers/AppointmentsAnnexes/AppointmentAnnexesController.cs),
  [`Services/AppointmentsAnnexes/AppointmentAnnexService.cs`](../../Services/AppointmentsAnnexes/AppointmentAnnexService.cs).

---

## 5. Notas / gotchas

- Las dos rutas **no comparten prefijo**: subir va bajo `api/AppointmentAnnexes/...` y listar es
  `/appointments/{id}` (sin `/api`). Úsalas tal cual.
- El `POST` responde con **la cita**, no con los anexos recién creados → usa el `GET` para refrescar.
- No existe (todavía) endpoint para **eliminar** un anexo; si se necesita, hay que agregarlo.
