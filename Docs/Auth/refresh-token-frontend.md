# Autenticación con Refresh Token — Guía de integración Frontend

Guía completa para consumir el flujo de autenticación del backend **ConfirmaMed**
(JWT de acceso + refresh token rotativo en cookie `HttpOnly`).

---

## 1. Modelo de dos tokens

| Token | Dónde vive | Quién lo maneja | Duración |
|-------|-----------|-----------------|----------|
| **Access token (JWT)** | En el **cuerpo** de la respuesta (`items.token`) | El frontend lo guarda **en memoria** y lo envía en `Authorization: Bearer <token>` | **60 min** |
| **Refresh token** | Cookie `cm_rt` **`HttpOnly`** (el navegador la envía solo) | **El navegador**, de forma automática. El JS **no** puede leerla | **Deslizante 7 días** / **tope absoluto 30 días** |

> **Clave:** el refresh token es `HttpOnly` a propósito. El frontend **nunca** lo
> lee ni lo guarda; solo debe asegurarse de que el navegador lo mande con
> `credentials: 'include'` / `withCredentials: true`.

### Rotación y detección de reúso
- Cada `POST /auth/refresh` **revoca** el refresh anterior y emite uno nuevo (rotación).
- Si un refresh **ya revocado** se vuelve a usar (señal de robo), el backend
  **revoca toda la familia de la sesión** → el usuario queda deslogueado y debe
  volver a iniciar sesión.
- **Consecuencia práctica para el frontend:** nunca dispares dos `/auth/refresh`
  en paralelo con la misma cookie. Usa un patrón **single-flight** (ver §6).

---

## 2. Requisito imprescindible: enviar credenciales

Para que el navegador mande/reciba la cookie `cm_rt` (cross-site en producción),
**todas** las llamadas al backend deben incluir credenciales:

- `fetch`: `credentials: 'include'`
- `axios`: `withCredentials: true`

Sin esto, el login parecerá funcionar (devuelve el JWT) pero **el refresh nunca
llegará** porque la cookie no se guarda ni se reenvía.

El front debe servirse desde uno de los **orígenes permitidos** por CORS
(en dev se acepta cualquier puerto de `localhost`/`127.0.0.1`).

---

## 3. Endpoints

Base URL de autenticación: `/api/auth`

### `POST /api/auth/login`
Inicia sesión y abre una nueva sesión (familia de refresh tokens).

**Request**
```json
{ "userName": "delcruz_", "password": "••••••" }
```

**Response `200`** — además setea la cookie `cm_rt`:
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Sesión iniciada",
  "path": "/api/auth/login",
  "responseTime": "2026-10-04T05:16:22.4Z",
  "items": {
    "id": 2,
    "fullName": "leonardo de la cruz ardila",
    "userName": "delcruz_",
    "role": "admin",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```
**Errores:** `401` `Credenciales invalidas`.

---

### `POST /api/auth/refresh`
Renueva el access token usando la cookie `cm_rt`. **No lleva body.**
Rota la cookie (emite una nueva) y devuelve un nuevo JWT.

**Response `200`** — misma forma que login (`items.token` nuevo + cookie nueva).

**Errores:**
- `401 Sesión no válida` — no hay cookie, el token no existe, fue reusado
  (familia revocada) o la fila desapareció. **Borra la cookie.**
- `401 Sesión expirada` — venció la ventana deslizante o el tope absoluto.

> Ante un `401` de `/auth/refresh`, la sesión terminó: limpia el estado y
> redirige a login.

---

### `POST /api/auth/logout`
Cierra la sesión: revoca **toda la familia** y borra la cookie. **No lleva body.**

**Response `204 No Content`.**
Es `AllowAnonymous`: funciona aunque el access token ya haya expirado.

---

### `GET /api/auth/verify`
Valida un JWT pasado por header. Útil para comprobar un token puntual.

**Request:** header `Authorization: Bearer <token>`
**Response `200`:** `items` = `{ id, fullName, role }`.
**Errores:** `401 Token no encontrado` / `Token inválido o expirado`.

---

### `GET /api/auth/me`
Datos del usuario autenticado (requiere Bearer válido).

**Response `200`:** `items` = `{ id, name }`.

---

## 4. Formato de respuestas (⚠️ inconsistencia a manejar)

- **Respuestas de éxito** (controladores) → **camelCase**:
  `{ success, statusCode, message, path, responseTime, items }`
- **Respuestas de error** (middleware global) → **PascalCase**:
  `{ Success, StatusCode, Message, Path, ResponseTime, Items }`

Lee el mensaje de forma tolerante, p. ej.:
```js
const msg = data.message ?? data.Message;
```

---

## 5. Ciclo de vida recomendado en el frontend

1. **Login** → guarda `items.token` en **memoria** (no en `localStorage`; la cookie
   `HttpOnly` ya protege la sesión persistente).
2. **Requests protegidas** → `Authorization: Bearer <accessToken>` + `withCredentials`.
3. **Al recibir `401`** en una request protegida (JWT expirado a los 60 min):
   llama `POST /auth/refresh`, actualiza el access token y **reintenta** la request original.
4. **Al recargar la página** el access token en memoria se pierde, pero la cookie
   `cm_rt` sigue viva → al arrancar la app llama `POST /auth/refresh` para
   **restaurar la sesión** silenciosamente. Si da `401`, no hay sesión → login.
5. **Logout** → `POST /auth/logout` y limpia el access token en memoria.

---

## 6. Ejemplo con Axios (interceptor + single-flight)

```js
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL, // p.ej. https://tu-backend.up.railway.app
  withCredentials: true,                 // ← imprescindible (cookie cm_rt)
});

