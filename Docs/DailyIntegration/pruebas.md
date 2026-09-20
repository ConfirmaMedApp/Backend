# Hoja de pruebas — Videollamadas Daily.co

Casos manuales para verificar la integración end-to-end. Todos los tiempos son **hora Colombia (UTC-5)**.

---

## 0. Prueba rápida (sin JWT, para unirte a una llamada)

Este flujo cubre el 90% de lo que quieres probar: crear/tomar una cita, asignarla, y abrir el link en el navegador. **No necesitas generar JWT** — el endpoint de asignación es público (`[AllowAnonymous]`) y desde el smoke test devuelve el `videoCallLink` listo para abrir.

### Paso 1 — Levantar el API

```bash
cd Backend && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5099
```

### Paso 2 — Crear una cita corta para probar (start dentro de 3 min, dura 5 min)

```bash
export PGCONN="postgresql://neondb_owner:npg_AQa83TzrcuWm@ep-dawn-bonus-ax562ruc-pooler.c-4.us-east-2.aws.neon.tech:5432/confirmamed_db_dev?sslmode=require"

CITA_ID=$(psql "$PGCONN" -tA -c "
INSERT INTO appointments
    (date_appointment, start_hour, end_hour, duration_id, doctor_id, speciality_id, user_id, status, is_occuped, is_approved, created_at)
SELECT
    CURRENT_DATE,
    (now() AT TIME ZONE 'America/Bogota' + interval '3 min')::time,
    (now() AT TIME ZONE 'America/Bogota' + interval '8 min')::time,
    2, 3, 6, 2, true, false, false, now()
RETURNING id;
")
echo "Cita creada: $CITA_ID"
```

> Si prefieres usar una cita existente en vez de crear una, corre:
> `psql "$PGCONN" -c "SELECT id, date_appointment, start_hour, end_hour FROM appointments WHERE is_occuped=false AND date_appointment>=CURRENT_DATE ORDER BY date_appointment, start_hour LIMIT 5;"`
> Y sustituye `CITA_ID` por el id que elijas.

### Paso 3 — Asignar la cita (paciente 2 = leonardo)

```bash
curl -s -X POST http://127.0.0.1:5099/api/appointments/assign \
  -H "Content-Type: application/json" \
  -d "{\"appointmentId\": $CITA_ID, \"patientId\": 2}" | python3 -m json.tool
```

En `items.videoCallLink` viene la URL completa con el token del paciente. Cópiala.

### Paso 4 — Abrir el link

Pegar la URL en el navegador. Dependiendo de en qué momento la abras:
- Antes del `start_hour + 3 min` → Daily muestra "Meeting hasn't started yet".
- Dentro de la ventana → entra a la sala (pide cámara/micro).
- Después de `end_hour + 5 min` → Daily bloquea el ingreso.

Para probar como si fueras el **doctor**, abre el link del paciente en otra pestaña / incógnito — la sala tiene `max_participants = 2` así que ambos entran.

### Paso 5 — Cleanup

```bash
DAILY_KEY="b163eef60bdbd34f7b3800a7711fc3d984e8d95d20b56e0bc665d29ec4a980cf"
curl -s -X DELETE -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$CITA_ID >/dev/null
psql "$PGCONN" -c "DELETE FROM appointments WHERE id = $CITA_ID;"
psql "$PGCONN" -c "DELETE FROM video_call_monthly_usage WHERE year = EXTRACT(year FROM CURRENT_DATE) AND month = EXTRACT(month FROM CURRENT_DATE);"
```

> **Cuándo sí necesitas JWT:** solo para los endpoints admin (`POST /video/provision` manual, `GET /video/doctor-token`, `DELETE /video`). Esos usan `[Authorize]` porque en producción los llama el frontend admin autenticado. La sección 1.3 explica cómo generar uno de prueba para esos casos.

---

## 1. Preparación

### 1.1 Variables

- **API local:** `http://127.0.0.1:5099` (ajusta si usas otro puerto).
- **Base de datos:** conexión ya definida en [appsettings.Development.json](../../appsettings.Development.json) → `ConnectionStrings:DefaultDevConnection`.
- **Daily API Key + subdominio:** ya definidos en `appsettings.Development.json` → sección `Daily`.
- **JWT Secret:** `bdc8d5e1-9961-4077-99b1-b4c3c69e0654` (mismo de `Jwt:Key`).

