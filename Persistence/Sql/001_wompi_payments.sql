-- =====================================================================
-- Pagos de citas con Wompi (enlaces de pago)
-- Idempotente: se puede correr varias veces sin romper nada.
-- Aplicar en la branch `develop` de Neon (project ConfirmaMed).
-- =====================================================================

-- 1) Precio (USD) por combinación doctor + especialidad.
--    Un mismo doctor puede cobrar distinto por especialidad, y dos
--    doctores pueden cobrar distinto por la misma especialidad.
ALTER TABLE doctors_has_specialities
    ADD COLUMN IF NOT EXISTS price_usd NUMERIC(12,2);

-- 2) Precio (USD) copiado a la cita al crearla (foto del valor) +
--    estado de pago de la cita para filtrar/mostrar rápido.
ALTER TABLE appointments
    ADD COLUMN IF NOT EXISTS price_usd NUMERIC(12,2);

ALTER TABLE appointments
    ADD COLUMN IF NOT EXISTS payment_status VARCHAR(20) NOT NULL DEFAULT 'UNPAID';

-- 3) Pagos: cada enlace Wompi generado para una cita + el resultado
--    de la transacción que llega por webhook.
CREATE TABLE IF NOT EXISTS appointment_payments (
    id                     SERIAL PRIMARY KEY,
    appointment_id         INT NOT NULL REFERENCES appointments(id),
    wompi_payment_link_id  VARCHAR(64),
    checkout_url           TEXT,
    amount_usd             NUMERIC(12,2)  NOT NULL,   -- valor en USD mostrado
    trm_used               NUMERIC(14,4)  NOT NULL,   -- TRM aplicada USD->COP
    amount_in_cents        BIGINT         NOT NULL,   -- COP en centavos enviado a Wompi
    currency               VARCHAR(3)     NOT NULL DEFAULT 'COP',
    wompi_transaction_id   VARCHAR(64),
    wompi_reference        VARCHAR(64),
    status                 VARCHAR(20)    NOT NULL DEFAULT 'PENDING', -- PENDING/APPROVED/DECLINED/VOIDED/ERROR
    expires_at             TIMESTAMPTZ,
    paid_at                TIMESTAMPTZ,
    created_at             TIMESTAMPTZ    NOT NULL DEFAULT now(),
    updated_at             TIMESTAMPTZ    NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_appointment_payments_appointment
    ON appointment_payments (appointment_id);

-- Un payment_link_id es único cuando existe (se usa para correlacionar el webhook).
CREATE UNIQUE INDEX IF NOT EXISTS ux_appointment_payments_link
    ON appointment_payments (wompi_payment_link_id)
    WHERE wompi_payment_link_id IS NOT NULL;
