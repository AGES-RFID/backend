using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Common.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleError(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException keyNotFound => NotFound(new { 
                error = keyNotFound.Message,
                code = "KEY_NOT_FOUND"
            }),
            InvalidOperationException invalidOperation => BadRequest(new { 
                error = invalidOperation.Message,
                code = "INVALID_OPERATION"
            }),
            ArgumentException argumentException => BadRequest(new { 
                error = argumentException.Message,
                code = "INVALID_ARGUMENT"
            }),
            UnauthorizedAccessException unauthorized => Unauthorized(new { 
                error = unauthorized.Message,
                code = "UNAUTHORIZED"
            }),
            _ => StatusCode(500, new { 
                error = "Ocorreu um erro interno no servidor",
                code = "INTERNAL_SERVER_ERROR"
            })
        };
    }

    protected IActionResult HandleValidationResult(ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new { 
                error = "Dados inválidos",
                code = "VALIDATION_ERROR",
                details = errors
            });
        }

        return null;
    }

    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    protected IActionResult Execute<T>(Func<T> operation)
    {
        try
        {
            var result = operation();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    protected IActionResult CreatedResponse<T>(T resource, string routeName = null, object routeValues = null)
    {
        if (routeName != null && routeValues != null)
        {
            return CreatedAtRoute(routeName, routeValues, resource);
        }

        return Created(string.Empty, resource);
    }

    protected IActionResult NoContentResponse()
    {
        return NoContent();
    }
}
