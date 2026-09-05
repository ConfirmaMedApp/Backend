using Backend.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseControllerCustom : ControllerBase
{
    // Response model for getting sources
    protected IActionResult OkResponse<T>(T data, string message = "Recurso obtenido satisfactoriamente", int statusCode = 200)
    {
        var response = new ApiResponseCustom<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Path = HttpContext.Request.Path,
            ResponseTime = DateTime.UtcNow,
            Items = data
        };

        return StatusCode(statusCode, response);
    }

    // Response model for created source
    protected IActionResult CreatedResponse<T>(T data, string? message = null, int statusCode = 201)
    {
        var finalMessage = string.IsNullOrWhiteSpace(message) ? "Recurso creado satisfactoriamente" : message;
        var response = new ApiResponseCustom<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = finalMessage,
            Path = HttpContext.Request.Path,
            ResponseTime = DateTime.UtcNow,
            Items = data
        };

        return StatusCode(statusCode, response);
    }

    // Response model for endpoints without response
    protected IActionResult NoContentResponse(string message = "No hay contenido", int statusCode = 204)
    {
        var response = new ApiResponseCustom<object>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Path = HttpContext.Request.Path,
            ResponseTime = DateTime.UtcNow,
            Items = null
        };

        return StatusCode(statusCode, response);
    }
}