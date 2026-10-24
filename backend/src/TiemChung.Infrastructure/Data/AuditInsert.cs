using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

public sealed partial class TiemChungDbContext
{
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var audits = ChangeTracker.Entries<SecurityAudit>().Where(e => e.State == EntityState.Added).ToArray();
        if (audits.Length == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        // EF's trigger-compatible identity read selects the audit table. Runtime has INSERT only.
        // SCOPE_IDENTITY returns this INSERT's key without reading any audit rows or elevating SQL context.
        await using var ownedTransaction = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        foreach (var entry in audits) entry.State = EntityState.Detached;
        try {
            var count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            foreach (var entry in audits) {
                var a = entry.Entity;
                await using var command = Database.GetDbConnection().CreateCommand();
                command.Transaction = Database.CurrentTransaction!.GetDbTransaction();
                command.CommandText = """
                    INSERT dbo.SecurityAudit(OccurredAt,ActorAccountId,SubjectAccountId,SessionId,Action,Outcome,
                        ResourceType,ResourceId,TraceId,ChangedFields,OperationId,OperatorIdentity)
                    VALUES(@at,@actor,@subject,@session,@action,@outcome,@type,@resource,@trace,@fields,@operation,@operator);
                    SELECT CONVERT(bigint,SCOPE_IDENTITY());
                    """;
                (string Name, object? Value)[] values = [("@at",a.OccurredAt),("@actor",a.ActorAccountId),
                    ("@subject",a.SubjectAccountId),("@session",a.SessionId),("@action",a.Action),("@outcome",a.Outcome),
                    ("@type",a.ResourceType),("@resource",a.ResourceId),("@trace",a.TraceId),("@fields",a.ChangedFields),
                    ("@operation",a.OperationId),("@operator",a.OperatorIdentity)];
                foreach (var value in values) command.Parameters.Add(new SqlParameter(value.Name,value.Value ?? DBNull.Value));
                try { a.Id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)); }
                catch (SqlException error) { throw new DbUpdateException("Audit insert failed; transaction must roll back.",error); }
                // Append-only rows stay detached after acceptance; do not retain EF's old temporary-key snapshot.
                // No runtime path updates an audit entity after inserting it.
                if (!acceptAllChangesOnSuccess) {
                    entry.Property(x => x.Id).CurrentValue = a.Id;
                    entry.Property(x => x.Id).IsTemporary = false;
                    entry.State = EntityState.Added;
                }
                count++;
            }
            if (ownedTransaction is not null) await ownedTransaction.CommitAsync(cancellationToken);
            return count;
        } catch {
            foreach (var entry in audits) entry.State = EntityState.Added;
            throw;
        }
    }
}
