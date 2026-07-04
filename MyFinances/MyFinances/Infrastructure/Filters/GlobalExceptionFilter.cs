using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyFinances.Infrastructure.Filters;

public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Erro nao tratado na aplicacao. Path: {Path}", context.HttpContext.Request.Path);

        var response = new { erro = "Erro interno do servidor. Tente novamente mais tarde." };
        context.Result = new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
