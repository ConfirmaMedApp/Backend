using Backend.Services.Durations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers.Durations;

[Authorize]
public class DurationsController(IDurationService durationService) : BaseControllerCustom
{
    [HttpGet(Name = "GetAllDurations")]
    [EnableRateLimiting("UserPolicy")]
    public async Task<IActionResult> GetAll()
    {
        var durations =  await durationService.GetAllAsync();
        return OkResponse(durations);
    }
}
