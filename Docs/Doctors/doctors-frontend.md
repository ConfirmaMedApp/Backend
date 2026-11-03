# Doctores: descripción, foto y hoja de vida — Guía Frontend

Tres funcionalidades nuevas sobre doctores:

1. **Listado de doctores por especialidad** (para la parte del paciente).
2. **Descripción** (texto) y **foto** (opcional) al crear/actualizar un doctor.
3. **Hoja de vida**: datos del doctor + descripción + foto + especialidades.

> Migración de BD relacionada: [`01-description-photo.sql`](./01-description-photo.sql)
> (agrega `description` y `photo_url` a `doctors`, actualiza las funciones y agrega
> `update_doctor_photo` y `get_specialities_by_doctor`). **Aplicada en el branch `develop`.**
> Falta aplicarla en `staging`/`production` cuando se publique.

---

## 0. Convenciones

**Envelope de respuesta.** Éxito en `camelCase`, datos en `items`:

```json
{ "success": true, "statusCode": 200, "message": "...", "path": "...", "responseTime": "...", "items": {} }
```

> ⚠️ Los **errores** salen en `PascalCase` (`Success`, `StatusCode`, `Message`, `Items: null`). Maneja ambos casos.

**Prefijo:** todos los endpoints cuelgan de `/api`. Rate limit: `UserPolicy`.

---

## 1. Listado de doctores por especialidad (paciente)

Ya existía; ahora **cada doctor incluye `description` y `photoUrl`**. Es **público**
(no requiere JWT), pensado para que el paciente elija doctor dentro de una especialidad.

```
GET /api/DoctorsHasSpecialities/specialities/{specialityId}
```

**Respuesta** (`items` es un arreglo de doctores):

```json
{
  "success": true,
  "statusCode": 200,
  "items": [
    {
      "id": 3,
      "name": "jhon fredy",
      "lastName": "carreño pinto",
      "document": "1099737501",
      "documentType": { "id": 1, "name": "cédula" },
      "email": "jhonfredy2325@gmail.com",
      "description": "Médico general con 10 años de experiencia...",
      "photoUrl": "https://res.cloudinary.com/.../doctors/photos/abc.jpg",
      "status": true
    }
  ]
}
```

`description` y `photoUrl` pueden venir `null` si el doctor aún no los tiene.

---

## 2. Crear / Actualizar doctor con descripción

El `description` es un campo de **texto opcional** (máx. 1000 caracteres) que viaja en
el **JSON** de crear/actualizar. **La foto NO va aquí** (ver sección 3). Estos endpoints
requieren **JWT**.

### Crear

```
POST /api/Doctors
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "ana",
  "lastName": "gómez",
  "document": "123456789",
  "documentTypeId": 1,
  "email": "ana@example.com",
  "description": "Pediatra. Universidad Nacional. Subespecialista en neonatología.",
  "status": true
}
```

### Actualizar

```
PUT /api/Doctors
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "id": 5,
  "name": "ana",
  "lastName": "gómez",
  "document": "123456789",
  "documentTypeId": 1,
  "email": "ana@example.com",
  "description": "Texto actualizado de la hoja de vida.",
  "status": true
}
```

> `description` es opcional: puedes omitirlo o mandarlo `null`. Si lo omites en el
> `PUT`, queda en `null` (el update reemplaza el valor).

La respuesta de ambos es el doctor con `description` y `photoUrl`.

---

## 3. Subir / cambiar la foto del doctor

Endpoint aparte, **multipart/form-data** (igual que el avatar de usuarios). Requiere
**JWT**. La foto es opcional a nivel de doctor, pero si llamas a este endpoint el
archivo **sí** es obligatorio. Se sube a Cloudinary (carpeta `doctors/photos`).

```
PUT /api/Doctors/{id}/photo
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

| Campo  | Tipo | Obligatorio | Reglas                                                        |
|--------|------|-------------|---------------------------------------------------------------|
| `File` | file | sí          | Imagen JPEG/PNG/GIF/WebP, máx. 5 MB                           |

Ejemplo `fetch`:

```js
const form = new FormData();
form.append("File", fileInput.files[0]);

await fetch(`/api/Doctors/${doctorId}/photo`, {
  method: "PUT",
  headers: { Authorization: `Bearer ${token}` }, // NO pongas Content-Type manual
  body: form,
});
```

**Respuesta:** el doctor actualizado con el nuevo `photoUrl`.

> Flujo típico de alta: `POST /api/Doctors` (con `description`) → tomas el `id` de la
> respuesta → `PUT /api/Doctors/{id}/photo` con la imagen.

---

## 4. Hoja de vida del doctor (paciente)

Datos del doctor + descripción + foto + **especialidades**. Es **público**
(`[AllowAnonymous]`), pensado para la ficha del doctor en la parte del paciente.

```
GET /api/Doctors/{id}/profile
```

**Respuesta:**

```json
{
  "success": true,
  "statusCode": 200,
  "items": {
    "id": 3,
    "name": "jhon fredy",
    "lastName": "carreño pinto",
    "document": "1099737501",
    "documentType": { "id": 1, "name": "cédula" },
    "email": "jhonfredy2325@gmail.com",
    "description": "Médico general con 10 años de experiencia...",
    "photoUrl": "https://res.cloudinary.com/.../doctors/photos/abc.jpg",
    "status": true,
    "specialities": [
      { "id": 2, "name": "oftalmología", "description": "", "code": "0993", "status": true },
      { "id": 7, "name": "psicología",   "description": "...", "code": "0994", "status": true }
    ]
  }
}
```

Si el doctor no existe → `404` con el envelope de error.

---

## 5. Resumen de rutas

| Acción                         | Método | Ruta                                                    | Auth      |
|--------------------------------|--------|---------------------------------------------------------|-----------|
| Doctores por especialidad      | GET    | `/api/DoctorsHasSpecialities/specialities/{id}`         | Pública   |
| Crear doctor (+descripción)    | POST   | `/api/Doctors`                                          | JWT       |
| Actualizar doctor (+descripción)| PUT   | `/api/Doctors`                                          | JWT       |
| Subir/cambiar foto             | PUT    | `/api/Doctors/{id}/photo` (multipart)                   | JWT       |
| Hoja de vida                   | GET    | `/api/Doctors/{id}/profile`                             | Pública   |
| Detalle por id (+descr/foto)   | GET    | `/api/Doctors/{id}`                                     | JWT       |
