using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class CatalogEndpoints
{
    internal static readonly string[] AdministrationRoutes = ["Intramuscular", "Subcutaneous", "Intradermal", "Oral", "Nasal", "Other"];

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog").WithTags("Catalog");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 547 }) {
                throw new IdentityFailure(409, "catalogReferenced");
            }
            catch (DbUpdateException e) when (e.InnerException is SqlException sql && sql.Number is 2601 or 2627) {
                throw new IdentityFailure(409, "catalogDuplicate");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        MapTechnologies(group);
        MapLookup<Benh>(group, "diseases", "BENH", "MaBenh", "TenBenh", 150);

        group.MapGet("/suggestions", async (TiemChungDbContext db, CancellationToken ct) => Results.Ok(new CatalogSuggestionsDto(
            await db.Vaccines.AsNoTracking().Select(v => v.NhaSanXuat.Trim()).Where(x => x != "").Distinct().Order().ToArrayAsync(ct),
            await db.Vaccines.AsNoTracking().Select(v => v.XuatXu.Trim()).Where(x => x != "").Distinct().Order().ToArrayAsync(ct), AdministrationRoutes)));

        group.MapGet("/vaccines", async (string? search, int? diseaseId, int? technologyId, decimal? minPrice, decimal? maxPrice,
            string? status, TiemChungDbContext db, CancellationToken ct) => {
            ValidateFilters(search, diseaseId, technologyId, minPrice, maxPrice, status);
            search = search?.Trim();
            var query = db.Vaccines.AsNoTracking().Where(v =>
                (string.IsNullOrEmpty(search) || v.Code.Contains(search) || v.TenVacXin.Contains(search) || v.NhaSanXuat.Contains(search) ||
                    db.Set<VacXin_Benh>().Any(l => l.MaVacXin == v.MaVacXin && db.Set<Benh>().Any(d => d.MaBenh == l.MaBenh && d.TenBenh.Contains(search)))) &&
                (!diseaseId.HasValue || db.Set<VacXin_Benh>().Any(l => l.MaVacXin == v.MaVacXin && l.MaBenh == diseaseId)) &&
                (!technologyId.HasValue || db.Set<VacXin_CongNghe>().Any(l => l.MaVacXin == v.MaVacXin && l.MaCongNghe == technologyId)) &&
                (!minPrice.HasValue || v.Gia >= minPrice) && (!maxPrice.HasValue || v.Gia <= maxPrice) && (status == null || v.TrangThai == status));
            return Results.Ok(new VaccineListResponse("database", await Vaccines(db, await query.OrderBy(v => v.MaVacXin).ToArrayAsync(ct), ct)));
        });
        group.MapGet("/vaccines/{id:int}", async (int id, TiemChungDbContext db, CancellationToken ct) =>
            Results.Ok(await VaccineDetail(db, id, ct)));
        group.MapPost("/vaccines", async (VaccineRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            await ValidateVaccine(req, null, db, ct);
            var row = new VacXin { Code = req.Code!, TrangThai = req.Status ?? "Active" }; SetVaccine(row, req); db.Add(row);
            await db.SaveChangesAsync(ct);
            await SetVaccineChildren(db, row.MaVacXin, req, ct);
            await Audit(db, context, actor, "created", "vaccines", row.MaVacXin, ct);
            var result = await VaccineDetail(db, row.MaVacXin, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/vaccines/{row.MaVacXin}", result);
        });
        group.MapPut("/vaccines/{id:int}", async (int id, VaccineRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            await LockPackageStatus(db, ct);
            var row = await db.Vaccines.FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            await ValidateVaccine(req, row, db, ct); SetVaccine(row, req);
            await SetVaccineChildren(db, id, req, ct);
            await Audit(db, context, actor, "updated", "vaccines", id, ct);
            var result = await VaccineDetail(db, id, ct); await tx.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapDelete("/vaccines/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            var row = await db.Vaccines.FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            db.RemoveRange(await db.Set<VacXin_CongNghe>().Where(x => x.MaVacXin == id).ToArrayAsync(ct));
            db.RemoveRange(await db.Set<VacXin_Benh>().Where(x => x.MaVacXin == id).ToArrayAsync(ct));
            db.RemoveRange(await db.Set<VaccineRegimen>().Where(x => x.VaccineId == id).ToArrayAsync(ct));
            db.Remove(row); await Audit(db, context, actor, "deleted", "vaccines", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
        group.MapGet("/packages", async (string? search, int? diseaseId, int? technologyId, decimal? minPrice, decimal? maxPrice,
            string? status, TiemChungDbContext db, CancellationToken ct) => {
            ValidateFilters(search, diseaseId, technologyId, minPrice, maxPrice, status); search = search?.Trim();
            var rows = await db.Set<GoiVacXin>().AsNoTracking().Where(p =>
                (string.IsNullOrEmpty(search) || p.Code.Contains(search) || p.TenGoi.Contains(search) || db.Set<GoiVacXin_VacXin>().Any(l => l.MaGoi == p.MaGoi &&
                    db.Vaccines.Any(v => v.MaVacXin == l.MaVacXin && (v.Code.Contains(search) || v.TenVacXin.Contains(search) || v.NhaSanXuat.Contains(search) ||
                        db.Set<VacXin_Benh>().Any(vd => vd.MaVacXin == v.MaVacXin && db.Set<Benh>().Any(d => d.MaBenh == vd.MaBenh && d.TenBenh.Contains(search))))))) &&
                (!minPrice.HasValue || p.GiaGoi >= minPrice) && (!maxPrice.HasValue || p.GiaGoi <= maxPrice) && (status == null || p.TrangThai == status) &&
                ((!diseaseId.HasValue && !technologyId.HasValue) || db.Set<GoiVacXin_VacXin>().Any(l => l.MaGoi == p.MaGoi &&
                    db.Vaccines.Any(v => v.MaVacXin == l.MaVacXin && (!technologyId.HasValue || db.Set<VacXin_CongNghe>().Any(vt => vt.MaVacXin == v.MaVacXin && vt.MaCongNghe == technologyId)) &&
                        (!diseaseId.HasValue || db.Set<VacXin_Benh>().Any(vd => vd.MaVacXin == v.MaVacXin && vd.MaBenh == diseaseId))))))
                .OrderBy(p => p.MaGoi).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items = await Packages(db, rows, ct) });
        });
        group.MapGet("/packages/{id:int}", async (int id, TiemChungDbContext db, CancellationToken ct) => Results.Ok(await PackageDetail(db, id, ct)));
        group.MapPost("/packages", async (PackageRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            await LockPackageStatus(db, ct); await ValidatePackage(req, null, db, ct);
            var row = new GoiVacXin { Code = req.Code!, TenGoi = req.Name.Trim(), GiaGoi = req.Price, TrangThai = req.Status ?? "Active",
                MoTa = req.Description?.Trim(), DoiTuongSuDung = req.TargetAudience?.Trim() };
            db.Add(row); await db.SaveChangesAsync(ct); SetItems(db, row.MaGoi, req.Items);
            await Audit(db, context, actor, "created", "packages", row.MaGoi, ct);
            var result = await PackageDetail(db, row.MaGoi, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/packages/{row.MaGoi}", result);
        });
        group.MapPut("/packages/{id:int}", async (int id, PackageRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            await LockPackageStatus(db, ct);
            // Registration takes the same package lock before reading components/prices.
            var row = await db.Set<GoiVacXin>().FromSqlInterpolated($"SELECT * FROM GoiVacXin WITH (UPDLOCK,HOLDLOCK) WHERE MaGoi={id}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            await ValidatePackage(req, row, db, ct);
            var current = await db.Set<GoiVacXin_VacXin>().Where(x => x.MaGoi == id).ToArrayAsync(ct);
            var changed = !current.OrderBy(x => x.MaVacXin).Select(x => (x.MaVacXin, x.SoLuong))
                .SequenceEqual(req.Items.OrderBy(x => x.VaccineId).Select(x => (x.VaccineId, x.Quantity)));
            if (changed && await db.RegistrationPackageItems.AnyAsync(x => x.MaGoi == id, ct)) throw new IdentityFailure(409, "catalogReferenced");
            row.TenGoi = req.Name.Trim(); row.GiaGoi = req.Price; row.TrangThai = req.Status ?? row.TrangThai;
            row.MoTa = req.Description?.Trim() ?? row.MoTa; row.DoiTuongSuDung = req.TargetAudience?.Trim() ?? row.DoiTuongSuDung;
            if (changed) { db.RemoveRange(current); await db.SaveChangesAsync(ct); SetItems(db, id, req.Items); }
            await Audit(db, context, actor, "updated", "packages", id, ct);
            var result = await PackageDetail(db, id, ct); await tx.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapDelete("/packages/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            var row = await db.Set<GoiVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            db.RemoveRange(await db.Set<GoiVacXin_VacXin>().Where(x => x.MaGoi == id).ToArrayAsync(ct));
            db.Remove(row); await Audit(db, context, actor, "deleted", "packages", id, ct); await tx.CommitAsync(ct); return Results.NoContent();
        });
        return app;
    }

    private static VaccineTechnologyDto TechnologyDto(CongNgheVacXin t) => new(t.MaCongNghe, t.TenCongNghe, t.MoTa, t.Code, t.TrangThai,
        t.DacDiemKyThuat, t.GhiChuChuyenMon, t.NguonThongTin, t.DuongDanNguon, t.NgayCapNhatThongTin);

    private static void MapTechnologies(RouteGroupBuilder group)
    {
        const string route = "/vaccine-technologies";
        group.MapGet(route, async (string? search, string? status, TiemChungDbContext db, CancellationToken ct) => {
            ValidateFilters(search, null, null, null, null, status); search = search?.Trim();
            var rows = await db.Set<CongNgheVacXin>().AsNoTracking().Where(t =>
                (string.IsNullOrEmpty(search) || t.Code.Contains(search) || t.TenCongNghe.Contains(search)) && (status == null || t.TrangThai == status))
                .OrderBy(t => t.MaCongNghe).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items = rows.Select(TechnologyDto) });
        });
        group.MapGet(route + "/{id:int}", async (int id, TiemChungDbContext db, CancellationToken ct) =>
            Results.Ok(TechnologyDto(await db.Set<CongNgheVacXin>().AsNoTracking().SingleOrDefaultAsync(t => t.MaCongNghe == id, ct) ?? throw new IdentityFailure(404, "resourceNotFound"))));
        async Task<IResult> Save(int? id, VaccineTechnologyRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct)
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = id.HasValue ? await db.Set<CongNgheVacXin>().FindAsync([id.Value], ct) ?? throw new IdentityFailure(404, "resourceNotFound") : new CongNgheVacXin();
            ValidateCode(req.Code, "CNVX", id.HasValue ? row.Code : null); ValidateText(req.Name, 120); ValidateText(req.Description, 500, false); ValidateStatus(req.Status);
            ValidateText(req.TechnicalCharacteristics, 2000, false); ValidateText(req.ProfessionalNotes, 2000, false);
            ValidateText(req.SourceName, 250, false); ValidateUrl(req.SourceUrl);
            if (!id.HasValue) { row.Code = req.Code!; db.Add(row); }
            row.TenCongNghe = req.Name.Trim(); row.MoTa = req.Description?.Trim() ?? ""; row.TrangThai = req.Status ?? row.TrangThai;
            row.DacDiemKyThuat = req.TechnicalCharacteristics?.Trim(); row.GhiChuChuyenMon = req.ProfessionalNotes?.Trim();
            row.NguonThongTin = req.SourceName?.Trim(); row.DuongDanNguon = req.SourceUrl?.Trim(); row.NgayCapNhatThongTin = req.InformationUpdatedAt;
            await db.SaveChangesAsync(ct);
            await Audit(db, context, actor, id.HasValue ? "updated" : "created", "vaccine-technologies", row.MaCongNghe, ct);
            await tx.CommitAsync(ct);
            return id.HasValue ? Results.Ok(TechnologyDto(row)) : Results.Created($"/api/v1/catalog/vaccine-technologies/{row.MaCongNghe}", TechnologyDto(row));
        }
        group.MapPost(route, (VaccineTechnologyRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => Save(null, req, context, auth, db, ct));
        group.MapPut(route + "/{id:int}", (int id, VaccineTechnologyRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => Save(id, req, context, auth, db, ct));
        group.MapDelete(route + "/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = await db.Set<CongNgheVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            // Restrict FK refuses a referenced technology; links must be removed explicitly on the vaccine.
            db.Remove(row); await Audit(db, context, actor, "deleted", "vaccine-technologies", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
    }

    private static void MapLookup<T>(RouteGroupBuilder group, string resource, string prefix, string idProperty, string nameProperty, int nameMax) where T : class, new()
    {
        group.MapGet("/" + resource, async (string? search, string? status, TiemChungDbContext db, CancellationToken ct) => {
            ValidateFilters(search, null, null, null, null, status); search = search?.Trim();
            var items = await db.Set<T>().AsNoTracking().Where(x =>
                (string.IsNullOrEmpty(search) || EF.Property<string>(x, nameProperty).Contains(search) || EF.Property<string>(x, "Code").Contains(search)) &&
                (status == null || EF.Property<string>(x, "TrangThai") == status)).OrderBy(x => EF.Property<int>(x, idProperty))
                .Select(x => new CatalogTypeDto(EF.Property<int>(x, idProperty), EF.Property<string>(x, nameProperty), EF.Property<string>(x, "MoTa"), EF.Property<string>(x, "Code"), EF.Property<string>(x, "TrangThai"))).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items });
        });
        async Task<IResult> Save(int? id, CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct)
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = id.HasValue ? await db.Set<T>().FindAsync([id.Value], ct) ?? throw new IdentityFailure(404, "resourceNotFound") : new T();
            ValidateText(req.Name, nameMax); ValidateText(req.Description, 500, false); ValidateStatus(req.Status);
            var entry = db.Entry(row); var code = entry.Property<string>("Code").CurrentValue;
            ValidateCode(req.Code, prefix, id.HasValue ? code : null);
            if (!id.HasValue) { entry.Property<string>("Code").CurrentValue = req.Code!; db.Add(row); }
            entry.Property<string>(nameProperty).CurrentValue = req.Name.Trim(); entry.Property<string>("MoTa").CurrentValue = req.Description?.Trim() ?? "";
            entry.Property<string>("TrangThai").CurrentValue = req.Status ?? (id.HasValue ? entry.Property<string>("TrangThai").CurrentValue : "Active");
            await db.SaveChangesAsync(ct); var savedId = entry.Property<int>(idProperty).CurrentValue;
            await Audit(db, context, actor, id.HasValue ? "updated" : "created", resource, savedId, ct);
            await tx.CommitAsync(ct);
            var result = new CatalogTypeDto(savedId, req.Name.Trim(), req.Description?.Trim() ?? "", entry.Property<string>("Code").CurrentValue, entry.Property<string>("TrangThai").CurrentValue);
            return id.HasValue ? Results.Ok(result) : Results.Created($"/api/v1/catalog/{resource}/{savedId}", result);
        }
        group.MapPost("/" + resource, (CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => Save(null, req, context, auth, db, ct));
        group.MapPut("/" + resource + "/{id:int}", (int id, CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => Save(id, req, context, auth, db, ct));
        group.MapDelete("/" + resource + "/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = await db.Set<T>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            db.Remove(row); await Audit(db, context, actor, "deleted", resource, id, ct); await tx.CommitAsync(ct); return Results.NoContent();
        });
    }

    private static async Task<VaccineDto> VaccineDetail(TiemChungDbContext db, int id, CancellationToken ct) =>
        (await Vaccines(db, [await db.Vaccines.AsNoTracking().SingleOrDefaultAsync(x => x.MaVacXin == id, ct) ?? throw new IdentityFailure(404, "resourceNotFound")], ct)).Single();

    private static async Task<VaccineDto[]> Vaccines(TiemChungDbContext db, VacXin[] rows, CancellationToken ct)
    {
        if (rows.Length == 0) return [];
        var ids = rows.Select(x => x.MaVacXin).ToArray();
        var technologies = await (from link in db.Set<VacXin_CongNghe>().AsNoTracking() join t in db.Set<CongNgheVacXin>().AsNoTracking() on link.MaCongNghe equals t.MaCongNghe
            where ids.Contains(link.MaVacXin) orderby t.MaCongNghe select new { link.MaVacXin, Technology = new CatalogReferenceDto(t.MaCongNghe, t.Code, t.TenCongNghe) }).ToArrayAsync(ct);
        var diseases = await (from link in db.Set<VacXin_Benh>().AsNoTracking() join d in db.Set<Benh>().AsNoTracking() on link.MaBenh equals d.MaBenh
            where ids.Contains(link.MaVacXin) orderby d.MaBenh select new { link.MaVacXin, Disease = new CatalogReferenceDto(d.MaBenh, d.Code, d.TenBenh) }).ToArrayAsync(ct);
        var regimens = await db.Set<VaccineRegimen>().AsNoTracking().Where(x => ids.Contains(x.VaccineId)).OrderBy(x => x.Id).ToArrayAsync(ct);
        var regimenIds = regimens.Select(x => x.Id).ToArray();
        var doses = await db.Set<VaccineRegimenDose>().AsNoTracking().Where(x => regimenIds.Contains(x.RegimenId)).OrderBy(x => x.DoseOrder).ToArrayAsync(ct);
        return rows.Select(v => new VaccineDto(v.MaVacXin, v.TenVacXin, v.NhaSanXuat, v.XuatXu, v.Gia) {
            Code = v.Code, Status = v.TrangThai,
            Technologies = technologies.Where(t => t.MaVacXin == v.MaVacXin).Select(t => t.Technology).ToArray(),
            Diseases = diseases.Where(d => d.MaVacXin == v.MaVacXin).Select(d => d.Disease).ToArray(), Description = v.MoTa,
            TargetAudience = v.DoiTuongSuDung, AdministrationRoute = v.DuongDung, AdministrationRouteNote = v.DuongDungKhac,
            Dosage = v.LieuLuong, Volume = v.TheTich, SourceName = v.NguonThongTin, SourceUrl = v.DuongDanNguon, InformationUpdatedAt = v.NgayCapNhatThongTin,
            Regimens = regimens.Where(r => r.VaccineId == v.MaVacXin).Select(r => new VaccineRegimenDto(r.Id, r.Name, r.TargetAudience, r.Summary, r.SourceName, r.SourceUrl,
                doses.Where(d => d.RegimenId == r.Id).Select(d => new VaccineRegimenDoseDto(d.Id, d.DoseOrder, d.TimingLabel, d.MinimumIntervalDays, d.RecommendedIntervalDays, d.Note)).ToArray())).ToArray()
        }).ToArray();
    }
    private static async Task<PackageDto> PackageDetail(TiemChungDbContext db, int id, CancellationToken ct) =>
        (await Packages(db, [await db.Set<GoiVacXin>().AsNoTracking().SingleOrDefaultAsync(x => x.MaGoi == id, ct) ?? throw new IdentityFailure(404, "resourceNotFound")], ct)).Single();
    private static async Task<PackageDto[]> Packages(TiemChungDbContext db, GoiVacXin[] rows, CancellationToken ct)
    {
        if (rows.Length == 0) return [];
        var ids = rows.Select(x => x.MaGoi).ToArray();
        var links = await (from l in db.Set<GoiVacXin_VacXin>().AsNoTracking() join v in db.Vaccines.AsNoTracking() on l.MaVacXin equals v.MaVacXin
            where ids.Contains(l.MaGoi) orderby l.MaVacXin select new { l.MaGoi, Item = new PackageItemDto(l.MaVacXin, l.SoLuong, v.TenVacXin, v.Code) }).ToArrayAsync(ct);
        var diseases = await (from l in db.Set<GoiVacXin_VacXin>().AsNoTracking() join vd in db.Set<VacXin_Benh>().AsNoTracking() on l.MaVacXin equals vd.MaVacXin
            join d in db.Set<Benh>().AsNoTracking() on vd.MaBenh equals d.MaBenh where ids.Contains(l.MaGoi)
            select new { l.MaGoi, d.MaBenh, d.Code, d.TenBenh }).Distinct().OrderBy(x => x.MaBenh).ToArrayAsync(ct);
        return rows.Select(p => new PackageDto(p.MaGoi, p.TenGoi, p.GiaGoi, links.Where(l => l.MaGoi == p.MaGoi).Select(l => l.Item).ToArray()) {
            Code = p.Code, Status = p.TrangThai, Description = p.MoTa, TargetAudience = p.DoiTuongSuDung,
            Diseases = diseases.Where(d => d.MaGoi == p.MaGoi).Select(d => new CatalogReferenceDto(d.MaBenh, d.Code, d.TenBenh)).ToArray()
        }).ToArray();
    }

    // Lock before reading either side: opposite package/vaccine lock orders can deadlock during status changes.
    private static Task LockPackageStatus(TiemChungDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("""
        DECLARE @result int;
        EXEC @result=sys.sp_getapplock @Resource=N'KLCN.Catalog.PackageStatus',
            @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
        IF @result<0 THROW 51001, 'Catalog status lock unavailable.', 1;
        """, ct);

    private static async Task<IdentityActor> RequireWrite(HttpContext context, IdentityAuthorization auth, bool price, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, true, ct);
        if (actor.IsCustomer || !actor.Permissions.Contains("QuanLyDanhMucChuyenMon") || price && !actor.Permissions.Contains("QuanLyDanhMucGia")) throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }
    private static void ValidateText(string? value, int max, bool required = true)
    {
        if (value?.Trim().Length > max || required && string.IsNullOrWhiteSpace(value)) throw new IdentityFailure(400, "validation");
    }
    private static void ValidateStatus(string? status) { if (status is not null and not "Active" and not "Inactive") throw new IdentityFailure(400, "validation"); }
    private static void ValidateCode(string? code, string prefix, string? existing)
    {
        if (code == null && existing != null) return;
        if (code == null || code.Length > 50 || !code.StartsWith(prefix + "-", StringComparison.Ordinal) || !Regex.IsMatch(code, "^[A-Z0-9]+(?:-[A-Z0-9]+)*\\z", RegexOptions.CultureInvariant) ||
            existing != null && code != existing) throw new IdentityFailure(400, "validation");
    }
    private static void ValidateUrl(string? url)
    {
        ValidateText(url, 1000, false);
        if (!string.IsNullOrWhiteSpace(url) && (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)))
            throw new IdentityFailure(400, "validation");
    }
    private static void ValidatePrice(decimal value) { if (value < 0 || value > 9999999999999999m || decimal.Truncate(value) != value) throw new IdentityFailure(400, "validation"); }
    private static void ValidateFilters(string? search, int? disease, int? technology, decimal? min, decimal? max, string? status)
    {
        if (search?.Length > 150 || disease is <= 0 || technology is <= 0 || min > max) throw new IdentityFailure(400, "validation");
        if (min.HasValue) ValidatePrice(min.Value); if (max.HasValue) ValidatePrice(max.Value); ValidateStatus(status);
    }
    private static async Task ValidateVaccine(VaccineRequest req, VacXin? row, TiemChungDbContext db, CancellationToken ct)
    {
        ValidateCode(req.Code, "VX", row?.Code); ValidateStatus(req.Status);
        ValidateText(req.Name, 150); ValidateText(req.Manufacturer, 150); ValidateText(req.Origin, 100); ValidatePrice(req.Price);
        if (row != null && req.Status == "Inactive" && await db.Set<GoiVacXin_VacXin>().AnyAsync(
            link => link.MaVacXin == row.MaVacXin && db.Set<GoiVacXin>().Any(p => p.MaGoi == link.MaGoi && p.TrangThai == "Active"), ct))
            throw new IdentityFailure(409, "catalogReferenced");
        var activating = row != null && row.TrangThai != "Active" && req.Status == "Active";
        ValidateText(req.Description, 2000, false); ValidateText(req.TargetAudience, 1000, row == null || req.TargetAudience != null);
        if (activating) ValidateText(req.TargetAudience ?? row!.DoiTuongSuDung, 1000);
        ValidateText(req.Dosage, 200, false); ValidateText(req.SourceName, 250, false); ValidateUrl(req.SourceUrl);
        if (req.Volume is < 0 or > 99999999.99m || req.Volume.HasValue && decimal.Round(req.Volume.Value, 2) != req.Volume.Value) throw new IdentityFailure(400, "validation");
        var route = req.AdministrationRoute ?? row?.DuongDung;
        var routeNote = req.AdministrationRouteNote ?? row?.DuongDungKhac;
        if (route != null || row == null || activating) {
            if (!AdministrationRoutes.Contains(route) || route == "Other" && string.IsNullOrWhiteSpace(routeNote) || route != "Other" && !string.IsNullOrWhiteSpace(req.AdministrationRouteNote))
                throw new IdentityFailure(400, "validation");
            ValidateText(routeNote, 500, false);
        }
        if (req.TechnologyIds != null) {
            if (req.TechnologyIds.Any(x => x <= 0) || req.TechnologyIds.Distinct().Count() != req.TechnologyIds.Length ||
                await db.Set<CongNgheVacXin>().CountAsync(x => req.TechnologyIds.Contains(x.MaCongNghe) &&
                    (x.TrangThai == "Active" || row != null && db.Set<VacXin_CongNghe>().Any(l => l.MaVacXin == row.MaVacXin && l.MaCongNghe == x.MaCongNghe)), ct) != req.TechnologyIds.Length)
                throw new IdentityFailure(400, "validation");
        }
        if (req.DiseaseIds != null) {
            if (req.DiseaseIds.Length == 0 || req.DiseaseIds.Any(x => x <= 0) || req.DiseaseIds.Distinct().Count() != req.DiseaseIds.Length ||
                await db.Set<Benh>().CountAsync(x => req.DiseaseIds.Contains(x.MaBenh), ct) != req.DiseaseIds.Length) throw new IdentityFailure(400, "validation");
            if ((req.Status ?? row?.TrangThai ?? "Active") == "Active" && !await db.Set<Benh>().AnyAsync(x => req.DiseaseIds.Contains(x.MaBenh) && x.TrangThai == "Active", ct))
                throw new IdentityFailure(400, "validation");
        } else if (row == null || activating &&
            !await db.Set<VacXin_Benh>().AnyAsync(l => l.MaVacXin == row.MaVacXin && db.Set<Benh>().Any(d => d.MaBenh == l.MaBenh && d.TrangThai == "Active"), ct))
            throw new IdentityFailure(400, "validation");
        if (req.Regimens != null) foreach (var regimen in req.Regimens) {
            if (regimen == null) throw new IdentityFailure(400, "validation");
            ValidateText(regimen.Name, 150); ValidateText(regimen.TargetAudience, 1000); ValidateText(regimen.Summary, 1500, false);
            ValidateText(regimen.SourceName, 250, false); ValidateUrl(regimen.SourceUrl);
            if (regimen.Doses == null || regimen.Doses.Length is < 1 or > 20 || regimen.Doses.Any(d => d == null || d.DoseOrder is < 1 or > 20 ||
                d.MinimumIntervalDays is < 0 || d.RecommendedIntervalDays is < 0 || d.RecommendedIntervalDays < d.MinimumIntervalDays) ||
                regimen.Doses.Select(d => d.DoseOrder).Distinct().Count() != regimen.Doses.Length) throw new IdentityFailure(400, "validation");
            foreach (var dose in regimen.Doses) { ValidateText(dose.TimingLabel, 250); ValidateText(dose.Note, 500, false); }
        }
    }
    private static void SetVaccine(VacXin row, VaccineRequest req)
    {
        row.TenVacXin = req.Name.Trim(); row.NhaSanXuat = req.Manufacturer.Trim(); row.XuatXu = req.Origin.Trim();
        row.Gia = req.Price; row.TrangThai = req.Status ?? row.TrangThai;
        row.MoTa = req.Description?.Trim() ?? row.MoTa; row.DoiTuongSuDung = req.TargetAudience?.Trim() ?? row.DoiTuongSuDung;
        row.DuongDung = req.AdministrationRoute ?? row.DuongDung;
        row.DuongDungKhac = row.DuongDung == "Other" ? req.AdministrationRouteNote?.Trim() ?? row.DuongDungKhac : null;
        row.LieuLuong = req.Dosage?.Trim() ?? row.LieuLuong; row.TheTich = req.Volume ?? row.TheTich;
        row.NguonThongTin = req.SourceName?.Trim() ?? row.NguonThongTin; row.DuongDanNguon = req.SourceUrl?.Trim() ?? row.DuongDanNguon;
        row.NgayCapNhatThongTin = req.InformationUpdatedAt ?? row.NgayCapNhatThongTin;
    }
    private static async Task SetVaccineChildren(TiemChungDbContext db, int id, VaccineRequest req, CancellationToken ct)
    {
        // Omitted technologies preserve existing links; [] explicitly removes all.
        if (req.TechnologyIds != null) {
            db.RemoveRange(await db.Set<VacXin_CongNghe>().Where(x => x.MaVacXin == id).ToArrayAsync(ct));
            await db.SaveChangesAsync(ct);
            db.AddRange(req.TechnologyIds.Select(t => new VacXin_CongNghe { MaVacXin = id, MaCongNghe = t }));
        }
        if (req.DiseaseIds != null) {
            db.RemoveRange(await db.Set<VacXin_Benh>().Where(x => x.MaVacXin == id).ToArrayAsync(ct));
            await db.SaveChangesAsync(ct);
            db.AddRange(req.DiseaseIds.Select(d => new VacXin_Benh { MaVacXin = id, MaBenh = d }));
        }
        if (req.Regimens != null) {
            db.RemoveRange(await db.Set<VaccineRegimen>().Where(x => x.VaccineId == id).ToArrayAsync(ct));
            await db.SaveChangesAsync(ct);
            foreach (var r in req.Regimens) {
                var regimen = new VaccineRegimen { VaccineId = id, Name = r.Name.Trim(), TargetAudience = r.TargetAudience.Trim(), Summary = r.Summary?.Trim(), SourceName = r.SourceName?.Trim(), SourceUrl = r.SourceUrl?.Trim() };
                db.Add(regimen); await db.SaveChangesAsync(ct);
                db.AddRange(r.Doses.Select(d => new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = d.DoseOrder, TimingLabel = d.TimingLabel.Trim(),
                    MinimumIntervalDays = d.MinimumIntervalDays, RecommendedIntervalDays = d.RecommendedIntervalDays, Note = d.Note?.Trim() }));
            }
        }
    }
    private static async Task ValidatePackage(PackageRequest req, GoiVacXin? row, TiemChungDbContext db, CancellationToken ct)
    {
        ValidateCode(req.Code, "GOI", row?.Code); ValidateStatus(req.Status); ValidateText(req.Name, 150); ValidatePrice(req.Price);
        ValidateText(req.Description, 2000, false); ValidateText(req.TargetAudience, 1000, row == null || req.TargetAudience != null);
        if (req.Items == null || req.Items.Length is < 1 or > 100 || req.Items.Any(x => x == null || x.VaccineId <= 0 || x.Quantity is < 1 or > 100) ||
            req.Items.Select(x => x.VaccineId).Distinct().Count() != req.Items.Length) throw new IdentityFailure(400, "validation");
        var ids = req.Items.Select(x => x.VaccineId).ToArray();
        var active = (req.Status ?? row?.TrangThai ?? "Active") == "Active";
        if (await db.Vaccines.CountAsync(x => ids.Contains(x.MaVacXin) && (!active || x.TrangThai == "Active"), ct) != ids.Length)
            throw new IdentityFailure(400, "validation");
    }
    private static void SetItems(TiemChungDbContext db, int id, PackageItemDto[] items) => db.AddRange(items.Select(x => new GoiVacXin_VacXin { MaGoi = id, MaVacXin = x.VaccineId, SoLuong = x.Quantity }));
    private static async Task Audit(TiemChungDbContext db, HttpContext context, IdentityActor actor, string action, string type, int id, CancellationToken ct)
    {
        var audit = IdentityTransactions.Audit(db, "catalog." + action, context.TraceIdentifier, actor.Account.MaTaiKhoan);
        audit.ResourceType = type; audit.ResourceId = id.ToString(System.Globalization.CultureInfo.InvariantCulture); await db.SaveChangesAsync(ct);
    }
}