// --- Access token en memoria ---
let accessToken = null;
export const setAccessToken = (t) => { accessToken = t; };

// Adjunta el Bearer a cada request
api.interceptors.request.use((config) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

// --- Refresh con single-flight (evita refresh en paralelo) ---
let refreshing = null;

async function refreshAccessToken() {
  // Instancia "cruda" para no pasar por el interceptor de respuesta
  const { data } = await axios.post(
    `${api.defaults.baseURL}/api/auth/refresh`,
    null,
    { withCredentials: true },
  );
  const newToken = data.items.token;
  setAccessToken(newToken);
  return newToken;
}

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    const status = error.response?.status;
    const isAuthCall = original?.url?.includes('/api/auth/');

    if (status === 401 && !original._retry && !isAuthCall) {
      original._retry = true;
      try {
        // Todas las requests que fallen con 401 comparten la MISMA promesa de refresh
        refreshing = refreshing ?? refreshAccessToken().finally(() => { refreshing = null; });
        const newToken = await refreshing;
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original); // reintenta la original
      } catch (e) {
        setAccessToken(null);
        // Sesión terminada → redirige a login
        // window.location.assign('/login');
        return Promise.reject(e);
      }
    }
    return Promise.reject(error);
  },
);

export default api;
```

### Login / logout / bootstrap

```js
import api, { setAccessToken } from './api';

export async function login(userName, password) {
  const { data } = await api.post('/api/auth/login', { userName, password });
  setAccessToken(data.items.token);
  return data.items; // { id, fullName, userName, role, token }
}

export async function logout() {
  try { await api.post('/api/auth/logout'); }
  finally { setAccessToken(null); }
}

// Llamar una vez al arrancar la app (restaura sesión tras recargar)
export async function bootstrapSession() {
  try {
    await api.post('/api/auth/refresh'); // el interceptor NO aplica aquí (es /auth/)
    // ...pero sí actualizamos el token manualmente:
  } catch {
    return null;
  }
}
```

> Nota: para `bootstrapSession`, llama directamente `refreshAccessToken()` del
> módulo si lo exportas, para capturar el token y setearlo. El punto importante
> es: **al iniciar la app, intenta un refresh**; si falla, no hay sesión.

---

## 7. Ejemplo con fetch

```js
const BASE = import.meta.env.VITE_API_URL;
let accessToken = null;

async function apiFetch(path, options = {}) {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    credentials: 'include', // ← imprescindible
    headers: {
      'Content-Type': 'application/json',
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...options.headers,
    },
  });

  if (res.status === 401 && !path.includes('/api/auth/')) {
    const r = await fetch(`${BASE}/api/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!r.ok) { accessToken = null; throw new Error('session-ended'); }
    accessToken = (await r.json()).items.token;
    return apiFetch(path, options); // reintenta
  }
  return res;
}
```

> Con `fetch` también debes serializar los refresh concurrentes (single-flight)
> si disparas varias llamadas a la vez. El ejemplo de Axios es el patrón de referencia.

---

## 8. Tiempos de vida (configurables)

| Concepto | Valor por defecto | Variable |
|----------|-------------------|----------|
| Access token (JWT) | 60 min | `Jwt__ExpireMinutes` |
| Refresh — ventana deslizante | 7 días | `Jwt__RefreshTokenExpireDays` |
| Refresh — tope absoluto de sesión | 30 días | `Jwt__RefreshTokenAbsoluteExpireDays` |
| Nombre de la cookie | `cm_rt` | `Jwt__RefreshCookieName` |

- **Deslizante:** cada refresh reinicia los 7 días de inactividad permitida.
- **Absoluto:** la sesión muere a los 30 días desde el login, aunque haya actividad.

---

## 9. Comportamiento de la cookie `cm_rt`

| | Desarrollo | Producción |
|---|-----------|-----------|
| `HttpOnly` | sí | sí |
| `Secure` | no (permite `http://localhost`) | **sí** (requiere HTTPS) |
| `SameSite` | `Lax` | **`None`** (cross-site front/back) |
| `Path` | `/` | `/` |

En producción, front y back están en dominios distintos, por eso
`SameSite=None; Secure`. Esto **obliga** a HTTPS en ambos y a `withCredentials`
en el front.
