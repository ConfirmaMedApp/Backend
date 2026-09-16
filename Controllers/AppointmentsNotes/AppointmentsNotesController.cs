using Backend.DTOs.AppointmentsNotes.Requests;
using Backend.Services.AppointmentsNotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.AppointmentsNotes;

[Authorize]
public class AppointmentsNotesController(IAppointmentNoteService appointmentNoteService) : BaseControllerCustom
{
    [HttpPost(Name = "CreateAppointmentNote")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Create([FromBody] AppointmentNoteRequestCreateDto dto)
    {
        var appointmentNote = await appointmentNoteService.CreateAsync(dto);
        return CreatedResponse(appointmentNote);
    }

    [HttpGet("appointments/{appointmentId:int}", Name = "GetAppointmentNotesByAppointment")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromRoute] int appointmentId)
    {
        var appointmentNotes = await appointmentNoteService.GetAllAsync(appointmentId);
        return OkResponse(appointmentNotes);
    }

    [HttpGet("patients/{patientId:int}", Name = "GetAppointmentNotesByPatient")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAllByPatient([FromRoute] int patientId)
    {
        var appointmentNotes = await appointmentNoteService.GetAllByPatientAsync(patientId);
        return OkResponse(appointmentNotes);
    }

    [HttpGet("{id:int}", Name = "GetAppointmentNoteById")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var appointmentNote = await appointmentNoteService.GetByIdAsync(id);
        return OkResponse(appointmentNote);
    }
}
