using Backend.Services.DocumentTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.DocumentTypes;

[Authorize]
public class DocumentTypesController(IDocumentTypeService documentTypeService) : BaseControllerCustom
{
    [AllowAnonymous]
    [HttpGet(Name = "GetAllDocumentTypes")]
    public async Task<IActionResult> GetAllDocumentTypes()
    {
        var result = await documentTypeService.GetAllAsync();
        return OkResponse(result);
    }
}