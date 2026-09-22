using Backend.DTOs.Patients.Requests;
using Backend.Services.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Patients;

[Authorize]
public class PatientsController(IPatientService patientService) : BaseControllerCustom
{
    [AllowAnonymous]
    [HttpGet("by-document", Name = "GetPatientByDocument")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetByDocument([FromQuery] string document)
    {
        document = document.Trim();
        var patient = await patientService.GetByDocumentAsync(document);
        return OkResponse(patient);
    }

    [HttpPost(Name = "CreatePatient")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> Create([FromBody] PatientRequestCreateDto request)
    {
        var patient = await patientService.CreateAsync(request);
        return CreatedResponse(patient);
    }

    [AllowAnonymous]
    [HttpPost("public/usage", Name = "CreatePatientPublic")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> CreatePublic([FromBody] PatientRequestCreateDto request)
    {
        var patient = await patientService.CreateAsync(request);
        return CreatedResponse(patient);
    }

    [HttpPut(Name = "UpdatePatient")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> Update([FromBody] PatientRequestUpdateDto request)
    {
        var patient = await patientService.UpdateAsync(request);
        return OkResponse(patient);
    }

    [AllowAnonymous]
    [HttpPut("public/usage", Name = "UpdatePatientPublic")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> UpdatePublic([FromBody] PatientRequestUpdateDto request)
    {
        var patient = await patientService.UpdateAsync(request);
        return OkResponse(patient);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetPatientById")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var patient = await patientService.GetByIdAsync(id);
        return OkResponse(patient);
    }

    [HttpGet(Name = "GetAllPatients")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromQuery] int? limit, [FromQuery] int? offset, [FromQuery] bool? status, [FromQuery] string search = "")
    {
        var patients = await patientService.GetAllAsync(limit, offset, status, search);
        return OkResponse(patients);
    }

    [HttpGet("doctor/attended", Name = "GetPatientsAttendedByDoctor")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAttendedByDoctor(
        [FromQuery] string? startDate,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        [FromQuery] int? offset)
    {
        var patients = await patientService.GetAttendedByDoctorAsync(startDate, search ?? string.Empty, limit, offset);
        return OkResponse(patients);
    }
}
