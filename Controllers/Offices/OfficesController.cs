using Backend.DTOs.Offices.Requests;
using Backend.Services.Offices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Offices;

[Authorize]
public class OfficesController(IOfficeService officeService) : BaseControllerCustom
{
   [HttpGet(Name = "GetAllOffices")]
   [EnableRateLimiting("UserPolicy")]
   public async Task<IActionResult> GetAll()
   {
      var result = await officeService.GetAllAsync();
      return OkResponse(result);
   }

   [HttpGet("{id:int}", Name = "GetOfficeById")]
   [EnableRateLimiting("UserPolicy")]
   public async Task<IActionResult> GetById(int id)
   {
      var result = await officeService.GetByIdAsync(id);
      return OkResponse(result);
   }

   [HttpGet("users/{userId:int}", Name = "GetOfficeByUserId")]
   [EnableRateLimiting("UserPolicy")]
   public async Task<IActionResult> GetByUserId(int userId)
   {
      var result = await officeService.GetByUserIdAsync(userId);
      return OkResponse(result);
   }

   [HttpPost(Name = "CreateOffice")]
   [EnableRateLimiting("UserPolicy")]
   public async Task<IActionResult> Create([FromBody] OfficeRequestCreateDto dto)
   {
      var result = await officeService.CreateAsync(dto);
      return CreatedResponse(result);
   }

   [HttpPut(Name = "UpdateOffice")]
   [EnableRateLimiting("UserPolicy")]
   public async Task<IActionResult> Update([FromBody] OfficeRequestUpdateDto dto)
   {
      var result = await officeService.UpdateAsync(dto);
      return OkResponse(result);
   }
}