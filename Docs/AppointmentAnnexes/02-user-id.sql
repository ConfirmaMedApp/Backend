-- =============================================================================
-- Anexos de cita: agregar user_id (quién sube el anexo)
-- Branch de base de datos: develop (confirmamed_db_dev)
-- Aditivo y no destructivo sobre los datos: la columna es nullable.
--   - El doctor/staff sube el anexo autenticado -> se guarda su user_id (del JWT).
--   - El paciente lo sube desde el link de la teleconsulta (sin token) -> NULL.
-- No reemplaza a patient_id: patient_id dice de QUIÉN es la cita; user_id dice
-- QUIÉN subió el documento.
-- =============================================================================

BEGIN;

-- 1. Columna user_id en appointments_annexes (nullable) -----------------------
--    Nullable porque el paciente no tiene usuario de plataforma.
ALTER TABLE appointments_annexes
    ADD COLUMN IF NOT EXISTS user_id integer REFERENCES users(id);

-- 2. create_appointment_annex: ahora recibe y guarda user_id ------------------
--    Se elimina la versión de 4 argumentos para no dejar una sobrecarga vieja.
DROP FUNCTION IF EXISTS public.create_appointment_annex(text, text, integer, integer);

CREATE OR REPLACE FUNCTION public.create_appointment_annex(
    p_file text,
    p_name text,
    p_appointment_id integer,
    p_patient_id integer,
    p_user_id integer
)
RETURNS integer
LANGUAGE plpgsql
AS $function$
begin
    insert into appointments_annexes(file, "name", appointment_id, patient_id, user_id, created_at)
    values (p_file, p_name, p_appointment_id, p_patient_id, p_user_id, now());

    return p_appointment_id;
end;
$function$;

-- 3. get_all_appointments_annexes: retornar user_id ---------------------------
--    CREATE OR REPLACE no permite cambiar el tipo de retorno (RETURNS TABLE),
--    por eso se dropea y se recrea.
DROP FUNCTION IF EXISTS public.get_all_appointments_annexes(integer);

CREATE OR REPLACE FUNCTION public.get_all_appointments_annexes(p_appointment_id integer)
RETURNS TABLE(
    id integer,
    file text,
    name text,
    appointmentid integer,
    appointmentdateappointment character varying,
    appointmentstarthour text,
    patientid integer,
    userid integer
)
LANGUAGE plpgsql
AS $function$
begin
    return query select
        aa.id,
        aa.file,
        aa."name",
        a.id as appointmentid,
        a.date_appointment::varchar as appointmentdateappointment,
        to_char(a.start_hour, 'HH24:MI') as appointmentstarthour,
        aa.patient_id as patientid,
        aa.user_id as userid
    from appointments_annexes aa
    inner join appointments a on aa.appointment_id = a.id
    where a.id = p_appointment_id;
end;
$function$;

COMMIT;
