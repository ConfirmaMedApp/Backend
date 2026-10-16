-- =============================================================================
-- Paso 5: filtro por estado en get_all_appointments_by_user
-- Branch de base de datos: develop (confirmamed_db_dev)
--
-- Agrega el parámetro opcional p_state_code. Comportamiento (opción 1):
--   - Si p_state_code viene NULL: la app sigue mandando p_is_occuped=true
--     (regla actual "solo ocupadas").
--   - Si p_state_code viene con valor: la app manda p_is_occuped=NULL y se
--     filtra por el estado indicado (permite ?state=libre, etc.).
-- (DROP necesario: cambia la firma al agregar un parámetro.)
-- =============================================================================

DROP FUNCTION IF EXISTS public.get_all_appointments_by_user(date, integer, integer, boolean, integer, integer);

CREATE FUNCTION public.get_all_appointments_by_user(
    p_date_selected date,
    p_user_id integer,
    p_speciality_id integer DEFAULT NULL::integer,
    p_is_occuped boolean DEFAULT NULL::boolean,
    p_limit integer DEFAULT NULL::integer,
    p_offset integer DEFAULT NULL::integer,
    p_state_code character varying DEFAULT NULL::character varying
)
RETURNS TABLE(
    id integer, dateappointment character varying, starthour character varying, endhour character varying,
    durationid integer, durationinterval character varying, doctorid integer, doctorname character varying,
    doctorlastname character varying, doctordocument character varying, specialityid integer,
    specialityname character varying, specialitycode character varying, patientid integer,
    patientname character varying, patientlastname character varying, patientdocument character varying,
    status boolean, isoccuped boolean, isapproved boolean,
    roomname text, roomurl text, roomcreatedat timestamp without time zone,
    stateid integer, statecode character varying, statename character varying, statecolor character varying
)
LANGUAGE plpgsql
AS $$
declare
    v_doctor_id integer;
begin
    select u.doctor_id into v_doctor_id from users u where u.id = p_user_id;

    return query select
        a.id,
        a.date_appointment::varchar as dateappointment,
        a.start_hour::varchar       as starthour,
        a.end_hour::varchar         as endhour,
        ds.id                       as durationid,
        ds."interval"::varchar      as durationinterval,
        d.id                        as doctorid,
        d."name"                    as doctorname,
        d.lastname                  as doctorlastname,
        d."document"                as doctordocument,
        s.id                        as specialityid,
        s."name"                    as specialityname,
        s.code                      as specialitycode,
        p.id                        as patientid,
        p."name"                    as patientname,
        p.lastname                  as patientlastname,
        p."document"                as patientdocument,
        a.status,
        a.is_occuped                as isoccuped,
        a.is_approved               as isapproved,
        a.room_name                 as roomname,
        a.room_url                  as roomurl,
        a.room_created_at           as roomcreatedat,
        est.id                      as stateid,
        est.code                    as statecode,
        est."name"                  as statename,
        est.color                   as statecolor
    from appointments a
    inner join durations         ds  on a.duration_id   = ds.id
    inner join doctors           d   on a.doctor_id     = d.id
    inner join specialities      s   on a.speciality_id = s.id
    inner join appointment_states est on a.state_id     = est.id
    left  join patients          p   on a.patient_id    = p.id
    where a.date_appointment = p_date_selected
      and d.id = v_doctor_id
      and (p_speciality_id is null or s.id = p_speciality_id)
      and (p_is_occuped    is null or a.is_occuped = p_is_occuped)
      and (p_state_code    is null or est.code = p_state_code)
    order by a.start_hour
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$$;
