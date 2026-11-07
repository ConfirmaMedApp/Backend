namespace Backend.Services.Appointments.Background;

/// <summary>
/// Corre cada minuto: cancela las citas no pagadas que están por iniciar,
/// aplica las transiciones automáticas de estado por tiempo
/// (asignada -> en_atencion -> finalizada) y dispara los recordatorios 24h/2h.
/// </summary>
public class AppointmentSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    // Minutos antes del inicio en que una cita sin pagar se cancela automáticamente.
    private const int UnpaidCancelMinutesBefore = 15;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AppointmentSchedulerService iniciado (intervalo: {Interval})", Interval);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();

                    // Primero cancelamos las impagas por iniciar, para que no pasen a 'en_atencion'.
                    await appointmentService.ProcessUnpaidCancellationsAsync(UnpaidCancelMinutesBefore);
                    await appointmentService.ProcessAutomaticTransitionsAsync();
                    await appointmentService.ProcessRemindersAsync(24);
                    await appointmentService.ProcessRemindersAsync(2);
                }
                catch (Exception ex)
                {
                    // Un fallo en un ciclo no debe detener el servicio.
                    logger.LogError(ex, "Error en el ciclo del AppointmentSchedulerService");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Apagado normal de la aplicación.
        }

        logger.LogInformation("AppointmentSchedulerService detenido");
    }
}
