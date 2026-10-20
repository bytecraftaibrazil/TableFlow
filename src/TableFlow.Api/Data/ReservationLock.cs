using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TableFlow.Api.Data;

// SQL Server locks are shared by every API instance and released on commit/rollback.
internal static class ReservationLock
{
    public static async Task AcquireAsync(TableFlowDbContext db, string resource)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; SELECT @result;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = "TableFlow:" + resource;
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result is -1 or -2 or -3)
            throw new ResourceConflictException("Reservation is being changed. Retry the operation.");
        if (result < 0)
            throw new InvalidOperationException($"SQL Server could not acquire the application lock (code {result}).");
    }

    public static async Task TablesAsync(TableFlowDbContext db, params int[] tableIds)
    {
        foreach (var id in tableIds.Distinct().Order())
            await AcquireAsync(db, $"table:{id}");
    }
}
