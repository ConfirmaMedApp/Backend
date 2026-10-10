-- =============================================================================
-- Paso 1: Estados de cita + historial de estados
-- Branch de base de datos: develop (confirmamed_db_dev)
-- Aditivo y no destructivo: NO elimina is_occuped / is_approved todavía.
-- =============================================================================

BEGIN;

-- 1. Catálogo de estados ------------------------------------------------------
CREATE TABLE IF NOT EXISTS appointment_states (
    id         serial PRIMARY KEY,
    code       varchar(20) NOT NULL UNIQUE,
    name       varchar(50) NOT NULL,
    color      varchar(9)  NOT NULL DEFAULT '#9ca3af',
    created_at timestamp   NOT NULL DEFAULT now()
);

INSERT INTO appointment_states (code, name, color) VALUES
    ('libre',       'Libre',       '#22c55e'),
    ('asignada',    'Asignada',    '#3b82f6'),
    ('en_atencion', 'En atención', '#f59e0b'),
    ('finalizada',  'Finalizada',  '#6b7280'),
    ('cancelada',   'Cancelada',   '#ef4444'),
    ('no_asistio',  'No asistió',  '#a855f7')
ON CONFLICT (code) DO NOTHING;

-- 2. Columna state_id en appointments (nullable por ahora) --------------------
ALTER TABLE appointments
    ADD COLUMN IF NOT EXISTS state_id integer REFERENCES appointment_states(id);

-- 3. Tabla de historial -------------------------------------------------------
CREATE TABLE IF NOT EXISTS appointment_state_history (
    id             serial PRIMARY KEY,
    appointment_id integer     NOT NULL REFERENCES appointments(id),
    from_state_id  integer     REFERENCES appointment_states(id),
    to_state_id    integer     NOT NULL REFERENCES appointment_states(id),
    changed_by     integer     REFERENCES users(id),   -- NULL = cambio automático / sistema
    reason         varchar(50),                          -- p.ej. 'asignacion', 'no_pago', 'manual', 'migracion'
    note           text,
    created_at     timestamp   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_appt_state_hist_appt
    ON appointment_state_history (appointment_id, created_at);

-- 4. Trigger que registra cada cambio de estado ------------------------------
-- changed_by / reason se leen de settings de sesión que la app puede fijar:
--   SET LOCAL app.current_user_id = '<userId>';
--   SET LOCAL app.state_change_reason = '<reason>';
-- Si no están fijados, quedan NULL (cambio de sistema).
CREATE OR REPLACE FUNCTION log_appointment_state_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_user   int;
    v_reason varchar(50);
BEGIN
    -- En UPDATE, solo registrar si el estado realmente cambió
    IF TG_OP = 'UPDATE' AND NEW.state_id IS NOT DISTINCT FROM OLD.state_id THEN
        RETURN NEW;
    END IF;

    v_user   := nullif(current_setting('app.current_user_id', true), '')::int;
    v_reason := nullif(current_setting('app.state_change_reason', true), '');

    INSERT INTO appointment_state_history
        (appointment_id, from_state_id, to_state_id, changed_by, reason)
    VALUES (
        NEW.id,
        CASE WHEN TG_OP = 'UPDATE' THEN OLD.state_id ELSE NULL END,
        NEW.state_id,
        v_user,
        v_reason
    );

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_appointment_state_history ON appointments;
CREATE TRIGGER trg_appointment_state_history
    AFTER INSERT OR UPDATE OF state_id ON appointments
    FOR EACH ROW
    EXECUTE FUNCTION log_appointment_state_change();

-- 5. Backfill de los datos actuales ------------------------------------------
-- El trigger ya existe, así que este UPDATE genera una fila de historial
-- "baseline" (from_state=NULL -> estado actual) con reason='migracion'.
SET LOCAL app.state_change_reason = 'migracion';

UPDATE appointments a
SET state_id = s.id
FROM appointment_states s
WHERE a.state_id IS NULL
  AND s.code = CASE
        WHEN a.is_occuped = false THEN 'libre'
        WHEN (a.date_appointment + a.end_hour) < (now() AT TIME ZONE 'America/Bogota') THEN 'finalizada'
        ELSE 'asignada'
      END;

-- 6. Default + NOT NULL -------------------------------------------------------
-- create_diary aún no fija state_id, así que el default garantiza 'libre'
-- en las citas nuevas hasta que se actualice esa función (Paso 2).
DO $$
DECLARE
    v_libre int;
BEGIN
    SELECT id INTO v_libre FROM appointment_states WHERE code = 'libre';
    EXECUTE format('ALTER TABLE appointments ALTER COLUMN state_id SET DEFAULT %s', v_libre);
END $$;

ALTER TABLE appointments
    ALTER COLUMN state_id SET NOT NULL;

COMMIT;
