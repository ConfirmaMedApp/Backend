-- =============================================================================
-- Paso 7: función para consultar el historial de estados de una cita
-- Branch de base de datos: develop (confirmamed_db_dev)
-- Devuelve la línea de tiempo (orden ascendente) con estado origen/destino,
-- razón, nota, quién lo hizo (NULL = sistema) y cuándo.
-- =============================================================================

CREATE OR REPLACE FUNCTION public.get_appointment_history(p_appointment_id integer)
RETURNS TABLE(
    id integer,
    fromstateid integer, fromstatecode character varying, fromstatename character varying, fromstatecolor character varying,
    tostateid integer, tostatecode character varying, tostatename character varying, tostatecolor character varying,
    reason character varying, note text,
    changedbyid integer, changedbyname character varying, changedbylastname character varying, changedbyrole character varying,
    createdat timestamp without time zone
)
LANGUAGE plpgsql
AS $$
begin
    return query
    select
        h.id,
        fs.id, fs.code, fs."name", fs.color,
        ts.id, ts.code, ts."name", ts.color,
        h.reason, h.note,
        u.id, u."name", u.lastname, u.role,
        h.created_at
    from appointment_state_history h
    inner join appointment_states ts on ts.id = h.to_state_id
    left  join appointment_states fs on fs.id = h.from_state_id
    left  join users u               on u.id  = h.changed_by
    where h.appointment_id = p_appointment_id
    order by h.created_at, h.id;
end;
$$;
