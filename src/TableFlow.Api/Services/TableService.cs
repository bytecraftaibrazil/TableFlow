using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Services
{
    public class TableService : ITableService
    {
        private static readonly List<TableResponse> Tables =
        [
            new(1, 1, 10, 4, true),
            new(2, 1, 11, 2, true),
            new(3, 1, 12, 6, false),
            new(4, 2, 1, 4, true),
            new(5, 2, 2, 8, true),
            new(6, 3, 20, 2, true),
            new(7, 3, 21, 6, true)
        ];

        #region Get
        public IReadOnlyList<TableResponse> GetAll()
        {
            return Tables;
        }

        public TableResponse? GetById(int id)
        {
            return Tables.FirstOrDefault(t => t.Id == id);
        }

        public IReadOnlyList<TableResponse> GetByRestaurantId(int restaurantId)
        {
            return Tables.Where(
                t => t.RestaurantId == restaurantId).ToList();
        }

        public IReadOnlyList<TableResponse> GetActive()
        {
            return Tables.Where(t => t.IsActive == true).ToList();
        }
        #endregion

        public TableResponse Create(CreateTableRequest request)
        {
            var nextId = Tables.Count == 0
            ? 1 : Tables.Max(t => t.Id) + 1;

            var table = new TableResponse
            (
                nextId,
                request.RestaurantId,
                request.Number,
                request.Capacity,
                request.IsActive
            );

            Tables.Add(table);

            return table;
        }

        public TableResponse? Update(int id, UpdateTableRequest request)
        {
            var tableIndex = Tables.FindIndex(t => t.Id == id);

            if (tableIndex == -1)
                return null;

            var updatedTable = new TableResponse
            (
                id,
                request.RestaurantId,
                request.Number,
                request.Capacity,
                request.IsActive
            );

            Tables[tableIndex] = updatedTable;

            return updatedTable;
        }

        public bool Delete(int id)
        {
            var table = Tables.FirstOrDefault(t => t.Id == id);

            if (table is null)
                return false;

            Tables.Remove(table);

            return true;
        }
    }
}