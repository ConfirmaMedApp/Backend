using Backend.Services.Genders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.Genders;

[Authorize]
public class GendersController(IGenderService genderService) : BaseControllerCustom
{
    [AllowAnonymous]
    [HttpGet(Name = "GetAllGenders")]
    public async Task<IActionResult> GetAll()
    {
        var result = await genderService.GetAllAsync();
        return OkResponse(result);
    }
}