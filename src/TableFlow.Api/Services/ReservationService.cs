using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Models;

namespace TableFlow.Api.Services
{
    public class ReservationService : IReservationService
    {
        private readonly TableFlowDbContext _dbContext;

        public ReservationService(
            TableFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static ReservationResponse ToResponse(
           Reservation reservation)
        {
            return new ReservationResponse(
                reservation.Id,
                reservation.RestaurantId,
                reservation.TableId,
                reservation.CustomerName,
                reservation.ReservationDate,
                reservation.PartySize,
                reservation.Status
            );
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetAllAsync()
        {
            var reservations = await _dbContext.Reservations
                .AsNoTracking()
                .OrderBy(reservation => reservation.ReservationDate)
                .ThenBy(reservation => reservation.Id).ToListAsync();

            return reservations.Select(ToResponse).ToList();


        }

        public async Task<ReservationResponse?> GetByIdAsync(int id)
        {
            var reservation = await _dbContext.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(reservation => reservation.Id == id);

            return reservation is null ? null : ToResponse(reservation);
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetByRestaurantIdAsync(int restaurantId)
        {
            var reservations = await _dbContext.Reservations
                    .AsNoTracking()
                    .Where(reservation => reservation.RestaurantId == restaurantId)
                    .OrderBy(reservation => reservation.ReservationDate)
                    .ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetByTableIdAsync(int tableId)
        {
            var reservations = await _dbContext.Reservations
                    .AsNoTracking()
                    .Where(reservation => reservation.TableId == tableId)
                    .OrderBy(reservation => reservation.ReservationDate)
                    .ToListAsync();

            return reservations
                .Select(ToResponse)
                .ToList();
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetByStatusAsync(string status)
        {
            var normalizedStatus = status.Trim();

            var reservations = await _dbContext.Reservations
                    .AsNoTracking()
                    .Where(reservation => reservation.Status == normalizedStatus)
                    .OrderBy(reservation => reservation.ReservationDate)
                    .ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetFutureReservationsAsync()
        {
            var reservations = await _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation => reservation.ReservationDate > DateTime.Now)
                .OrderBy(reservation => reservation.ReservationDate)
                .ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }


        public async Task<ReservationOperationResult> CreateAsync(CreateReservationRequest request)
        {
            var restaurantExists = await _dbContext.Restaurants
                .AnyAsync(restaurant => restaurant.Id == request.RestaurantId);

            if (!restaurantExists)
            {
                return new ReservationOperationResult(ReservationOperationStatus.RestaurantNotFound);
            }

            var tableRelation = await _dbContext.Tables
                    .AsNoTracking()
                    .Where(table => table.Id == request.TableId)
                    .Select(table => new
                    {
                        table.Id,
                        table.RestaurantId
                    }).FirstOrDefaultAsync();

            if (tableRelation is null)
            {
                return new ReservationOperationResult(ReservationOperationStatus.TableNotFound);
            }

            if (tableRelation.RestaurantId != request.RestaurantId)
            {
                return new ReservationOperationResult(ReservationOperationStatus.TableDoesNotBelongToRestaurant);
            }

            var reservation = new Reservation
            {
                RestaurantId = request.RestaurantId,
                TableId = request.TableId,
                CustomerName = request.CustomerName.Trim(),
                ReservationDate = request.ReservationDate,
                PartySize = request.PartySize,
                Status = "Pending"
            };

            await _dbContext.Reservations.AddAsync(reservation);
            await _dbContext.SaveChangesAsync();

            return new ReservationOperationResult(
                ReservationOperationStatus.Success,
                ToResponse(reservation)
            );
        }

        public async Task<ReservationResponse?> UpdateAsync(int id, UpdateReservationRequest request)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return null;

            reservation.RestaurantId = request.RestaurantId;

            reservation.TableId = request.TableId;

            reservation.CustomerName = request.CustomerName.Trim();

            reservation.ReservationDate = request.ReservationDate;

            reservation.PartySize = request.PartySize;

            await _dbContext.SaveChangesAsync();

            return ToResponse(reservation);
        }


        public async Task<ReservationResponse?> CancelAsync(int id)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return null;

            reservation.Status = "Cancelled";

            await _dbContext.SaveChangesAsync();

            return ToResponse(reservation);
        }

        public async Task<ReservationResponse?> ConfirmAsync(int id)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return null;

            reservation.Status = "Confirmed";

            await _dbContext.SaveChangesAsync();

            return ToResponse(reservation);
        }
    }
}