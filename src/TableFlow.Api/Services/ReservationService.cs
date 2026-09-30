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
        private readonly IReservationEngine _reservationEngine;

        public ReservationService(
            TableFlowDbContext dbContext,
            IReservationEngine reservationEngine)
        {
            _dbContext = dbContext;
            _reservationEngine = reservationEngine;
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

        private static ReservationOperationStatus MapValidationStatus(
            ReservationValidationStatus status)
        {
            return status switch
            {
                ReservationValidationStatus.RestaurantNotFound =>
                    ReservationOperationStatus.RestaurantNotFound,

                ReservationValidationStatus.RestaurantInactive =>
                    ReservationOperationStatus.RestaurantInactive,

                ReservationValidationStatus.TableNotFound =>
                    ReservationOperationStatus.TableNotFound,

                ReservationValidationStatus.TableDoesNotBelongToRestaurant =>
                    ReservationOperationStatus.TableDoesNotBelongToRestaurant,

                ReservationValidationStatus.TableInactive =>
                    ReservationOperationStatus.TableInactive,
                
                ReservationValidationStatus.PartySizeExceedsTableCapacity =>
                    ReservationOperationStatus.PartySizeExceedsTableCapacity,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unexpected reservation validation status."
                )
            };
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

        public async Task<IReadOnlyList<ReservationResponse>> GetUpcomingConfirmedAsync()
        {
            var query = _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status == "Confirmed"
                )
                .Where(reservation =>
                    reservation.ReservationDate > DateTime.Now
                )
                .OrderBy(reservation =>
                    reservation.ReservationDate
                );

            var reservations = await query.ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }

        public async Task<IReadOnlyList<ReservationResponse>> GetUpcomingPendingAsync()
        {
            var query = _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status == "Pending"
                )
                .Where(reservation =>
                    reservation.ReservationDate > DateTime.Now
                )
                .OrderBy(reservation =>
                    reservation.ReservationDate
                );

            var reservations = await query.ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }

        public async Task<PagedResult<ReservationResponse>> SearchAsync(
            ReservationFilterRequest request)
        {
            var query = _dbContext.Reservations
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();

                query = query.Where(reservation => reservation.Status == status);
            }

            if (request.RestaurantId.HasValue)
            {
                query = query.Where(reservation => reservation.RestaurantId == request.RestaurantId.Value);
            }

            if (request.TableId.HasValue)
            {
                query = query.Where(reservation => reservation.TableId == request.TableId.Value);
            }

            if (request.MinimumPartySize.HasValue)
            {
                query = query.Where(reservation => reservation.PartySize >= request.MinimumPartySize.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(reservation => reservation.ReservationDate >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(reservation => reservation.ReservationDate <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync();

            query = request.Descending
                ? query.OrderByDescending(reservation => reservation.ReservationDate)
                    .ThenByDescending(reservation => reservation.Id)
                : query.OrderBy(reservation => reservation.ReservationDate)
                    .ThenBy(reservation => reservation.Id);

            var skip = (request.PageNumber - 1) * request.PageSize;

            var items = await query
                .Skip(skip)
                .Take(request.PageSize)
                .Select(reservation =>
                    new ReservationResponse(
                        reservation.Id,
                        reservation.RestaurantId,
                        reservation.TableId,
                        reservation.CustomerName,
                        reservation.ReservationDate,
                        reservation.PartySize,
                        reservation.Status
                    )
                )
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            var hasNextPage = request.PageNumber < totalPages;

            return new PagedResult<ReservationResponse>(
                items,
                request.PageNumber,
                request.PageSize,
                totalCount,
                totalPages,
                hasNextPage
            );
        }

        public async Task<ReservationOperationResult> CreateAsync(CreateReservationRequest request)
        {
            var candidate = new ReservationCandidate(
                request.RestaurantId,
                request.TableId,
                request.PartySize,
                request.ReservationDate
            );

            var validationResult = await _reservationEngine.ValidateAsync(candidate);

            if (!validationResult.IsSuccess)
            {
                return new ReservationOperationResult(
                    MapValidationStatus(
                        validationResult.Status
                    )
                );
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

        public async Task<ReservationOperationResult> UpdateAsync(int id, UpdateReservationRequest request)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return new ReservationOperationResult(ReservationOperationStatus.ReservationNotFound);

            if (reservation.Status == "Cancelled")
                return new ReservationOperationResult(
                    ReservationOperationStatus.CancelledReservationCannotBeUpdated);
            var candidate = new ReservationCandidate(
                request.RestaurantId,
                request.TableId,
                request.PartySize,
                request.ReservationDate
            );

            var validationResult =
                await _reservationEngine.ValidateAsync(
                    candidate
                );

            if (!validationResult.IsSuccess)
            {
                return new ReservationOperationResult(
                    MapValidationStatus(
                        validationResult.Status
                    )
                );
            }

            reservation.RestaurantId = request.RestaurantId;

            reservation.TableId = request.TableId;

            reservation.CustomerName = request.CustomerName.Trim();

            reservation.ReservationDate = request.ReservationDate;

            reservation.PartySize = request.PartySize;

            await _dbContext.SaveChangesAsync();

            return new ReservationOperationResult(
                    ReservationOperationStatus.Success,
                    ToResponse(reservation)
                );
        }


        public async Task<ReservationOperationResult> CancelAsync(int id)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return new ReservationOperationResult(ReservationOperationStatus.ReservationNotFound);

            if (reservation.Status == "Cancelled")
                return new ReservationOperationResult(ReservationOperationStatus.Success, ToResponse(reservation));

            reservation.Status = "Cancelled";

            await _dbContext.SaveChangesAsync();

            return new ReservationOperationResult(
                ReservationOperationStatus.Success,
                ToResponse(reservation)
            );
        }

        public async Task<ReservationOperationResult> ConfirmAsync(int id)
        {
            var reservation = await _dbContext.Reservations.FindAsync(id);

            if (reservation is null)
                return new ReservationOperationResult(ReservationOperationStatus.ReservationNotFound);

            if (reservation.Status == "Cancelled")
                return new ReservationOperationResult(ReservationOperationStatus.InvalidStatusTransition);

            if (reservation.Status == "Confirmed")
            {
                return new ReservationOperationResult(
                    ReservationOperationStatus.Success,
                    ToResponse(reservation)
                );
            }

            reservation.Status = "Confirmed";

            await _dbContext.SaveChangesAsync();

            return new ReservationOperationResult(
                ReservationOperationStatus.Success,
                ToResponse(reservation)
            );
        }
    }
}
