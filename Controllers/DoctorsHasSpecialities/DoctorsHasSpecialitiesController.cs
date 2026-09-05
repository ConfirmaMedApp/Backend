using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.Services.DoctorsHasSpecialities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.DoctorsHasSpecialities;

public class DoctorsHasSpecialitiesController(IDoctorHasSpecialityService doctorHasSpecialityService) : BaseControllerCustom
{
    [HttpGet("specialities/{specialityId:int}", Name = "GetBySpeciality")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetBySpecialityId(int specialityId)
    {
        var result = await doctorHasSpecialityService.GetBySpecialityIdAsync(specialityId);
        return OkResponse(result);
    }

    [HttpPost]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Create([FromBody] DoctorSpecialityRequestCreateUpdateDto dto)
    {
        var result = await doctorHasSpecialityService.CreateAsync(dto);
        return CreatedResponse(result);
    }
}
