using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Tests;

// Test assembly only: never referenced by API/operator publish.
internal static class IdentityFixture
{
    public static async Task MarkSyntheticAsync(TiemChungDbContext db)
    {
        if(!db.Database.GetDbConnection().Database.StartsWith("KLCN_M1_Test_",StringComparison.Ordinal))
            throw new InvalidOperationException("Synthetic DB name required.");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE FixtureOnly (Id int NOT NULL PRIMARY KEY CHECK (Id=1)); INSERT INTO FixtureOnly VALUES(1);");
    }

    public static async Task LinkAsync(TiemChungDbContext db,string environment,int accountId,int? customerId,int? recipientId)
    {
        if(environment is not ("Development" or "Test") || !db.Database.GetDbConnection().Database.StartsWith("KLCN_M1_Test_",StringComparison.Ordinal))
            throw new InvalidOperationException("Fixture refused.");
        var marker=await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM FixtureOnly WHERE Id=1").SingleAsync();
        if(marker!=1 || (customerId is null)==(recipientId is null)) throw new InvalidOperationException("Fixture marker/target required.");
        var account=await db.Set<TaiKhoan>().SingleAsync(x=>x.MaTaiKhoan==accountId);
        if(account.AccountType!="Customer") throw new InvalidOperationException("Customer account required.");
        db.Add(new AccountProfileLink { AccountId=accountId,CustomerId=customerId,RecipientId=recipientId,State="Active",CreatedAt=DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }
}
