using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

// Session ownership keeps SMTP outside SQL transactions while ordering it against appointment mutations.
internal sealed class AppointmentDeliveryLock(TiemChungDbContext db, string resource) : IAsyncDisposable
{
    internal static async Task<AppointmentDeliveryLock> AcquireAsync(TiemChungDbContext db, int id, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        var resource = $"KLCN.Appointment.Delivery.{id}";
        try {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=30000; SELECT @r;";
            var parameter = command.CreateParameter(); parameter.ParameterName = "@resource"; parameter.Value = resource;
            command.Parameters.Add(parameter);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) < 0)
                throw new IdentityFailure(409, "appointmentBusy");
            return new(db, resource);
        } catch { await db.Database.CloseConnectionAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        try {
            if (db.Database.GetDbConnection().State == ConnectionState.Open) {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "EXEC sys.sp_releaseapplock @Resource=@resource, @LockOwner='Session';";
                var parameter = command.CreateParameter(); parameter.ParameterName = "@resource"; parameter.Value = resource;
                command.Parameters.Add(parameter);
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
        } finally { await db.Database.CloseConnectionAsync(); }
    }
}
