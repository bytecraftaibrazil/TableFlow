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
        private readonly IReservationWriteSession _writeSession;

        public TableService(TableFlowDbContext dbContext, IReservationWriteSession writeSession)
        {
            _dbContext = dbContext;
            _writeSession = writeSession;
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
                .AsNoTracking()
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
            // Shares the reservation-write lock so a booking cannot appear between
            // checking reservations and transferring or reducing the table's capacity.
            await using var transaction = await _writeSession.BeginAsync();
            await _writeSession.LockTablesAsync(id);

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

            if (table.RestaurantId != request.RestaurantId
                && await _dbContext.Reservations.AnyAsync(reservation => reservation.TableId == id))
                return new TableOperationResult(TableOperationStatus.TableReassignmentConflict);

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

            if (request.Capacity < table.Capacity)
            {
                var utcNow = DateTime.UtcNow;
                var capacityConflict = await _dbContext.Reservations.AnyAsync(reservation =>
                    reservation.TableId == id
                    && (reservation.Status == "Pending" || reservation.Status == "Confirmed")
                    && reservation.PartySize > request.Capacity
                    && reservation.ReservationDate.AddMinutes(reservation.DurationMinutes) > utcNow);

                if (capacityConflict)
                    return new TableOperationResult(TableOperationStatus.TableCapacityConflict);
            }

            table.RestaurantId = request.RestaurantId;
            table.Number = request.Number;
            table.Capacity = request.Capacity;
            table.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

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

            if (await _dbContext.Reservations.AnyAsync(r => r.TableId == id))
                throw new ResourceConflictException("Table has related records and cannot be deleted.");

            _dbContext.Tables.Remove(table);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 } sql
                && (sql.Message.Contains("FK_Reservations_Tables_TableId", StringComparison.Ordinal)
                    || sql.Message.Contains("FK_Reservations_Restaurants_RestaurantId", StringComparison.Ordinal)
                    || sql.Message.Contains("FK_Tables_Restaurants_RestaurantId", StringComparison.Ordinal)))
            {
                throw new ResourceConflictException("Resource has related records and cannot be deleted.");
            }

            return true;
        }
    }
}
