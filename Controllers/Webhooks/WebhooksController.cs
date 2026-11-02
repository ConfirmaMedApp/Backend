using Backend.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController(IAppointmentPaymentService appointmentPaymentService) : ControllerBase
{
    [HttpPost("wompi", Name = "WompiWebhook")]
    public async Task<IActionResult> Wompi()
    {
        // Leemos el cuerpo crudo para poder verificar la firma exactamente como lo envió Wompi.
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        var checksum = Request.Headers["X-Event-Checksum"].FirstOrDefault();

        await appointmentPaymentService.HandleWebhookAsync(rawBody, checksum);

        // Siempre respondemos 200 para que Wompi no reintente un evento ya recibido.
        return Ok(new { received = true });
    }
}
