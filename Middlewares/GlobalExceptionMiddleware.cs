using System.Text.Json;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.Conflict;
using Backend.Exceptions.DatabaseOperation;
using Backend.Exceptions.Forbidden;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Responses;

namespace Backend.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var response = context.Response;
                response.ContentType = "application/json";
                response.Body = originalBodyStream;

                var apiResponse = new ApiResponseCustom<object>
                {
                    Success = false,
                    Path = context.Request.Path,
                    ResponseTime = DateTime.UtcNow,
                    Items = null
                };

                switch (ex)
                {
                    case NotFoundException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status404NotFound;
                        apiResponse.Message = ex.Message;
                        break;

                    case UnauthorizedException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status401Unauthorized;
                        apiResponse.Message = ex.Message;
                        break;

                    case BadRequestException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status400BadRequest;
                        apiResponse.Message = ex.Message;
                        break;

                    case ForbiddenException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status403Forbidden;
                        apiResponse.Message = ex.Message;
                        break;

                    case ConflictException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status409Conflict;
                        apiResponse.Message = ex.Message;
                        break;

                    case DatabaseOperationException:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        apiResponse.Message = ex.Message;
                        break;

                    default:
                        response.StatusCode = apiResponse.StatusCode = StatusCodes.Status500InternalServerError;
                        apiResponse.Message = "An unknown error occurred.";
                        logger.LogError(ex, "Unhandled exception");
                        break;
                }

                var result = JsonSerializer.Serialize(apiResponse);
                await response.WriteAsync(result);
            }
        }
    }