-- =============================================================================
-- Paso 3: Cancelar / No asistió + atribución del historial (note)
-- Branch de base de datos: develop (confirmamed_db_dev)
-- =============================================================================

-- 1. Trigger de historial: ahora también captura la nota ----------------------
--    La app fija por transacción:
--      app.current_user_id, app.state_change_reason, app.state_change_note
CREATE OR REPLACE FUNCTION log_appointment_state_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_user   int;
    v_reason varchar(50);
    v_note   text;
BEGIN
    IF TG_OP = 'UPDATE' AND NEW.state_id IS NOT DISTINCT FROM OLD.state_id THEN
        RETURN NEW;
    END IF;

    v_user   := nullif(current_setting('app.current_user_id', true), '')::int;
    v_reason := nullif(current_setting('app.state_change_reason', true), '');
    v_note   := nullif(current_setting('app.state_change_note', true), '');

    INSERT INTO appointment_state_history
        (appointment_id, from_state_id, to_state_id, changed_by, reason, note)
    VALUES (
        NEW.id,
        CASE WHEN TG_OP = 'UPDATE' THEN OLD.state_id ELSE NULL END,
        NEW.state_id,
        v_user,
        v_reason,
        v_note
    );

    RETURN NEW;
END;
$$;

-- 2. cancel_appointment: asignada -> cancelada --------------------------------
--    El turno NO se reutiliza: queda ocupado (is_occuped=true) para no ofrecerse.
--    Devuelve el id si canceló, o 0 si la cita ya no estaba 'asignada'.
CREATE OR REPLACE FUNCTION public.cancel_appointment(p_id integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
begin
    update appointments
    set state_id    = (select id from appointment_states where code = 'cancelada'),
        is_occuped  = true,
        is_approved = false,
        updated_at  = now()
    where id = p_id
      and state_id = (select id from appointment_states where code = 'asignada');

    if not found then
        return 0;
    end if;

    return p_id;
end;
$$;

-- 3. mark_no_show: en_atencion -> no_asistio ----------------------------------
--    Conserva is_occuped/is_approved (la cita existió y fue aprobada).
--    Devuelve el id si aplicó, o 0 si la cita no estaba 'en_atencion'.
CREATE OR REPLACE FUNCTION public.mark_no_show(p_id integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
begin
    update appointments
    set state_id   = (select id from appointment_states where code = 'no_asistio'),
        updated_at = now()
    where id = p_id
      and state_id = (select id from appointment_states where code = 'en_atencion');

    if not found then
        return 0;
    end if;

    return p_id;
end;
$$;

-- 4. get_patients_attended_by_doctor: "atendido" = estado 'finalizada' --------
--    Antes se apoyaba en is_occuped+is_approved+fecha pasada, lo que ahora
--    incluiría por error las citas 'no_asistio'. Mismo tipo de retorno -> REPLACE.
CREATE OR REPLACE FUNCTION public.get_patients_attended_by_doctor(
    p_doctor_id integer,
    p_start_date date DEFAULT NULL::date,
    p_search character varying DEFAULT ''::character varying,
    p_limit integer DEFAULT NULL::integer,
    p_offset integer DEFAULT NULL::integer
)
RETURNS TABLE(
    id integer, name character varying, lastname character varying, email character varying,
    phone character varying, birthdate text, document character varying, documenttypeid integer,
    documenttypename character varying, genderid integer, gendername character varying, status boolean
)
LANGUAGE plpgsql
AS $$
begin
    return query
    select p.id, p."name", p.lastname, p.email, p.phone,
           to_char(p.birthdate, 'yyyy-mm-dd'), p."document",
           dt.id as documenttypeid, dt."name" as documenttypename,
           g.id as genderid, g."name" as gendername, p.status
    from (
        select distinct on (a.patient_id) a.patient_id, a.date_appointment, a.start_hour
        from appointments a
        inner join appointment_states est on a.state_id = est.id
        where a.doctor_id = p_doctor_id
          and a.status = true
          and est.code = 'finalizada'
          and (p_start_date is null or a.date_appointment >= p_start_date)
        order by a.patient_id, a.date_appointment desc, a.start_hour desc
    ) last_visit
    inner join patients p        on p.id = last_visit.patient_id
    inner join document_types dt on p.document_type_id = dt.id
    inner join genders g         on p.gender_id = g.id
    where p.status = true
      and (
            coalesce(p_search, '') = ''
         or p."document" ilike '%' || p_search || '%'
         or p."name"     ilike '%' || p_search || '%'
         or p.lastname   ilike '%' || p_search || '%'
         or concat(p."name", ' ', p.lastname) ilike '%' || p_search || '%'
      )
    order by last_visit.date_appointment desc, last_visit.start_hour desc
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$$;
