using Backend.DTOs.Payments.Responses;
using Backend.Entities.Payments;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Payments;

public class AppointmentPaymentRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IAppointmentPaymentRepository
{
    public Task<int> CreateAsync(AppointmentPayment payment)
    {
        const string command = """
            INSERT INTO appointment_payments
                (appointment_id, wompi_payment_link_id, checkout_url, amount_usd, trm_used,
                 amount_in_cents, currency, wompi_transaction_id, wompi_reference, status, expires_at)
            VALUES
                (@AppointmentId, @WompiPaymentLinkId, @CheckoutUrl, @AmountUsd, @TrmUsed,
                 @AmountInCents, @Currency, @WompiTransactionId, @WompiReference, @Status, @ExpiresAt)
            RETURNING id;
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteScalarAsync<int>(command, payment), dbConnectionFactory);
    }

    public Task<AppointmentPayment?> GetByPaymentLinkIdAsync(string paymentLinkId)
    {
        const string query = $"""
            SELECT {SelectColumns}
            FROM appointment_payments
            WHERE wompi_payment_link_id = @PaymentLinkId
            LIMIT 1;
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<AppointmentPayment>(query, new { PaymentLinkId = paymentLinkId }),
            dbConnectionFactory);
    }

    public Task<AppointmentPayment?> GetLatestByAppointmentIdAsync(int appointmentId)
    {
        const string query = $"""
            SELECT {SelectColumns}
            FROM appointment_payments
            WHERE appointment_id = @AppointmentId
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<AppointmentPayment>(query, new { AppointmentId = appointmentId }),
            dbConnectionFactory);
    }

    public Task<AppointmentPaymentInfoDto?> GetAppointmentPaymentInfoAsync(int appointmentId)
    {
        // Precio de la cita con respaldo al precio del combo doctor+especialidad.
        const string query = """
            SELECT
                a.id                                    AS "AppointmentId",
                a.doctor_id                             AS "DoctorId",
                a.speciality_id                         AS "SpecialityId",
                COALESCE(a.price_usd, dhs.price_usd)    AS "PriceUsd",
                a.payment_status                        AS "PaymentStatus"
            FROM appointments a
            LEFT JOIN doctors_has_specialities dhs
                ON dhs.doctor_id = a.doctor_id AND dhs.speciality_id = a.speciality_id
            WHERE a.id = @Id
            LIMIT 1;
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<AppointmentPaymentInfoDto>(query, new { Id = appointmentId }),
            dbConnectionFactory);
    }

    public Task<int?> ApplyTransactionUpdateByLinkIdAsync(
        string paymentLinkId,
        string status,
        string? transactionId,
        string? reference,
        DateTime? paidAt,
        bool markAppointmentPaid)
    {
        const string updatePayment = """
            UPDATE appointment_payments
            SET status = @Status,
                wompi_transaction_id = COALESCE(@TransactionId, wompi_transaction_id),
                wompi_reference = COALESCE(@Reference, wompi_reference),
                paid_at = COALESCE(@PaidAt, paid_at),
                updated_at = now()
            WHERE wompi_payment_link_id = @PaymentLinkId
            RETURNING appointment_id;
            """;

        return ApplyTransactionUpdateCoreAsync(updatePayment, new
        {
            Status = status,
            TransactionId = transactionId,
            Reference = reference,
            PaidAt = paidAt,
            PaymentLinkId = paymentLinkId
        }, markAppointmentPaid);
    }

    public Task<int?> ApplyTransactionUpdateByReferenceAsync(
        string reference,
        string status,
        string? transactionId,
        DateTime? paidAt,
        bool markAppointmentPaid)
    {
        const string updatePayment = """
            UPDATE appointment_payments
            SET status = @Status,
                wompi_transaction_id = COALESCE(@TransactionId, wompi_transaction_id),
                paid_at = COALESCE(@PaidAt, paid_at),
                updated_at = now()
            WHERE wompi_reference = @Reference
            RETURNING appointment_id;
            """;

        return ApplyTransactionUpdateCoreAsync(updatePayment, new
        {
            Status = status,
            TransactionId = transactionId,
            PaidAt = paidAt,
            Reference = reference
        }, markAppointmentPaid);
    }

    // Ejecuta el UPDATE del pago y, si corresponde, marca la cita como pagada, en una transacción.
    private Task<int?> ApplyTransactionUpdateCoreAsync(string updatePayment, object parameters, bool markAppointmentPaid)
    {
        const string markPaid = "UPDATE appointments SET payment_status = 'PAID' WHERE id = @AppointmentId;";

        return ExecuteSafeAsync<int?>(async conn =>
        {
            using var tx = conn.BeginTransaction();

            var appointmentId = await conn.ExecuteScalarAsync<int?>(updatePayment, parameters, tx);

            if (appointmentId is null)
            {
                tx.Rollback();
                return null;
            }

            if (markAppointmentPaid)
                await conn.ExecuteAsync(markPaid, new { AppointmentId = appointmentId.Value }, tx);

            tx.Commit();
            return appointmentId;
        }, dbConnectionFactory);
    }

    private const string SelectColumns = """
        id                     AS "Id",
        appointment_id         AS "AppointmentId",
        wompi_payment_link_id  AS "WompiPaymentLinkId",
        checkout_url           AS "CheckoutUrl",
        amount_usd             AS "AmountUsd",
        trm_used               AS "TrmUsed",
        amount_in_cents        AS "AmountInCents",
        currency               AS "Currency",
        wompi_transaction_id   AS "WompiTransactionId",
        wompi_reference        AS "WompiReference",
        status                 AS "Status",
        expires_at             AS "ExpiresAt",
        paid_at                AS "PaidAt",
        created_at             AS "CreatedAt",
        updated_at             AS "UpdatedAt"
        """;
}
