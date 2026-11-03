# Doctores — Descripción, foto y hoja de vida

Documentación de los cambios hechos para las funcionalidades de doctores
solicitadas para la parte del paciente.

- **Guía de integración Frontend:** [`doctors-frontend.md`](./doctors-frontend.md)
- **Migración de BD:** [`01-description-photo.sql`](./01-description-photo.sql)

---

## 1. Resumen

Se agregaron tres capacidades sobre doctores:

1. **Listado de doctores por especialidad** (para el paciente). Ya existía el
   endpoint; ahora cada doctor incluye `description` y `photoUrl`.
2. **Descripción** (texto, opcional) al **crear/actualizar** un doctor, y la
   posibilidad de **agregar una foto** (opcional) mediante un endpoint aparte.
   El `GET por id` también devuelve ambos campos.
3. **Hoja de vida** del doctor: datos + descripción + foto + sus especialidades.

Decisiones tomadas (acordadas contigo):

- La **foto** se maneja en un **endpoint aparte** (`multipart/form-data`),
  siguiendo el patrón del avatar de usuarios. No se cambió el contrato JSON de
  crear/actualizar (solo se le agregó `description`).
- La migración de BD se **aplicó al branch `develop`** de Neon.
- La **hoja de vida** y el **listado por especialidad** quedaron **públicos**
  (`[AllowAnonymous]`), al ser para la parte del paciente.

---

## 2. Cambios en base de datos

Archivo: [`01-description-photo.sql`](./01-description-photo.sql).
**Estado: aplicado en `develop`. Pendiente en `staging` y `production`.**

### Tabla `doctors`
Se agregaron dos columnas (ambas nulas / opcionales):

| Columna       | Tipo   | Notas                                       |
|---------------|--------|---------------------------------------------|
| `description` | `text` | Descripción / hoja de vida del doctor       |
| `photo_url`   | `text` | URL de la foto en Cloudinary                |

### Funciones
Las tres funciones de lectura cambian su tabla de retorno, por eso se hizo
`DROP` + `CREATE` (CREATE OR REPLACE no permite cambiar el tipo de retorno):

| Función                       | Cambio                                                                 |
|-------------------------------|------------------------------------------------------------------------|
| `create_doctor`               | Agrega parámetro `p_description` (al final, con default `null`)         |
| `update_doctor`               | Agrega parámetro `p_description` (al final, con default `null`)         |
| `get_doctor_by_id`            | Devuelve además `description`, `photourl`                              |
| `get_all_doctors`             | Devuelve además `description`, `photourl`                              |
| `get_doctors_by_speciality`   | Devuelve además `status`, `description`, `photourl`                    |
| `update_doctor_photo` *(nueva)* | `(p_id, p_photo_url)` → actualiza solo la foto                        |
| `get_specialities_by_doctor` *(nueva)* | `(p_doctor_id)` → especialidades del doctor (para la hoja de vida) |

> Nota sobre el nombrado de columnas: las funciones exponen los alias sin guion
> bajo (`photourl`, `documenttypeid`) para que Dapper los mapee a las
> propiedades C# (`PhotoUrl`, `DocumentTypeId`) por coincidencia de nombre
> insensible a mayúsculas, igual que el resto del proyecto.

---

## 3. Cambios en el código (.NET)

### Entidad
- `Entities/Doctors/Doctor.cs` — nuevas propiedades `Description` y `PhotoUrl` (nullable).

### DTOs
- `DTOs/Doctors/Requests/DoctorRequestCreateDto.cs` — agrega `Description`.
- `DTOs/Doctors/Requests/DoctorRequestUpdateDto.cs` — agrega `Description`.
- `DTOs/Doctors/Responses/DoctorFlatDto.cs` — agrega `Description`, `PhotoUrl`.
- `DTOs/Doctors/Responses/DoctorResponseDto.cs` — agrega `Description`, `PhotoUrl`.
- `DTOs/Doctors/Requests/DoctorPhotoRequestDto.cs` *(nuevo)* — `IFormFile? File` para la subida de foto.
- `DTOs/Doctors/Responses/DoctorProfileResponseDto.cs` *(nuevo)* — hereda de
  `DoctorResponseDto` y agrega `Specialities` (la hoja de vida).

