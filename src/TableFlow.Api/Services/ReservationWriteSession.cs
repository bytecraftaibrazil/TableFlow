using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TableFlow.Api.Data;
using TableFlow.Api.Entities;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Services;

// Keeps SQL-specific coordination separate from reservation decisions.
public sealed class ReservationWriteSession(TableFlowDbContext db) : IReservationWriteSession
{
    public Task<IDbContextTransaction> BeginAsync() => db.Database.BeginTransactionAsync();

    public Task LockReservationAsync(int reservationId)
        => ReservationLock.AcquireAsync(db, $"reservation:{reservationId}");

    public Task LockTablesAsync(params int[] tableIds)
        => ReservationLock.TablesAsync(db, tableIds);

    public Task ReloadAsync(Reservation reservation) => db.Entry(reservation).ReloadAsync();
}
