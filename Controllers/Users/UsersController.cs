using Backend.DTOs.Users.Requests;
using Backend.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Users;

[Authorize]
public class UsersController(IUserService userService) : BaseControllerCustom
{
    [HttpGet(Name = "GetAllUsers")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll([FromQuery] int? limit, [FromQuery] int? offset, [FromQuery] bool? status, [FromQuery] string search = "")
    {
        var result = await userService.GetAllAsync(limit, offset, status, search);
        return OkResponse(result);
    }

    [HttpGet("{id:int}", Name = "GetUserById")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await userService.GetByIdAsync(id);
        return OkResponse(result);
    }

    [HttpPost(Name = "CreateUser")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Create([FromBody] UserRequestCreateDto dto)
    {
        var result = await userService.CreateAsync(dto);
        return CreatedResponse(result);
    }

    [HttpPut(Name = "UpdateUser")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> Update([FromBody] UserRequestUpdateDto dto)
    {
        var result = await userService.UpdateAsync(dto);
        return OkResponse(result);
    }
}