### Repositorio
- `Repositories/Doctors/IDoctorRepository.cs` / `DoctorRepository.cs`:
  - `CreateAsync` / `UpdateAsync` ahora pasan `@Description` a las funciones.
  - `UpdatePhotoAsync(id, photoUrl)` *(nuevo)* → `update_doctor_photo`.
  - `GetSpecialitiesByDoctorAsync(doctorId)` *(nuevo)* → `get_specialities_by_doctor`
    (devuelve `IEnumerable<SpecialityResponseDto>`).

### Servicio
- `Services/Doctors/IDoctorService.cs` / `DoctorService.cs`:
  - Se inyecta `ICloudinaryService` y el validador `IValidator<DoctorPhotoRequestDto>`.
  - `GetProfileAsync(id)` *(nuevo)* → arma la hoja de vida (doctor + especialidades).
  - `UpdatePhotoAsync(id, dto)` *(nuevo)* → valida, sube la imagen a Cloudinary
    (carpeta `doctors/photos`) y persiste `photo_url`.

### Controlador
- `Controllers/Doctors/DoctorsController.cs`:
  - `GET  {id}/profile` *(nuevo, `[AllowAnonymous]`)* → hoja de vida.
  - `PUT  {id}/photo`   *(nuevo, `multipart/form-data`)* → sube/cambia la foto.

### Mapping
- `MappingProfile/BackendProfile.cs` — nuevo `CreateMap<DoctorFlatDto, DoctorProfileResponseDto>`
  (mapea `DocumentType` e ignora `Specialities`, que se asigna en el servicio).

### Validadores
- `Validators/Doctors/DoctorRequestCreateDtoValidator.cs` /
  `DoctorRequestUpdateDtoValidator.cs` — `Description` con máx. 1000 caracteres.
- `Validators/Doctors/DoctorPhotoRequestDtoValidator.cs` *(nuevo)* — exige archivo,
  imagen JPEG/PNG/GIF/WebP, máx. 5 MB (se auto-registra vía `AddValidatorsFromAssembly`).

---

## 4. Endpoints

| Acción                           | Método | Ruta                                              | Auth    |
|----------------------------------|--------|---------------------------------------------------|---------|
| Doctores por especialidad        | GET    | `/api/DoctorsHasSpecialities/specialities/{id}`   | Pública |
| Hoja de vida                     | GET    | `/api/Doctors/{id}/profile`                       | Pública |
| Detalle por id (+descr/foto)     | GET    | `/api/Doctors/{id}`                               | JWT     |
| Crear doctor (+descripción)      | POST   | `/api/Doctors`                                    | JWT     |
| Actualizar doctor (+descripción) | PUT    | `/api/Doctors`                                    | JWT     |
| Subir/cambiar foto               | PUT    | `/api/Doctors/{id}/photo` (multipart)             | JWT     |

Los contratos de request/response con ejemplos están en
[`doctors-frontend.md`](./doctors-frontend.md).

---

## 5. Pruebas realizadas

Se probó end-to-end contra el backend corriendo en local (`http://localhost:5050`,
`Development` → BD `develop`), autenticado como `delcruz_` (rol `admin`):

| Caso                                                        | Resultado |
|-------------------------------------------------------------|-----------|
| `GET /api/Doctors/3/profile` (hoja de vida, pública)        | 200 — datos + especialidades + descripción/foto |
| `GET /api/DoctorsHasSpecialities/specialities/7` (pública)  | 200 — doctores con `description`/`photoUrl` |
| `POST /api/Doctors` con `description`                       | 201 — descripción persistida |
| `PUT /api/Doctors` cambiando `description`                  | 200 — descripción actualizada |
| `GET /api/Doctors/{id}`                                     | 200 — descripción correcta |
| `PUT /api/Doctors/{id}/photo` (multipart)                   | 200 — subió a Cloudinary y guardó `photoUrl` |
| `PUT /api/Doctors/{id}/photo` sin archivo                   | 400 — "Debes enviar un archivo de imagen" |
| `GET /api/Doctors/999999/profile`                           | 404 |

Los datos de prueba (doctor temporal + imagen en Cloudinary) se eliminaron al terminar.

---

## 6. Pendientes

- [ ] Aplicar [`01-description-photo.sql`](./01-description-photo.sql) en `staging` y `production`.
- [ ] Commit de los cambios de código (aún sin commitear).
- [ ] (Opcional) Si la hoja de vida no debe ser pública, quitar `[AllowAnonymous]`
      del método `GetProfile` en `DoctorsController`.
