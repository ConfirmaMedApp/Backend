using Backend.DTOs.Payments.Responses;

namespace Backend.Services.Payments;

public interface IAppointmentPaymentService
{
    // Genera (o regenera) el enlace de pago Wompi de una cita y, opcionalmente, lo envía por email.
    Task<PaymentLinkResultDto> GeneratePaymentLinkAsync(int appointmentId, bool sendEmail, CancellationToken ct = default);

    // Genera la configuración (llave pública, referencia, monto y firma) para que el
    // frontend renderice el Widget / Checkout Web embebido de Wompi.
    Task<CheckoutConfigResultDto> GenerateCheckoutConfigAsync(int appointmentId, CancellationToken ct = default);

    // "Valores de la cita": precio en USD + conversión a COP con la TRM vigente.
    Task<AppointmentPricingResponseDto> GetPricingAsync(int appointmentId, CancellationToken ct = default);

    // Precio vigente (USD) de la cita SIN convertir a COP (no consulta la TRM).
    // Sirve para decidir si la cita requiere pago (y por tanto bloquear el video).
    Task<decimal?> GetAppointmentPriceUsdAsync(int appointmentId);

    // Estado de pago de la cita + último enlace generado.
    Task<AppointmentPaymentStatusResponseDto> GetPaymentStatusAsync(int appointmentId);

    // Reconsulta en Wompi el estado de la transacción y actualiza el pago de la cita
    // (fallback/polling cuando el webhook no llega). `transactionId` es el que Wompi
    // devuelve en el redirect (?id=); si es null se usa el ya almacenado, si lo hay.
    Task<AppointmentPaymentStatusResponseDto> RefreshPaymentStatusAsync(int appointmentId, string? transactionId = null, CancellationToken ct = default);

    // Procesa un webhook de Wompi (verifica firma incluida). Devuelve true si se recibió y procesó.
    Task<bool> HandleWebhookAsync(string rawBody, string? checksumHeader);
}
