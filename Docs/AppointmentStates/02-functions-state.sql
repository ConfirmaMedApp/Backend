-- =============================================================================
-- Paso 2: Funciones SQL que usan state_id
-- Branch de base de datos: develop (confirmamed_db_dev)
-- Mantiene is_occuped / is_approved sincronizados (el frontend aún los usa).
-- =============================================================================

-- 1. assignment_patient: fija estado 'asignada' y cierra la doble asignación ---
--    El UPDATE solo aplica si la cita sigue 'libre'. Devuelve el id si asignó,
--    o 0 si el turno ya no estaba libre (carrera / ya asignado).
CREATE OR REPLACE FUNCTION public.assignment_patient(p_appointment_id integer, p_patient_id integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
begin
    update appointments a
    set patient_id  = p_patient_id,
        is_occuped  = true,
        is_approved = true,
        state_id    = (select id from appointment_states where code = 'asignada'),
        updated_at  = now()
    where a.id = p_appointment_id
      and a.state_id = (select id from appointment_states where code = 'libre');

    if not found then
        return 0;
    end if;

    return p_appointment_id;
end;
$$;

-- 2. reschedule_to_slot: nuevo -> asignada, viejo -> libre ---------------------
CREATE OR REPLACE FUNCTION public.reschedule_to_slot(p_old_id integer, p_new_id integer)
RETURNS boolean
LANGUAGE plpgsql
AS $$
declare
    v_patient_id int;
    v_libre      int := (select id from appointment_states where code = 'libre');
    v_asignada   int := (select id from appointment_states where code = 'asignada');
begin
    IF NOT public.has_patient_assigned(p_old_id) THEN RETURN FALSE; END IF;
    IF NOT public.is_slot_available(p_new_id)   THEN RETURN FALSE; END IF;

    select patient_id into v_patient_id from appointments where id = p_old_id;

    -- Ocupar nuevo slot
    update appointments
    set patient_id  = v_patient_id,
        is_occuped  = true,
        is_approved = true,
        state_id    = v_asignada,
        updated_at  = now()
    where id = p_new_id;

    -- Liberar slot antiguo
    update appointments
    set patient_id  = null,
        is_occuped  = false,
        is_approved = false,
        state_id    = v_libre,
        updated_at  = now()
    where id = p_old_id;

    return true;
end;
$$;

-- 3. get_appointment_by_id: + estado ------------------------------------------
-- (DROP necesario: cambia el tipo de retorno al agregar columnas de estado)
DROP FUNCTION IF EXISTS public.get_appointment_by_id(integer);
CREATE OR REPLACE FUNCTION public.get_appointment_by_id(p_id integer)
RETURNS TABLE(
    id integer, dateappointment character varying, starthour character varying, endhour character varying,
    durationid integer, durationinterval character varying, doctorid integer, doctorname character varying,
    doctorlastname character varying, doctordocument character varying, specialityid integer,
    specialityname character varying, specialitycode character varying, patientid integer,
    patientname character varying, patientlastname character varying, patientdocument character varying,
    status boolean, isoccuped boolean, isapproved boolean, userid integer,
    roomname text, roomurl text, roomcreatedat timestamp without time zone,
    stateid integer, statecode character varying, statename character varying, statecolor character varying
)
LANGUAGE plpgsql
AS $$
begin
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
        a.user_id                   as userid,
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
    where a.id = p_id;
end;
$$;

-- 4. get_all_appointments: + estado y filtro opcional p_state_code ------------
-- (DROP necesario: cambia firma -nuevo parámetro- y tipo de retorno)
DROP FUNCTION IF EXISTS public.get_all_appointments(date, integer, integer, boolean, integer, integer);
CREATE OR REPLACE FUNCTION public.get_all_appointments(
    p_date_selected date,
    p_speciality_id integer DEFAULT NULL::integer,
    p_doctor_id integer DEFAULT NULL::integer,
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
begin
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
      and (p_speciality_id is null or s.id = p_speciality_id)
      and (p_doctor_id     is null or d.id = p_doctor_id)
      and (p_is_occuped    is null or a.is_occuped = p_is_occuped)
      and (p_state_code    is null or est.code = p_state_code)
    order by a.start_hour
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$$;

-- 5. get_all_appointments_by_user: + estado -----------------------------------
-- (DROP necesario: cambia el tipo de retorno al agregar columnas de estado)
DROP FUNCTION IF EXISTS public.get_all_appointments_by_user(date, integer, integer, boolean, integer, integer);
CREATE OR REPLACE FUNCTION public.get_all_appointments_by_user(
    p_date_selected date,
    p_user_id integer,
    p_speciality_id integer DEFAULT NULL::integer,
    p_is_occuped boolean DEFAULT NULL::boolean,
    p_limit integer DEFAULT NULL::integer,
    p_offset integer DEFAULT NULL::integer
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
    order by a.start_hour
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$$;