Exporta para reutilizar:

```bash
export API="http://127.0.0.1:5099"
export PGCONN="postgresql://neondb_owner:npg_AQa83TzrcuWm@ep-dawn-bonus-ax562ruc-pooler.c-4.us-east-2.aws.neon.tech:5432/confirmamed_db_dev?sslmode=require"
export DAILY_KEY="b163eef60bdbd34f7b3800a7711fc3d984e8d95d20b56e0bc665d29ec4a980cf"
export DAILY_SUB="leotrabajostest.daily.co"
```

### 1.2 Levantar el API

```bash
cd Backend && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5099
```

### 1.3 Generar un JWT válido (10 min de vida)

```bash
export JWT=$(python3 -c "
import hmac, hashlib, base64, json, time
key = 'bdc8d5e1-9961-4077-99b1-b4c3c69e0654'
header = {'alg':'HS256','typ':'JWT'}
now = int(time.time())
payload = {
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '2',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'QA Tester',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'admin',
    'iss': 'ConfirmMedApi', 'aud': 'ConfirmMedClient',
    'iat': now, 'exp': now + 600
}
def b64(d): return base64.urlsafe_b64encode(json.dumps(d,separators=(',',':')).encode()).rstrip(b'=').decode()
si = f'{b64(header)}.{b64(payload)}'
sig = base64.urlsafe_b64encode(hmac.new(key.encode(), si.encode(), hashlib.sha256).digest()).rstrip(b'=').decode()
print(f'{si}.{sig}')
")
```

Si en 10 min se vence, corre de nuevo el mismo bloque.

### 1.4 Utilidades rápidas

```bash
# Estado de una cita
psql "$PGCONN" -c "SELECT id, patient_id, is_occuped, room_name, room_url, room_created_at FROM appointments WHERE id = $ID;"

# Contador mensual
psql "$PGCONN" -c "SELECT * FROM video_call_monthly_usage;"

# Room en Daily (200 = existe, 404 = no)
curl -s -o /dev/null -w "HTTP %{http_code}\n" -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$ID

# Room completa en Daily (nbf/exp/max_participants…)
curl -s -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$ID | python3 -m json.tool
```

### 1.5 Reset entre pruebas

```bash
# Resetear cita a "libre sin room"
psql "$PGCONN" -c "UPDATE appointments SET patient_id = NULL, is_occuped = false, is_approved = false, room_name = NULL, room_url = NULL, room_created_at = NULL WHERE id = $ID;"

# Borrar room en Daily manualmente si quedó colgada
curl -X DELETE -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$ID

# Resetear el contador mensual del mes en curso
psql "$PGCONN" -c "DELETE FROM video_call_monthly_usage WHERE year = 2026 AND month = 9;"
```

### 1.6 Elegir una cita de prueba

Cualquier cita futura, libre, y con duración ≤ 45 min sirve:

```bash
psql "$PGCONN" -c "SELECT id, date_appointment, start_hour, end_hour, EXTRACT(EPOCH FROM (end_hour - start_hour))/60 AS min FROM appointments WHERE is_occuped = false AND date_appointment >= CURRENT_DATE ORDER BY date_appointment, start_hour LIMIT 5;"
```

Pacientes de ejemplo: `2` (leonardo), `3` (jhon), `4` (juan camilo).

> **Nota:** en dev, la API key de MailerSend está vacía → los correos no se envían realmente. La respuesta del endpoint sí trae el link; para validar el HTML, mira el render en [Docs/DailyIntegration/plan.md](plan.md) o simula el helper.

---

## 2. Casos de prueba

Formato: **Precondición · Acción · Esperado · Verificación**. Sustituye `$ID` por el id de cita y `$PID` por el id de paciente.

---

### Caso 1 — Provisión automática al asignar (happy path)

**Precondición**
- Cita `$ID` libre, futura, duración ≤ 45 min, sin `room_name`.
- Cupo mensual con al menos 1 lugar libre.

**Acción**

