using TableFlow.Api.DTOs;
using TableFlow.Api.Models;

namespace TableFlow.Api.Interfaces
{
    public interface ITableService
    {
        Task<IReadOnlyList<TableResponse>> GetAllAsync();

        Task<TableResponse?> GetByIdAsync(int id);

        Task<IReadOnlyList<TableResponse>> GetByRestaurantIdAsync(int restaurantId);

        Task<IReadOnlyList<TableResponse>> GetActiveAsync();

        Task<TableOperationResult> CreateAsync(CreateTableRequest request);

        Task<TableOperationResult> UpdateAsync(int id, UpdateTableRequest request);

        Task<bool> DeleteAsync(int id);
    }
}