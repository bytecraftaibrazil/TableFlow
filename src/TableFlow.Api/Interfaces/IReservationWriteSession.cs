using Microsoft.EntityFrameworkCore.Storage;
using TableFlow.Api.Entities;

namespace TableFlow.Api.Interfaces;

public interface IReservationWriteSession
{
    Task<IDbContextTransaction> BeginAsync();
    Task LockReservationAsync(int reservationId);
    Task LockTablesAsync(params int[] tableIds);
    Task ReloadAsync(Reservation reservation);
}