```bash
curl -s -X POST "$API/api/appointments/assign" \
  -H "Content-Type: application/json" \
  -d "{\"appointmentId\": $ID, \"patientId\": $PID}" | python3 -m json.tool
```

**Esperado**
- HTTP 200, `success: true`.
- La respuesta trae la cita asignada.

**Verificación**

```bash
psql "$PGCONN" -c "SELECT id, patient_id, is_occuped, room_name, room_url, room_created_at FROM appointments WHERE id = $ID;"
psql "$PGCONN" -c "SELECT * FROM video_call_monthly_usage;"
curl -s -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$ID | python3 -m json.tool
```

Debe cumplirse:
- `room_name = 'cita-$ID'`, `room_url` con formato `https://$DAILY_SUB/cita-$ID`.
- `rooms_created` incrementó en 1.
- Room en Daily con `nbf` = StartHour Colombia, `exp` = EndHour + 5 min, `max_participants = 2`, `eject_at_room_exp = true`, `eject_after_elapsed` = duración en segundos, `lang = "es"`, `privacy = "private"`.

---

### Caso 2 — Idempotencia de re-provisión

**Precondición**
- Cita `$ID` **ya provisionada** (viene del Caso 1).

**Acción**

```bash
curl -s -X POST "$API/api/appointments/$ID/video/provision" \
  -H "Authorization: Bearer $JWT" | python3 -m json.tool
```

**Esperado**
- HTTP 200, `success: true`.
- `items.roomUrl` es la misma que ya estaba.
- `items.patientLink` trae un token **nuevo** (comparar con el `patientLink` anterior si lo guardaste; el segmento después de `?t=` debe cambiar).

**Verificación**

```bash
# rooms_created NO debe haber subido de 1
psql "$PGCONN" -c "SELECT * FROM video_call_monthly_usage;"
```

---

### Caso 3 — Token del doctor

**Precondición**
- Cita `$ID` provisionada.

**Acción**

```bash
curl -s -H "Authorization: Bearer $JWT" \
  "$API/api/appointments/$ID/video/doctor-token" | python3 -m json.tool
```

**Esperado**
- HTTP 200.
- `items.roomUrl` igual a la de la cita.
- `items.token` no vacío.
- `items.joinUrl = "{roomUrl}?t={token}"`.
- `items.expiresAt` = EndHour Colombia + 5 min de gracia (formato `2026-MM-DDTHH:mm:ss-05:00`).

**Verificación adicional (opcional)**

Decodifica el token para confirmar que trae `o: true` (owner) y `u: "Dr. {Nombre} {Apellido}"`:

```bash
echo "$TOKEN" | cut -d. -f2 | tr '_-' '/+' | base64 -d 2>/dev/null
```

---

### Caso 4 — Deprovisión (DELETE)

**Precondición**
- Cita `$ID` provisionada.

**Acción**

```bash
curl -s -X DELETE -H "Authorization: Bearer $JWT" "$API/api/appointments/$ID/video" | python3 -m json.tool
```

**Esperado**
- HTTP 200, mensaje `"Videollamada eliminada satisfactoriamente"`.

**Verificación**

```bash
# Room ya no existe en Daily (404)
curl -s -o /dev/null -w "HTTP %{http_code}\n" -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$ID

# Columnas en NULL
psql "$PGCONN" -c "SELECT id, room_name, room_url, room_created_at FROM appointments WHERE id = $ID;"

# rooms_created NO baja (la liberación solo aplica si Daily falla en la creación)
psql "$PGCONN" -c "SELECT * FROM video_call_monthly_usage;"
```

---

### Caso 5 — DELETE idempotente (room ya no existe)

**Precondición**
- Cita `$ID` sin room (después del Caso 4).

**Acción** — mismo DELETE que en Caso 4.

**Esperado** — HTTP 200 igualmente (el orquestador detecta `room_name = NULL` y hace no-op).

---

### Caso 6 — Reagendamiento borra la room vieja

**Precondición**
- Cita `$OLD` provisionada (con paciente).
- Cita `$NEW` libre.

**Acción**

```bash
curl -s -X PUT "$API/api/appointments/reschedule" \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d "{\"oldAppointmentId\": $OLD, \"newAppointmentId\": $NEW}" | python3 -m json.tool
```

**Esperado** — HTTP 200.

