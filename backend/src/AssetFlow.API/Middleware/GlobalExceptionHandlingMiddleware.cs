using System.Text.Json;
using AssetFlow.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.API.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(exception, "An unhandled API exception occurred.");
            if (context.Response.HasStarted)
            {
                throw;
            }

            var statusCode = exception switch
            {
                AssetNotFoundException => StatusCodes.Status404NotFound,
                DuplicateSerialNumberException => StatusCodes.Status409Conflict,
                AssetValidationException or ToolArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            ProblemDetails problem = exception is AssetValidationException validationException
                ? new ValidationProblemDetails(validationException.Errors.ToDictionary(item => item.Key, item => item.Value))
                {
                    Status = statusCode,
                    Title = "Validation failed.",
                    Detail = validationException.Message,
                    Instance = context.Request.Path
                }
                : new ProblemDetails
                {
                    Status = statusCode,
                    Title = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
                    Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message,
                    Instance = context.Request.Path
                };

            await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
        }
    }
}
