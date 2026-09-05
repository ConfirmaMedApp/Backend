using Backend.DTOs.Doctors.Requests;
using Backend.Services.Doctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Doctors;

[Authorize]
public class DoctorsController(IDoctorService doctorService) : BaseControllerCustom
{
    [HttpGet(Name = "GetAllDoctors")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromQuery] int? limit, [FromQuery] int? offset, [FromQuery] bool? status,
        [FromQuery] string search = "")
    {
        var result = await doctorService.GetAllAsync(limit, offset, status, search);
        return OkResponse(result);
    }

    [HttpGet("{id:int}", Name = "GetDoctorById")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await doctorService.GetByIdAsync(id);
        return OkResponse(result);
    }

    [HttpPost(Name = "CreateDoctor")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Create([FromBody] DoctorRequestCreateDto dto)
    {
        var result = await doctorService.CreateAsync(dto);
        return CreatedResponse(result);
    }

    [HttpPut(Name = "UpdateDoctor")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Update([FromBody] DoctorRequestUpdateDto dto)
    {
        var result = await doctorService.UpdateAsync(dto);
        return OkResponse(result);
    }
}