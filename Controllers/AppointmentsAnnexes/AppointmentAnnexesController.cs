using Backend.DTOs.AppointmentsAnnexes.Requests;
using Backend.Services.AppointmentsAnnexes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.AppointmentsAnnexes;

public class AppointmentAnnexesController(IAppointmentAnnexService appointmentAnnexService) : BaseControllerCustom
{
    [HttpGet("/appointments/{appointmentId:int}", Name = "GetAppointmentAnnexesByAppointment")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetAll([FromRoute] int appointmentId)
    {
        var results = await appointmentAnnexService.GetAllAsync(appointmentId);
        return OkResponse(results);
    }

    [AllowAnonymous]
    [HttpPost("appointments/{appointmentId}/annexes", Name = "CreateSeveralAppointmentAnnexes")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> CreateSeveral([FromRoute] int appointmentId, [FromForm] List<IFormFile> files)
    {
        var dtos = files.Select((file, index) => new AppointmentAnnexRequestCreateDto
        {
            File = file,
            AppointmentId = appointmentId
        }).ToArray();
        
        var result = await appointmentAnnexService.CreateSeveralAsync(dtos);
        return CreatedResponse(result);
    }
}
