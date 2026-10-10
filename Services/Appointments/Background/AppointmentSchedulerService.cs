namespace Backend.Services.Appointments.Background;

/// <summary>
/// Corre cada minuto: aplica las transiciones automáticas de estado por tiempo
/// (asignada -> en_atencion -> finalizada) y dispara los recordatorios 24h/2h.
/// </summary>
public class AppointmentSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

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
