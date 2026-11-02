using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.Services.DoctorsHasSpecialities;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
    [HttpPut("price", Name = "SetDoctorSpecialityPrice")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> SetPrice([FromBody] DoctorSpecialityPriceRequestDto dto)
    {
        var result = await doctorHasSpecialityService.SetPriceAsync(dto);
        return OkResponse(result, "Precio actualizado satisfactoriamente");
    }

    [Authorize]
    [HttpGet("prices", Name = "GetDoctorSpecialityPrices")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetPrices([FromQuery] int? doctorId, [FromQuery] int? specialityId)
    {
        var result = await doctorHasSpecialityService.GetPricesAsync(doctorId, specialityId);
        return OkResponse(result);
    }
}
