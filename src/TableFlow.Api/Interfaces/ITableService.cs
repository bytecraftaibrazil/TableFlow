using TableFlow.Api.DTOs;

namespace TableFlow.Api.Interfaces
{
    public interface ITableService
    {
        IReadOnlyList<TableResponse> GetAll();

        TableResponse? GetById(int id);

        IReadOnlyList<TableResponse> GetByRestaurantId(int restaurantId);

        IReadOnlyList<TableResponse> GetActive();

        TableResponse Create(CreateTableRequest request);
    }
}