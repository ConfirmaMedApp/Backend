# Plan de implementación — Integración Daily.co

> Videollamadas 1:1 entre doctor y paciente sobre cada cita, con link único que se envía por correo al paciente y que solo funciona durante la ventana de tiempo agendada.

Referencia técnica de la API: ver artifact **"Daily.co + .NET"** (endpoints, propiedades de rooms, meeting tokens y ejemplos base). Este documento traduce esa guía a la arquitectura del proyecto.

---

## 1. Contexto

- **Actor con plataforma:** solo el doctor (usa la plataforma de administración).
- **Actor sin plataforma:** el paciente. Se le notifica y accede al link de la videollamada exclusivamente desde el correo que recibe.
- **Motor de videollamada:** [Daily.co](https://docs.daily.co/reference/rest-api) vía REST API (no hay SDK oficial .NET).
- **Estado actual del backend:** al asignar una cita ya se dispara [`AppointmentService.SendConfirmationEmailAsync`](../../Services/Appointments/AppointmentService.cs) — ese es el punto de extensión natural para agregar el link de video.

### Fuera de alcance en esta iteración
- Transcripción (`enable_transcription_storage`, `auto_start_transcription`, endpoints `/transcript/*`, webhooks de transcripción). Se dejan aparcados; en la fase 6 se listan los ganchos que dejaremos preparados para evitar refactors futuros.

---

## 2. Requerimientos funcionales

| # | Requerimiento | Notas técnicas |
|---|--------------|----------------|
| RF-1 | A cada `Appointment` le corresponde un link de videollamada único. | Crear un `Room` en Daily por cita, `name = cita-{appointmentId}`. |
| RF-2 | El link empieza a ser válido cuando llega la hora de la cita. | Room con `nbf = StartHour` (Not Before). |
| RF-3 | El link se inhabilita cuando termina la duración de la cita. | Room con `exp = EndHour + gracia` y `eject_at_room_exp = true`. |
| RF-4 | Las llamadas son siempre entre dos personas (doctor y paciente). | Room con `max_participants = 2`. |
| RF-5 | Duración máxima de 45 minutos por llamada. | Validar `EndHour - StartHour ≤ 45min` al crear. Además `eject_after_elapsed = 45*60` como red de seguridad. |
| RF-6 | Máximo 30 llamadas por mes en todo el sistema. | Contador mensual persistido; verificar antes de crear cada room. |
| RF-7 | El link se envía al correo del paciente. | Extender el template de confirmación con la URL personal del paciente (`room.Url?t={tokenPaciente}`). |

### Requerimientos no funcionales
- La creación de room + tokens no debe bloquear la asignación de la cita si Daily.co falla → manejar con fallback (asignación se guarda; se marca la cita como "sin link" y se puede reintentar).
- Nunca exponer la API Key de Daily en frontend ni en respuestas HTTP.
- Los tokens del doctor no se envían por email; el frontend admin los solicita on-demand cuando el doctor entra a la sala.

---

## 3. Diseño

### 3.1 Modelo de datos

Nueva información asociada a un `Appointment` y una tabla de conteo mensual.

**Migración a `appointments`:**
- `room_name` (`text`, nullable) — nombre lógico de la room en Daily.
- `room_url` (`text`, nullable) — URL base sin token (ej. `https://confirmamed.daily.co/cita-123`).
- `room_created_at` (`timestamp`, nullable) — para auditoría.

**Nueva tabla `video_call_monthly_usage`:**
```sql
CREATE TABLE video_call_monthly_usage (
    year        SMALLINT NOT NULL,
    month       SMALLINT NOT NULL,
    rooms_created INT NOT NULL DEFAULT 0,
    PRIMARY KEY (year, month)
);
```
Se incrementa dentro de una transacción atómica al crear una room; si `rooms_created >= 30` la creación falla antes de llamar a Daily.

> No guardamos los meeting tokens: son de un solo uso lógico y baratos de regenerar. Regenerar el token del doctor cada vez que abre la sala es también más seguro que persistirlo.

### 3.2 Capa de integración con Daily

Nuevo módulo bajo `Services/Daily/` siguiendo el patrón del proyecto (interfaz + implementación + registrar en [`InjectServices`](../../Services/InjectServices.cs)):

```
Services/Daily/
├── IDailyApiService.cs
└── DailyApiService.cs
DTOs/Daily/
├── Requests/
│   ├── CreateRoomRequestDto.cs
│   └── CreateMeetingTokenRequestDto.cs
└── Responses/
    ├── DailyRoomResponseDto.cs
    └── DailyMeetingTokenResponseDto.cs
```

**Contrato mínimo** (sin transcripción):

```csharp
public interface IDailyApiService
{
    Task<DailyRoomResponseDto> CreateRoomAsync(
        string name,
        DateTimeOffset notBefore,
        DateTimeOffset expiration,
        int maxParticipants,
        int maxDurationSeconds,
        CancellationToken ct = default);

    Task<string> CreateMeetingTokenAsync(
        string roomName,
        string userName,
        DateTimeOffset expiration,
        bool isOwner,
        CancellationToken ct = default);

    Task DeleteRoomAsync(string roomName, CancellationToken ct = default);
}
```

Propiedades que fija el servicio al crear la room (según artifact sección 4):
- `privacy = "private"`
- `nbf`, `exp` (unix seconds)
- `eject_at_room_exp = true`
- `eject_after_elapsed = maxDurationSeconds` (RF-5)
- `max_participants = 2`
- `enable_prejoin_ui = true`
- `lang = "es"`

Propiedades del meeting token:
- `room_name`, `user_name`, `exp`
- `eject_at_token_exp = true`
- `is_owner = true` para el doctor, `false` para el paciente

**Registro en `Program.cs`** con `HttpClient` tipado (siguiendo el ejemplo del artifact):
```csharp
builder.Services.Configure<DailySettings>(builder.Configuration.GetSection("Daily"));
builder.Services.AddHttpClient<IDailyApiService, DailyApiService>((sp, client) =>
{
    var s = sp.GetRequiredService<IOptions<DailySettings>>().Value;
    client.BaseAddress = new Uri(s.BaseUrl);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", s.ApiKey);
});
```

### 3.3 Configuración

`appsettings.Development.json` (y homólogos por entorno, más variables de entorno como hace [MailerSend](../../Services/MailerSend/MailerSenderService.cs)):

```json
"Daily": {
  "ApiKey": "",
  "BaseUrl": "https://api.daily.co/v1",
  "SubDomain": "confirmamed",
  "MaxCallDurationMinutes": 45,
  "MonthlyRoomsLimit": 30,
  "ExpirationGraceMinutes": 5
}
```

Fallback de variables de entorno:
- `Daily__ApiKey`
- `Daily__SubDomain`

### 3.4 Orquestación en el flujo de la cita

Nuevo servicio de orquestación `IAppointmentVideoCallService` para no engordar `AppointmentService`:

```
Services/Appointments/VideoCalls/
├── IAppointmentVideoCallService.cs
└── AppointmentVideoCallService.cs
```

Responsabilidades:
1. Validar RF-5 (`EndHour - StartHour ≤ 45min`).
2. Verificar y reservar cupo mensual (RF-6) contra `video_call_monthly_usage`.
3. Llamar a `IDailyApiService.CreateRoomAsync` con `nbf = StartHour` y `exp = EndHour + gracia`.
4. Persistir `room_name`, `room_url`, `room_created_at` en el appointment.
5. Generar `tokenPaciente` y componer el link final `{room_url}?t={tokenPaciente}`.
6. Delegar el envío del correo al helper de emails, pasando ese link.
7. Endpoint separado `GET /appointments/{id}/video/doctor-token` que el frontend admin llama al momento de entrar a la sala — genera un token nuevo con `is_owner = true` y `user_name = "Dr. …"`.

**Integración con [`AppointmentService.AssignAppointmentAsync`](../../Services/Appointments/AppointmentService.cs):** después de `AssignAppointmentAsync` en el repositorio y antes de `SendConfirmationEmailAsync`, invocar `appointmentVideoCallService.ProvisionForAsync(appointmentInfo)`. Si esto falla:
- La asignación no se revierte (la cita sí queda asignada).
- Se marca en logs y se manda el correo de confirmación sin el link (variante del template).
- Endpoint de reintento manual: `POST /appointments/{id}/video/provision`.

### 3.5 Template de correo

Extender [`Templates/confirmation-email.html`](../../Templates/confirmation-email.html) con un bloque condicional para el link:

```html
{{#VideoCallLink}}
<a href="{{VideoCallLink}}">Entrar a la videollamada</a>
<p>Este link solo estará activo entre las {{StartHour}} y las {{EndHour}}.</p>
{{/VideoCallLink}}
```

En [`AppointmentEmailTemplateHelper`](../../Helpers/AppointmentEmailTemplateHelper.cs) añadir el reemplazo `{{VideoCallLink}}` (cadena vacía si no hubo provisión). Al DTO `AppointmentConfirmationEmailDto` agregar `string? VideoCallLink`.

### 3.6 Endpoints propuestos

| Método | Ruta | Descripción | Autorización |
|--------|------|-------------|--------------|
| `POST` | `/appointments/{id}/video/provision` | Crea (o recrea) la room para la cita. Idempotente si ya existe y no ha expirado. | `[Authorize]` |
| `GET` | `/appointments/{id}/video/doctor-token` | Devuelve `{ url, token }` para que el doctor entre. | `[Authorize]` |
| `DELETE` | `/appointments/{id}/video` | Borra la room en Daily (útil al cancelar/reagendar). | `[Authorize]` |

El paciente no consume ningún endpoint; su punto de entrada es el link del correo.

### 3.7 Cancelación / reagendamiento

- **Cancelación:** llamar `DeleteRoomAsync` y liberar el cupo mensual solo si la eliminación ocurre en el mismo mes de creación (evita "regalar" cupos comprando y devolviendo).
- **Reagendamiento (`RescheduleToSlotAsync`):** re-provisionar la room del nuevo slot y borrar la vieja. Reenviar correo con el link nuevo.

---

## 4. Consideraciones de seguridad

- La API Key vive solo en variables de entorno / `appsettings` no versionados.
- Los tokens de Daily viajan por HTTPS únicamente.
- No incluir el token del doctor en el email de nadie.
- No loguear tokens ni URLs completas; loguear solo `room_name`.
- `nbf` + `exp` estrictos garantizan que el link filtrado por email no sirve fuera de la ventana.
- `max_participants = 2` evita que terceros con el link entren si el paciente lo comparte.

---

## 5. Plan por fases

### Fase 1 — Fundaciones (día 1)
- [ ] Crear `DailySettings` y `Services/Daily/{IDailyApiService,DailyApiService}` con `CreateRoomAsync`, `CreateMeetingTokenAsync`, `DeleteRoomAsync`.
- [ ] Registrar `HttpClient` tipado en `Program.cs`.
- [ ] Agregar sección `"Daily"` en `appsettings.Development.json`.
- [ ] DTOs en `DTOs/Daily/`.
- [ ] Prueba manual: crear una room con nbf/exp cortos y verificar en el dashboard de Daily.

### Fase 2 — Persistencia (día 1-2)
- [ ] Migración SQL: columnas `room_name`, `room_url`, `room_created_at` en `appointments`; tabla `video_call_monthly_usage`.
- [ ] Extender `AppointmentFlatDto` / `Appointment` entity con los nuevos campos.
- [ ] Repositorio `IVideoCallUsageRepository` con `TryReserveMonthlySlotAsync(year, month)` atómico (UPDATE … RETURNING con `WHERE rooms_created < :limit`).
- [ ] Método `UpdateVideoRoomAsync(appointmentId, roomName, roomUrl)` en `IAppointmentRepository`.

### Fase 3 — Orquestación (día 2)
- [ ] `IAppointmentVideoCallService` con `ProvisionForAsync`, `IssueDoctorTokenAsync`, `DeprovisionAsync`.
- [ ] Validación de duración ≤ 45 min.
- [ ] Registrar en `InjectServices.cs`.
- [ ] Integrar en `AppointmentService.AssignAppointmentAsync` (llamada tras la asignación, antes del correo).

### Fase 4 — Correo (día 2)
- [ ] Añadir `VideoCallLink` al `AppointmentConfirmationEmailDto`.
- [ ] Bloque condicional en `Templates/confirmation-email.html`.
- [ ] Ajustar `AppointmentEmailTemplateHelper.BuildConfirmationEmail`.
- [ ] Verificar el template con y sin link.

### Fase 5 — Endpoints y ciclo de vida (día 3)
- [ ] `AppointmentsController`: `POST /appointments/{id}/video/provision`, `GET /appointments/{id}/video/doctor-token`, `DELETE /appointments/{id}/video`.
- [ ] Integrar `DeprovisionAsync` en el flujo de reagendamiento (`RescheduleToSlotAsync`).
- [ ] Rate limit con `[EnableRateLimiting("UserPolicy")]`.

### Fase 6 — Ganchos para transcripción (futuro)
Dejar preparado (sin implementar) para no rehacer contratos después:
- Reservar el flag `EnableTranscription` en `CreateRoomRequestDto` (default `false`).
- Nombre y estructura del webhook `POST /webhook/daily` acordados aunque sin lógica interna.

---

## 6. Riesgos y decisiones abiertas

| Riesgo / duda | Decisión acordada |
|---------------|-------------------|
| Daily.co responde 5xx durante la asignación. | **Acordado:** guardar la cita igual, marcar sin link y ofrecer reintento manual vía `POST /appointments/{id}/video/provision`. |
| Reloj del server vs `exp` en unix. | **Acordado:** usar `DateTimeOffset.UtcNow` siempre; sumar la gracia en UTC. |
| Zona horaria de `StartHour`/`EndHour` en Colombia. | **Acordado:** la validez arranca en `StartHour` hora Colombia (UTC-5). Se convierte a `DateTimeOffset` combinando `DateAppointment` + `TimeSpan` con offset `-05:00` para `nbf`. La duración ya persiste en la cita, así que `exp = nbf + duración + gracia`. |
| Un paciente reenvía el link a un tercero. | **Acordado:** `max_participants = 2` es la única defensa por ahora. Se acepta como límite del modelo. |
| Cupo mensual llega a 30 a mitad de mes. | **Acordado por ahora:** fail fast con `BadRequestException("Se alcanzó el límite mensual de videollamadas")`. Cuando toque escalar se conversa con el cliente cómo comunicarlo mejor. |
| ¿Los reagendamientos consumen cupo mensual otra vez? | **Pendiente:** se posterga la decisión y se revisa en detalle antes de la fase 5. Mientras tanto, no se implementa consumo adicional en reagendamiento. |

---

## 7. Checklist de aceptación

- [ ] Al asignar una cita con duración válida, el paciente recibe un correo con un link que abre la sala.
- [ ] Intentar entrar antes de `StartHour` muestra el mensaje "not before" de Daily.
- [ ] Intentar entrar después de `EndHour + gracia` bloquea el ingreso y expulsa al que esté dentro.
- [ ] Con 30 rooms creadas en el mes, la próxima asignación con provisión responde 400 con mensaje claro.
- [ ] Solo dos participantes máximo pueden estar en la sala simultáneamente.
- [ ] El doctor recibe un token de owner cada vez que llama al endpoint `doctor-token`.
- [ ] Cancelar/reagendar borra la room vieja en Daily.
