-- =============================================================================
-- Paso 4: Transiciones automáticas por tiempo
-- Branch de base de datos: develop (confirmamed_db_dev)
--
-- Reglas (sin pagos por ahora):
--   asignada     -> en_atencion   cuando llega start_hour
--   en_atencion  -> finalizada    cuando pasa end_hour
--
-- Todas las comparaciones usan la hora local de Bogotá, porque
-- date_appointment + start/end_hour se almacena como hora de pared local
-- (timestamp sin zona) y la sesión corre en GMT.
-- =============================================================================

CREATE OR REPLACE FUNCTION public.process_appointment_transitions()
RETURNS TABLE(appointment_id integer, to_state text)
LANGUAGE plpgsql
AS $$
declare
    v_now   timestamp := now() AT TIME ZONE 'America/Bogota';
    v_asig  int := (select id from appointment_states where code = 'asignada');
    v_en    int := (select id from appointment_states where code = 'en_atencion');
    v_fin   int := (select id from appointment_states where code = 'finalizada');
begin
    -- asignada -> en_atencion (llegó la hora de inicio)
    perform set_config('app.state_change_reason', 'inicio_automatico', true);
    return query
        update appointments a
        set state_id = v_en, updated_at = now()
        where a.state_id = v_asig
          and (a.date_appointment + a.start_hour) <= v_now
        returning a.id, 'en_atencion'::text;

    -- en_atencion -> finalizada (pasó la hora de fin)
    -- Ve también las filas recién pasadas a en_atencion en la consulta anterior,
    -- así una cita muy vencida termina directamente en 'finalizada'.
    perform set_config('app.state_change_reason', 'finalizacion_automatica', true);
    return query
        update appointments a
        set state_id = v_fin, updated_at = now()
        where a.state_id = v_en
          and (a.date_appointment + a.end_hour) <= v_now
        returning a.id, 'finalizada'::text;
end;
$$;
