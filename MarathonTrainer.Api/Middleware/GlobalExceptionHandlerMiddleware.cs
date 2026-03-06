namespace MarathonTrainer.Api.Middleware;

public record ErrorResponse(string Error);

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            // InvalidOperationException is used for business rule violations with user-friendly messages.
            logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse("An unexpected error occurred."));
        }
    }
}
