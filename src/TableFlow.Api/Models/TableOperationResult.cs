using TableFlow.Api.DTOs;

namespace TableFlow.Api.Models
{
    public enum TableOperationStatus
    {
        Success,
        TableNotFound,
        RestaurantNotFound,
        DuplicateNumber,
        TableReassignmentConflict,
        TableCapacityConflict
    }

    public record TableOperationResult(
        TableOperationStatus Status,
        TableResponse? Table = null
    );
}
