using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentCatalogAudit
{
    private static readonly HashSet<string> CatalogGraphTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "VacXin_Benh", "VacXin_CongNghe", "VaccineRegimen", "GoiVacXin_VacXin"
    };

    public sealed record ReferenceCount(string Table, string Column, long Count, string Kind);
    public sealed record Candidate(string EntityType, int Id, string Code, string Name, string[] Reasons,
        ReferenceCount[] References)
    {
        public long CatalogGraphReferences => References.Where(x => x.Kind == "CatalogGraph").Sum(x => x.Count);
        public long BusinessDataReferences => References.Where(x => x.Kind == "BusinessData").Sum(x => x.Count);
        public bool SafeToDeleteAfterGraphCleanup => BusinessDataReferences == 0;
    }

    public sealed record Report(DateTimeOffset GeneratedAt, Candidate[] Candidates)
    {
        public int CandidateCount => Candidates.Length;
        public int BusinessReferencedCount => Candidates.Count(x => x.BusinessDataReferences > 0);
        public int GraphOnlyCount => Candidates.Count(x => x.BusinessDataReferences == 0);
    }

    private sealed record RawCandidate(string EntityType, string Table, string KeyColumn, int Id, string Code, string Name, string[] Reasons);
    private sealed record ForeignKeyRef(string Schema, string Table, string Column);

    public static async Task<Report> RunAsync(TiemChungDbContext db, IHostEnvironment environment, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Catalog audit is Development only.");

        var raw = new List<RawCandidate>();
        raw.AddRange((await db.Vaccines.AsNoTracking().Select(x => new { x.MaVacXin, x.Code, x.TenVacXin, x.NhaSanXuat, x.XuatXu }).ToArrayAsync(ct))
            .Select(x => CandidateOrNull("Vaccine", "VacXin", "MaVacXin", x.MaVacXin, x.Code, x.TenVacXin, x.NhaSanXuat, x.XuatXu))
            .Where(x => x is not null).Cast<RawCandidate>());
        raw.AddRange((await db.Set<TiemChung.Domain.Entities.GoiVacXin>().AsNoTracking().Select(x => new { x.MaGoi, x.Code, x.TenGoi }).ToArrayAsync(ct))
            .Select(x => CandidateOrNull("Package", "GoiVacXin", "MaGoi", x.MaGoi, x.Code, x.TenGoi))
            .Where(x => x is not null).Cast<RawCandidate>());
        raw.AddRange((await db.Set<TiemChung.Domain.Entities.Benh>().AsNoTracking().Select(x => new { x.MaBenh, x.Code, x.TenBenh }).ToArrayAsync(ct))
            .Select(x => CandidateOrNull("Disease", "Benh", "MaBenh", x.MaBenh, x.Code, x.TenBenh))
            .Where(x => x is not null).Cast<RawCandidate>());
        raw.AddRange((await db.Set<TiemChung.Domain.Entities.CongNgheVacXin>().AsNoTracking().Select(x => new { x.MaCongNghe, x.Code, x.TenCongNghe }).ToArrayAsync(ct))
            .Select(x => CandidateOrNull("Technology", "CongNgheVacXin", "MaCongNghe", x.MaCongNghe, x.Code, x.TenCongNghe))
            .Where(x => x is not null).Cast<RawCandidate>());

        var fkCache = new Dictionary<string, ForeignKeyRef[]>(StringComparer.OrdinalIgnoreCase);
        var results = new List<Candidate>();
        foreach (var item in raw.OrderBy(x => x.EntityType).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            if (!fkCache.TryGetValue(item.Table, out var refs))
            {
                refs = await ReadInboundForeignKeysAsync(db, item.Table, ct);
                fkCache[item.Table] = refs;
            }
            var counts = new List<ReferenceCount>();
            foreach (var fk in refs)
            {
                var count = await CountReferencesAsync(db, fk, item.Id, ct);
                if (count == 0) continue;
                counts.Add(new(fk.Table, fk.Column, count,
                    CatalogGraphTables.Contains(fk.Table) ? "CatalogGraph" : "BusinessData"));
            }
            results.Add(new(item.EntityType, item.Id, item.Code, item.Name, item.Reasons,
                counts.OrderBy(x => x.Kind).ThenBy(x => x.Table).ThenBy(x => x.Column).ToArray()));
        }
        return new(DateTimeOffset.UtcNow, results.ToArray());
    }

    public static string ToJson(Report report) => JsonSerializer.Serialize(report,
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

    private static RawCandidate? CandidateOrNull(string type, string table, string keyColumn, int id, string? code, string? name,
        params string?[] other)
    {
        code ??= "";
        name ??= "";
        var reasons = new List<string>();
        if (ContainsMarker(code)) reasons.Add("code-marker");
        if (ContainsMarker(name)) reasons.Add("name-marker");
        if (LooksLikeLegacyPlaceholder(name)) reasons.Add("legacy-placeholder-name");
        if (other.Any(ContainsMarker)) reasons.Add("metadata-marker");
        return reasons.Count == 0 ? null : new(type, table, keyColumn, id, code, name, reasons.Distinct().ToArray());
    }

    private static bool ContainsMarker(string? value) =>
        !string.IsNullOrWhiteSpace(value) && (value.Contains("[DEMO]", StringComparison.OrdinalIgnoreCase)
            || value.Contains("DEMO", StringComparison.OrdinalIgnoreCase)
            || value.Contains("SYNTHETIC", StringComparison.OrdinalIgnoreCase)
            || value.Contains("MÔ PHỎNG", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeLegacyPlaceholder(string value)
    {
        var normalized = value.Trim();
        return normalized.Equals("Vắc xin A", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Vắc xin B", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Gói A", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Gói B", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Gói A + B", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ForeignKeyRef[]> ReadInboundForeignKeysAsync(TiemChungDbContext db, string referencedTable, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT OBJECT_SCHEMA_NAME(fkc.parent_object_id) AS SchemaName,
                       OBJECT_NAME(fkc.parent_object_id) AS TableName,
                       COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName
                FROM sys.foreign_key_columns fkc
                WHERE fkc.referenced_object_id = OBJECT_ID(N'dbo.' + @table)
                ORDER BY SchemaName, TableName, ColumnName;
                """;
            var p = cmd.CreateParameter(); p.ParameterName = "@table"; p.Value = referencedTable; cmd.Parameters.Add(p);
            var result = new List<ForeignKeyRef>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return result.ToArray();
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private static async Task<long> CountReferencesAsync(TiemChungDbContext db, ForeignKeyRef fk, int id, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        try
        {
            static string Q(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT_BIG(1) FROM {Q(fk.Schema)}.{Q(fk.Table)} WHERE {Q(fk.Column)}=@id;";
            var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; cmd.Parameters.Add(p);
            return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }
        finally { if (close) await connection.CloseAsync(); }
    }
}