**Verificación**

```bash
# La room vieja YA NO existe en Daily
curl -s -o /dev/null -w "HTTP %{http_code}\n" -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$OLD

# Cita vieja tiene columnas room_* en NULL
psql "$PGCONN" -c "SELECT id, patient_id, room_name FROM appointments WHERE id = $OLD;"

# Cita nueva TAMPOCO tiene room (no se auto-provisiona)
psql "$PGCONN" -c "SELECT id, patient_id, room_name FROM appointments WHERE id = $NEW;"
```

Para poner video en el nuevo slot, ejecuta el Caso 7 sobre `$NEW`.

---

### Caso 7 — Re-provisión manual tras reagendamiento

**Precondición**
- Cita `$NEW` con paciente asignado (del Caso 6) pero sin room.

**Acción**

```bash
curl -s -X POST -H "Authorization: Bearer $JWT" "$API/api/appointments/$NEW/video/provision" | python3 -m json.tool
```

**Esperado**
- HTTP 200 con `roomUrl` y `patientLink`.
- Se **consume otro cupo** mensual (`rooms_created` sube +1 respecto al Caso 1).

---

### Caso 8 — Validación de duración (> 45 min → 400)

**Precondición**
- Crear una cita temporal de 60 min:

```bash
NEW_ID=$(psql "$PGCONN" -tA -c "INSERT INTO appointments (date_appointment, start_hour, end_hour, duration_id, doctor_id, speciality_id, user_id, status, is_occuped, is_approved, patient_id, created_at) VALUES ('2026-12-01', '10:00', '11:00', 2, 3, 6, 2, true, true, true, 2, now()) RETURNING id;")
echo "temp id: $NEW_ID"
```

**Acción**

```bash
curl -s -X POST -H "Authorization: Bearer $JWT" "$API/api/appointments/$NEW_ID/video/provision" | python3 -m json.tool
```

**Esperado**
- HTTP 400, mensaje `"La duración de la cita (60 min) supera el máximo permitido (45 min) para videollamadas"`.
- **No** hay room en Daily (`GET /rooms/cita-$NEW_ID` → 404).
- **No** se consumió cupo mensual.

**Cleanup**

```bash
psql "$PGCONN" -c "DELETE FROM appointments WHERE id = $NEW_ID;"
```

---

### Caso 9 — Límite mensual agotado (30 → 400)

**Precondición**
- Setear el contador al límite:

```bash
psql "$PGCONN" -c "INSERT INTO video_call_monthly_usage (year, month, rooms_created) VALUES (2026, 9, 30) ON CONFLICT (year, month) DO UPDATE SET rooms_created = 30;"
```
- Cita `$ID` libre y sin room.

**Acción** — mismo POST de `/video/provision`.

**Esperado**
- HTTP 400, mensaje `"Se alcanzó el límite mensual de videollamadas"`.
- No se creó room en Daily.
- Contador **no** cambia (queda en 30, la reserva atómica falló antes).

**Cleanup**

```bash
psql "$PGCONN" -c "DELETE FROM video_call_monthly_usage WHERE year = 2026 AND month = 9;"
```

---

### Caso 10 — Provisión sin paciente asignado (400)

**Precondición**
- Cita `$ID` libre (`patient_id IS NULL`).

**Acción** — mismo POST de `/video/provision`.

**Esperado**
- HTTP 400, mensaje `"La cita no tiene paciente asignado"`.

---

### Caso 11 — Doctor token cuando no hay room (400)

**Precondición**
- Cita `$ID` con `room_name IS NULL`.

**Acción**

```bash
curl -s -H "Authorization: Bearer $JWT" "$API/api/appointments/$ID/video/doctor-token"
```

**Esperado** — HTTP 400 con `"La sala de videollamada aún no ha sido provisionada"`.

---

### Caso 12 — Cita inexistente

**Acción**

```bash
curl -s -X POST -H "Authorization: Bearer $JWT" "$API/api/appointments/999999/video/provision"
```

**Esperado** — HTTP 404 con `"Cita no encontrada"`.

---

### Caso 13 — Ventana de tiempo del link (funcional, en el navegador)

**Precondición**
- Cita `$ID` provisionada.
- Copia `patientLink` del Caso 1 o Caso 2.

