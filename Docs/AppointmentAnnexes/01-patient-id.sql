-- =============================================================================
-- Anexos de cita: agregar patient_id
-- Branch de base de datos: develop (confirmamed_db_dev)
-- Aditivo y no destructivo sobre los datos: la columna es nullable y se
-- rellena con el patient_id de la cita al subir el documento (lo suba el
-- doctor o el paciente desde la teleconsulta).
-- =============================================================================

BEGIN;

-- 1. Columna patient_id en appointments_annexes (nullable) --------------------
--    Nullable porque una cita puede no tener paciente asignado todavía.
ALTER TABLE appointments_annexes
    ADD COLUMN IF NOT EXISTS patient_id integer REFERENCES patients(id);

-- 2. create_appointment_annex: ahora recibe y guarda patient_id ---------------
--    Se elimina la versión de 3 argumentos para no dejar una sobrecarga vieja.
DROP FUNCTION IF EXISTS public.create_appointment_annex(text, text, integer);

CREATE OR REPLACE FUNCTION public.create_appointment_annex(
    p_file text,
    p_name text,
    p_appointment_id integer,
    p_patient_id integer
)
RETURNS integer
LANGUAGE plpgsql
AS $function$
begin
    insert into appointments_annexes(file, "name", appointment_id, patient_id, created_at)
    values (p_file, p_name, p_appointment_id, p_patient_id, now());

    return p_appointment_id;
end;
$function$;

-- 3. get_all_appointments_annexes: retornar patient_id ------------------------
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
    patientid integer
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
        aa.patient_id as patientid
    from appointments_annexes aa
    inner join appointments a on aa.appointment_id = a.id
    where a.id = p_appointment_id;
end;
$function$;

COMMIT;
