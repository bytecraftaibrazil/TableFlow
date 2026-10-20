using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TableFlow.Api.Data;

public sealed class ConflictExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        if (exception is not ResourceConflictException
            && exception is not SqlException { Number: 1205 }
            && exception is not DbUpdateException { InnerException: SqlException { Number: 1205 } })
            return false;

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Resource conflict",
                Detail = exception is ResourceConflictException ? exception.Message : "Concurrent operation conflicted. Retry the request."
            }
        });
    }
}
