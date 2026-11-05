-- =====================================================================
-- Widget / Checkout Web embebido de Wompi
-- Idempotente: se puede correr varias veces sin romper nada.
-- Requiere haber aplicado antes 001_wompi_payments.sql.
-- Aplicar en la branch `develop` de Neon (project ConfirmaMed).
-- =====================================================================

-- El flujo del Widget correlaciona el webhook por `wompi_reference` (no por
-- payment_link_id). Índice para esa búsqueda. No es único: en el flujo de
-- enlaces, la referencia la asigna Wompi al llegar el webhook y no queremos
-- arriesgar una violación de unicidad en ese UPDATE.
CREATE INDEX IF NOT EXISTS ix_appointment_payments_reference
    ON appointment_payments (wompi_reference)
    WHERE wompi_reference IS NOT NULL;
