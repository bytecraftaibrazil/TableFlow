using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using TableFlow.Api.Controllers;
using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Models;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class TablesControllerTests
{
    [Fact]
    public async Task TransferConflictReturns409ProblemDetails()
    {
        var request = new UpdateTableRequest(2, 1, 4, true);
        var service = new Mock<ITableService>(MockBehavior.Strict);
        service.Setup(s => s.UpdateAsync(10, request))
            .ReturnsAsync(new TableOperationResult(TableOperationStatus.TableReassignmentConflict));
        var problemFactory = new Mock<ProblemDetailsFactory>(MockBehavior.Strict);
        problemFactory.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), It.IsAny<int?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((HttpContext _, int? status, string? title, string? type, string? detail, string? instance)
                => new ProblemDetails { Status = status, Title = title, Type = type, Detail = detail, Instance = instance });
        var controller = new TablesController(service.Object)
        {
            ProblemDetailsFactory = problemFactory.Object,
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Update(10, request);

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Table reassignment conflict", problem.Title);
        Assert.Contains("linked reservations", problem.Detail);
        service.Verify(s => s.UpdateAsync(10, request), Times.Once);
    }
}
