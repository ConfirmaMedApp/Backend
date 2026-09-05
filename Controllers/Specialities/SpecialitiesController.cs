using Backend.DTOs.Specialities.Requests;
using Backend.Services.Specialities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Specialities;

[Authorize]
public class SpecialitiesController(ISpecialityService specialityService) : BaseControllerCustom
{
    [AllowAnonymous]
    [HttpGet(Name = "GetAllSpecialities")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromQuery] int? limit = 5, [FromQuery] int? offset = 0, [FromQuery] bool? status = true, [FromQuery] string search = "")
    {
        var results = await specialityService.GetAllAsync(limit, offset, status, search);
        return OkResponse(results);
    }

    [HttpGet("{id}", Name = "GetSpecialityById")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await specialityService.GetByIdAsync(id);
        return OkResponse(result);
    }

    [HttpPost(Name = "CreateSpeciality")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Create([FromBody] SpecialityRequestCreateDto dto)
    {
        var result = await specialityService.CreateAsync(dto);
        return CreatedResponse(result);
    }

    [HttpPut(Name = "UpdateSpeciality")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Update([FromBody] SpecialityRequestUpdatedDto dto)
    {
        var result = await specialityService.UpdateAsync(dto);
        return OkResponse(result);
    }
}
