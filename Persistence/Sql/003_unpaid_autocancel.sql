-- =====================================================================
-- Auto-cancelación de citas no pagadas
-- Idempotente: se puede correr varias veces sin romper nada.
-- Aplicar en la branch `develop` de Neon (project ConfirmaMed) y luego prod.
--
-- Regla de negocio: una cita 'asignada' que NO esté pagada
-- (payment_status <> 'PAID') se cancela automáticamente cuando faltan
-- `p_minutes_before` minutos (por defecto 15) para su hora de inicio.
-- El scheduler la ejecuta cada minuto ANTES de las transiciones por tiempo,
-- así una cita impaga nunca llega a 'en_atencion'.
--
-- Mismas convenciones que process_appointment_transitions():
--   - hora local de Bogotá (los campos son hora de pared sin zona).
--   - cancelación del sistema: sin usuario, motivo 'impago' para el historial.
--   - el turno queda is_occuped=true (no se reutiliza), igual que cancel_appointment.
-- =====================================================================

CREATE OR REPLACE FUNCTION public.cancel_unpaid_appointments(p_minutes_before integer DEFAULT 15)
RETURNS TABLE(appointment_id integer)
LANGUAGE plpgsql
AS $$
declare
    v_now   timestamp := now() AT TIME ZONE 'America/Bogota';
    v_asig  int := (select id from appointment_states where code = 'asignada');
    v_canc  int := (select id from appointment_states where code = 'cancelada');
begin
    -- Cancelación del sistema: se limpia el usuario y se fija el motivo para
    -- que el trigger de historial registre 'impago' y no herede valores ajenos.
    perform set_config('app.current_user_id', '', true);
    perform set_config('app.state_change_note', '', true);
    perform set_config('app.state_change_reason', 'impago', true);

    return query
        update appointments a
        set state_id    = v_canc,
            is_occuped  = true,
            is_approved = false,
            updated_at  = now()
        where a.state_id = v_asig
          and a.payment_status <> 'PAID'
          and (a.date_appointment + a.start_hour) - (interval '1 minute' * p_minutes_before) <= v_now
        returning a.id;
end;
$$;