**Acciones**

1. Abrir el link **antes** de la hora agendada.
2. Abrir el link **dentro** de la ventana (StartHour a EndHour+5 min).
3. Abrir el link **después** de la ventana.

**Esperado**
- Antes: Daily muestra pantalla "Meeting hasn't started yet" (o similar).
- Dentro: entra a la sala y pide cámara/micro.
- Después: Daily muestra "This meeting has ended" y bloquea el ingreso.

Para agilizar esta prueba, crea una cita con hora dentro de los próximos 5 min y duración de 2 min:

```bash
# start = ahora + 3 min Colombia, end = start + 2 min
psql "$PGCONN" -c "INSERT INTO appointments (date_appointment, start_hour, end_hour, duration_id, doctor_id, speciality_id, user_id, status, is_occuped, is_approved, created_at) SELECT CURRENT_DATE, (now() AT TIME ZONE 'America/Bogota' + interval '3 min')::time, (now() AT TIME ZONE 'America/Bogota' + interval '5 min')::time, 2, 3, 6, 2, true, false, false, now() RETURNING id;"
```

Luego asigna paciente y sigue el link.

---

### Caso 14 — Máximo 2 participantes (funcional)

**Precondición** — cita `$ID` provisionada, ventana horaria activa.

**Acciones**
1. Abrir el link del paciente en dos pestañas / dispositivos distintos.
2. Con el token del doctor (Caso 3), abrir la sala en un tercer navegador.

**Esperado**
- Los primeros 2 entran.
- El tercero recibe error de Daily "This room is full" o similar.

---

### Caso 15 — Expulsión al vencerse (`eject_at_room_exp`)

**Precondición** — usar la cita corta creada en Caso 13; entrar a la sala **antes** del `exp`.

**Esperado** — cuando llega `EndHour + 5 min`, Daily expulsa a todos los participantes automáticamente.

---

## 3. Cleanup general

Después de las pruebas:

```bash
# Resetear todas las citas de prueba (ajusta los IDs)
psql "$PGCONN" -c "UPDATE appointments SET patient_id = NULL, is_occuped = false, is_approved = false, room_name = NULL, room_url = NULL, room_created_at = NULL WHERE id IN ($ID1, $ID2, ...);"

# Borrar rooms colgadas en Daily
for id in $ID1 $ID2; do
    curl -s -X DELETE -H "Authorization: Bearer $DAILY_KEY" https://api.daily.co/v1/rooms/cita-$id
done

# Resetear contador mensual del mes en curso
psql "$PGCONN" -c "DELETE FROM video_call_monthly_usage WHERE year = 2026 AND month = 9;"

# Borrar citas temporales creadas ad-hoc
psql "$PGCONN" -c "DELETE FROM appointments WHERE id IN ($TEMP_ID1, $TEMP_ID2);"
```

---

## 4. Resumen de resultados esperados

| # | Caso | Endpoint | Esperado |
|---|------|----------|----------|
| 1  | Asignación crea room | `POST /appointments/assign` | 200 + room en Daily + cupo +1 |
| 2  | Re-provisión idempotente | `POST /video/provision` | 200 + misma room + cupo igual |
| 3  | Token del doctor | `GET /video/doctor-token` | 200 + token owner |
| 4  | DELETE room | `DELETE /video` | 200 + Daily 404 + columnas NULL |
| 5  | DELETE idempotente | `DELETE /video` | 200 no-op |
| 6  | Reagendar borra vieja | `PUT /appointments/reschedule` | 200 + vieja borrada, nueva sin room |
| 7  | Re-provisión post reschedule | `POST /video/provision` | 200 + cupo +1 |
| 8  | Duración > 45 min | `POST /video/provision` | 400 |
| 9  | Cupo mensual lleno | `POST /video/provision` | 400 |
| 10 | Sin paciente | `POST /video/provision` | 400 |
| 11 | Token doctor sin room | `GET /video/doctor-token` | 400 |
| 12 | Cita inexistente | cualquiera | 404 |
| 13 | Ventana del link | navegador | comportamiento por franja |
| 14 | Max 2 participantes | navegador | tercero rechazado |
| 15 | Expulsión al `exp` | navegador | expulsión automática |
