using Backend.DTOs.Appointments.Requests;
using Backend.Services.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Appointments;

[Authorize]
public class AppointmentsController(IAppointmentService appointmentService) : BaseControllerCustom
{
    [HttpPost(Name = "CreateSeveralAppointments")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> CreateSeveralAppointments([FromBody] SeveralAppointmentsRequestCreateDto dto)
    {
        var diary = await appointmentService.CreateSeveralAppointmentsAsync(dto);
        return CreatedResponse(diary);
    }

    [HttpPut("reschedule", Name = "RescheduleAppointment")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> RescheduleToSlot([FromBody] RescheduleToSlotRequestDto dto)
    {
        var reschedule = await appointmentService.RescheduleToSlotAsync(dto);
        return OkResponse(reschedule);
    }

    [HttpGet("occupation/month/{year:int}/{month:int}", Name = "GetOccupationPerMonth")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetOccupationAppointmentsPerMonth([FromRoute] int year, [FromRoute] int month, [FromQuery] int? doctorId)
    {
        var days = await appointmentService.GetOccupationAppointmentsPerMonthAsync(year, month, doctorId);
        return OkResponse(days);
    }

    [HttpGet("dates/{dateSelected}/filters",  Name = "GetAllAppointments")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromRoute] string dateSelected, [FromQuery] int? specialityId, [FromQuery] int? doctorId, [FromQuery] bool? isOccuped, [FromQuery] int? limit, [FromQuery] int? offset)
    {
        var appointments = await appointmentService.GetAllAsync(dateSelected, specialityId, doctorId, isOccuped, limit, offset);
        return OkResponse(appointments);
    }

    [AllowAnonymous]
    [HttpPost("assign", Name = "AssignAppointment")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> AssignAppointment([FromBody] AssignAppointmentRequestDto dto)
    {
        var appointment = await appointmentService.AssignAppointmentAsync(dto);
        return OkResponse(appointment);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetAppointmentById")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var appointment = await appointmentService.GetByIdAsync(id);
        return OkResponse(appointment);
    }

    [AllowAnonymous]
    [HttpGet("patient/need/specialities/{specialityId:int}/date/{dateSelected}/hour/{startHour}", Name = "GetByPatientNeedAppointment")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetByPatientNeedAppointment([FromRoute] int specialityId, [FromRoute] string dateSelected, [FromRoute] string startHour)
    {
        var appointment = await appointmentService.GetByPatientNeedAppointmentAsync(specialityId, startHour, dateSelected);
        return OkResponse(appointment);
    }

    [AllowAnonymous]
    [HttpGet("patient/recommendations/specialities/{specialityId:int}/date/{dateSelected}", Name = "GetRecommendationsForPatient")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetRecommendationsForPatient([FromRoute] int specialityId, [FromRoute] string dateSelected)
    {
        var appointments = await appointmentService.GetRecommendationsForPatientAsync(specialityId, dateSelected);
        return OkResponse(appointments);
    }
}
