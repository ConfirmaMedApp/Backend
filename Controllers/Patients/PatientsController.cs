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
    public async Task<IActionResult> GetByDocumentAsync([FromQuery] string document)
    {
        document = document.Trim();
        var patient = await patientService.GetByDocumentAsync(document);
        return OkResponse(patient);
    }

    [AllowAnonymous]
    [HttpPost(Name = "CreatePatient")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> CreateAsync([FromBody] PatientRequestCreateDto request)
    {
        var patient = await patientService.CreateAsync(request);
        return CreatedResponse(patient);
    }

    [AllowAnonymous]
    [HttpPut(Name = "UpdatePatient")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> UpdateAsync([FromBody] PatientRequestUpdateDto request)
    {
        var patient = await patientService.UpdateAsync(request);
        return OkResponse(patient);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetPatientById")]
    [EnableRateLimiting("ClientPolicy")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var patient = await patientService.GetByIdAsync(id);
        return OkResponse(patient);
    }

    [HttpGet(Name = "GetAllPatients")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAllAsync([FromQuery] int? limit, [FromQuery] int? offset, [FromQuery] string search = "")
    {
        var patients = await patientService.GetAllAsync(limit, offset, search);
        return OkResponse(patients);
    }
}
