using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Models;

namespace TableFlow.Api.Services
{
    public class TableService : ITableService
    {
        private readonly TableFlowDbContext _dbContext;

        public TableService(TableFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static TableResponse ToResponse(
            RestaurantTable table
        )
        {
            return new TableResponse(
                table.Id,
                table.RestaurantId,
                table.Number,
                table.Capacity,
                table.IsActive
            );
        }

        #region Get
        public async Task<IReadOnlyList<TableResponse>> GetAllAsync()
        {
            return await _dbContext.Tables
            .AsNoTracking()
            .OrderBy(table => table.RestaurantId)
            .ThenBy(table => table.Number)
            .Select(table =>
                new TableResponse(
                    table.Id,
                    table.RestaurantId,
                    table.Number,
                    table.Capacity,
                    table.IsActive
                )
            ).ToListAsync();
        }

        public async Task<TableResponse?> GetByIdAsync(int id)
        {
            return await _dbContext.Tables
                .AsNoTracking()
                .Where(table => table.Id == id)
                .Select(table =>
                new TableResponse(
                    table.Id,
                    table.RestaurantId,
                    table.Number,
                    table.Capacity,
                    table.IsActive
                )
            ).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<TableResponse>> GetByRestaurantIdAsync(int restaurantId)
        {
            return await _dbContext.Tables
                .AsNoTracking()
                .Where(table => table.RestaurantId == restaurantId)
                .Select(table =>
                new TableResponse(
                    table.Id,
                    table.RestaurantId,
                    table.Number,
                    table.Capacity,
                    table.IsActive
                )
            ).ToListAsync();
        }

        public async Task<IReadOnlyList<TableResponse>> GetActiveAsync()
        {
            return await _dbContext.Tables
                .Where(table => table.IsActive)
                .OrderBy(table => table.RestaurantId)
                .ThenBy(table => table.Number)
                .Select(table =>
                    new TableResponse(
                        table.Id,
                        table.RestaurantId,
                        table.Number,
                        table.Capacity,
                        table.IsActive
                    )
                ).ToListAsync();
        }
        #endregion

        public async Task<TableOperationResult> CreateAsync(CreateTableRequest request)
        {
            var restaurantExists = await _dbContext.Restaurants
                .AnyAsync(restaurant => restaurant.Id == request.RestaurantId);

            if (!restaurantExists)
                return new TableOperationResult(
                    TableOperationStatus.RestaurantNotFound);

            var numberExists = await _dbContext.Tables
                .AnyAsync(table =>
                    table.RestaurantId == request.RestaurantId
                    && table.Number == request.Number
                );

            if (numberExists)
                return new TableOperationResult(
                    TableOperationStatus.DuplicateNumber);

            var table = new RestaurantTable
            {
                RestaurantId = request.RestaurantId,
                Number = request.Number,
                Capacity = request.Capacity,
                IsActive = request.IsActive
            };

            await _dbContext.Tables.AddAsync(table);
            await _dbContext.SaveChangesAsync();

            return new TableOperationResult(
                TableOperationStatus.Success,
                ToResponse(table)
            );
        }

        public async Task<TableOperationResult> UpdateAsync(int id, UpdateTableRequest request)
        {
            var table = await _dbContext.Tables.FindAsync(id);

            if (table is null)
                return new TableOperationResult(
                    TableOperationStatus.TableNotFound
                );

            var restaurantExists = await _dbContext.Restaurants
                    .AnyAsync(restaurant => restaurant.Id == request.RestaurantId);

            if (!restaurantExists)
                return new TableOperationResult(
                    TableOperationStatus.RestaurantNotFound
                );

            var numberExists =
                await _dbContext.Tables
                    .AnyAsync(otherTable =>
                        otherTable.RestaurantId == request.RestaurantId
                        && otherTable.Number == request.Number
                        && otherTable.Id != id
                    );

            if (numberExists)
            {
                return new TableOperationResult(
                    TableOperationStatus
                        .DuplicateNumber
                );
            }

            table.RestaurantId = request.RestaurantId;
            table.Number = request.Number;
            table.Capacity = request.Capacity;
            table.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync();

            return new TableOperationResult(
                TableOperationStatus.Success,
                ToResponse(table)
            );
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var table = await _dbContext.Tables.FindAsync(id);

            if (table is null)
                return false;

            _dbContext.Tables.Remove(table);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}