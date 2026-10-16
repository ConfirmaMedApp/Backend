-- =============================================================================
-- Paso 6: Reagendamiento con estados
-- Branch de base de datos: develop (confirmamed_db_dev)
--
-- reschedule_to_slot ahora:
--   * Devuelve INTEGER (el id del nuevo turno), antes devolvía boolean.
--     -> arregla el bug de la respuesta (el repo leía el boolean como id).
--   * Es atómica: el viejo debe seguir 'asignada' y el nuevo 'libre'.
--     Si no, devuelve 0 (la app responde 409).
--   * Nuevo turno -> asignada (ocupado). Viejo turno -> libre, sin paciente,
--     con las banderas de recordatorio reseteadas (para que pueda reutilizarse).
--
-- Las reglas de "misma especialidad" y "destino posterior al original" se validan
-- en el servicio (necesita los datos completos para dar mensajes claros); aquí
-- solo se protege la atomicidad de los estados.
-- (DROP necesario: cambia el tipo de retorno boolean -> integer.)
-- =============================================================================

DROP FUNCTION IF EXISTS public.reschedule_to_slot(integer, integer);

CREATE FUNCTION public.reschedule_to_slot(p_old_id integer, p_new_id integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
declare
    v_patient_id int;
    v_libre      int := (select id from appointment_states where code = 'libre');
    v_asignada   int := (select id from appointment_states where code = 'asignada');
begin
    -- El viejo debe estar 'asignada' (y de ahí tomamos el paciente)
    select patient_id into v_patient_id
    from appointments
    where id = p_old_id and state_id = v_asignada;

    if not found then
        return 0;
    end if;

    -- Ocupar el nuevo solo si sigue 'libre' (cierra la carrera)
    update appointments
    set patient_id        = v_patient_id,
        is_occuped        = true,
        is_approved       = true,
        state_id          = v_asignada,
        reminder_24h_sent = false,
        reminder_2h_sent  = false,
        updated_at        = now()
    where id = p_new_id and state_id = v_libre;

    if not found then
        return 0;
    end if;

    -- Liberar el viejo (vuelve a quedar reutilizable)
    update appointments
    set patient_id        = null,
        is_occuped        = false,
        is_approved       = false,
        state_id          = v_libre,
        reminder_24h_sent = false,
        reminder_2h_sent  = false,
        updated_at        = now()
    where id = p_old_id;

    return p_new_id;
end;
$$;
