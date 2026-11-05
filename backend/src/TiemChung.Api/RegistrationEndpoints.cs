using System.Globalization;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Domain.Rules;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class RegistrationEndpoints
{
    internal const decimal MaxMoney = CounterPaymentRules.MaxMoney;

    public static void MapRegistrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/registrations").WithTags("Registration");
        group.AddEndpointFilter(async (invocation, next) => {
            invocation.HttpContext.Response.Headers.CacheControl = "no-store";
            try { return await next(invocation); }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });

        var configuredFee = app.Configuration["Registration:ConsultationFeeVnd"];
        group.MapGet("/settings", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            await RequireActor(context, auth, false, ct);
            var fee = ReadConsultationFee(configuredFee);
            await tx.CommitAsync(ct);
            return Results.Ok(new RegistrationSettingsDto(fee));
        }).WithName("GetRegistrationSettings").Produces<RegistrationSettingsDto>();

        group.MapPost("", async (CreateRegistrationRequest req, HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireActor(context, auth, true, ct);
            Validate(req);
            var now = DateTimeOffset.UtcNow;
            if (!IsPreferredDateValid(req.PreferredDate, now)) throw new IdentityFailure(400, "validation");
            var contact = ValidateContact(req.Contact);
            var fee = ReadConsultationFee(configuredFee);
            if (actor.IsCustomer && (!await HasLink(db, actor, req.CustomerId, null, ct) ||
                !await HasLink(db, actor, null, req.RecipientId, ct)))
                throw new IdentityFailure(403, "permissionDenied");
            if (!await db.Customers.AnyAsync(x => x.MaKhachHang == req.CustomerId, ct))
                throw new IdentityFailure(404, "resourceNotFound");
            var recipient = await db.Recipients.AsNoTracking().SingleOrDefaultAsync(x => x.MaNguoiDuocTiem == req.RecipientId, ct)
                ?? throw new IdentityFailure(404, "resourceNotFound");
            if (recipient.MaKhachHang != req.CustomerId) throw new IdentityFailure(400, "validation");

            // Serialize with Catalog PUT at the package row; hold prices/content until commit.
            var packages = new Dictionary<int, GoiVacXin>();
            foreach (var id in req.Items.Where(x => x.Kind == "package").Select(x => x.ServiceId).Order()) {
                var package = await db.Set<GoiVacXin>().FromSqlInterpolated(
                    $"SELECT * FROM GoiVacXin WITH (UPDLOCK,HOLDLOCK) WHERE MaGoi={id}").SingleOrDefaultAsync(ct)
                    ?? throw new IdentityFailure(404, "resourceNotFound");
                if (package.TrangThai != "Active") throw new IdentityFailure(400, "validation");
                packages.Add(id, package);
            }
            var packageIds = packages.Keys.ToArray();
            var components = await db.Set<GoiVacXin_VacXin>().AsNoTracking()
                .Where(x => packageIds.Contains(x.MaGoi)).ToArrayAsync(ct);
            var vaccineIds = req.Items.Where(x => x.Kind == "vaccine").Select(x => x.ServiceId)
                .Concat(components.Select(x => x.MaVacXin)).Distinct().Order().ToArray();
            var vaccines = await db.Vaccines.AsNoTracking().Where(x => vaccineIds.Contains(x.MaVacXin))
                .ToDictionaryAsync(x => x.MaVacXin, ct);
            if (vaccines.Count != vaccineIds.Length) throw new IdentityFailure(404, "resourceNotFound");
            if (vaccines.Values.Any(x => x.TrangThai != "Active")) throw new IdentityFailure(400, "validation");

            var lines = new List<RegistrationLine>();
            foreach (var item in req.Items) {
                var price = item.Kind == "vaccine" ? vaccines[item.ServiceId].Gia : packages[item.ServiceId].GiaGoi;
                if (price < 0 || price > MaxMoney || decimal.Truncate(price) != price)
                    throw new IdentityFailure(400, "validation");
                if (item.ExpectedUnitPrice != price) throw new IdentityFailure(409, "catalogChanged");
                if (item.Kind == "package") {
                    var actual = components.Where(x => x.MaGoi == item.ServiceId).OrderBy(x => x.MaVacXin).ToArray();
                    if (actual.Length is < 1 or > 100 || actual.Any(x => x.SoLuong is < 1 or > 100))
                        throw new IdentityFailure(400, "validation");
                    if (!actual.Select(x => (x.MaVacXin, x.SoLuong)).SequenceEqual(
                        item.ExpectedComponents!.OrderBy(x => x.VaccineId).Select(x => (x.VaccineId, x.Quantity))))
                        throw new IdentityFailure(409, "catalogChanged");
                }
                lines.Add(new(item.Quantity, price, item.Kind == "package"));
            }
            var total = RegistrationRules.CalculateTotal(lines);
            if (total + fee > MaxMoney) throw new IdentityFailure(400, "validation");
            var row = new DangKyDichVu {
                MaKhachHang = req.CustomerId, MaNguoiDuocTiem = req.RecipientId,
                MaNhanVien = actor.Employee?.MaNhanVien, NgayDangKy = now.UtcDateTime, TrangThai = "DaDangKy",
                // Store Vietnam's calendar date at midnight, without a timezone conversion.
                ThoiGianDuKien = req.PreferredDate!.Value.ToDateTime(TimeOnly.MinValue),
                TenNguoiLienHe = contact.FullName, SoDienThoaiLienHe = contact.PhoneNumber, EmailLienHe = contact.Email,
                TinhThanhLienHe = contact.ProvinceCity, PhuongXaLienHe = contact.WardCommune,
                DiaChiLienHe = contact.AddressLine, PhiKhamTuVan = fee
            };
            db.Registrations.Add(row);
            await db.SaveChangesAsync(ct);
            for (var i = 0; i < req.Items.Length; i++) {
                var item = req.Items[i];
                if (item.Kind == "vaccine") db.RegistrationVaccineItems.Add(new() {
                    MaDangKy = row.MaDangKy, MaVacXin = item.ServiceId, SoLuong = item.Quantity, GiaTaiThoiDiemDangKy = lines[i].UnitPrice
                });
                else db.RegistrationPackageItems.Add(new() {
                    MaDangKy = row.MaDangKy, MaGoi = item.ServiceId, SoLuongGoi = item.Quantity, GiaTaiThoiDiemDangKy = lines[i].UnitPrice
                });
            }
            var audit = IdentityTransactions.Audit(db, "registrations.created", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Registration";
            audit.ResourceId = row.MaDangKy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var dto = await Detail(db, row, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/registrations/{row.MaDangKy}", dto);
        }).WithName("CreateRegistration").Produces<RegistrationDto>(201);

        group.MapGet("", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            int? customerId, int? recipientId, int? page, int? pageSize, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireActor(context, auth, false, ct);
            if (customerId is <= 0 || recipientId is <= 0 || page is <= 0 || pageSize is <= 0 or > 100 ||
                (long)((page ?? 1) - 1) * (pageSize ?? 25) > int.MaxValue) throw new IdentityFailure(400, "validation");
            var query = Scope(db, actor).Where(x => (!customerId.HasValue || x.MaKhachHang == customerId) &&
                (!recipientId.HasValue || x.MaNguoiDuocTiem == recipientId));
            var rows = await query.OrderByDescending(x => x.MaDangKy).Skip(((page ?? 1) - 1) * (pageSize ?? 25))
                .Take(pageSize ?? 25).ToArrayAsync(ct);
            var ids = rows.Select(x => x.MaDangKy).ToArray();
            var customers = await db.Customers.AsNoTracking().Where(x => rows.Select(r => r.MaKhachHang).Contains(x.MaKhachHang))
                .ToDictionaryAsync(x => x.MaKhachHang, ct);
            var recipients = await db.Recipients.AsNoTracking().Where(x => rows.Select(r => r.MaNguoiDuocTiem).Contains(x.MaNguoiDuocTiem))
                .ToDictionaryAsync(x => x.MaNguoiDuocTiem, ct);
            var singles = await db.RegistrationVaccineItems.AsNoTracking().Where(x => ids.Contains(x.MaDangKy)).ToArrayAsync(ct);
            var packages = await db.RegistrationPackageItems.AsNoTracking().Where(x => ids.Contains(x.MaDangKy)).ToArrayAsync(ct);
            var items = rows.Select(x => new RegistrationSummaryDto(x.MaDangKy, Utc(x.NgayDangKy), x.TrangThai,
                new(x.MaKhachHang, customers[x.MaKhachHang].HoTen), new(x.MaNguoiDuocTiem, recipients[x.MaNguoiDuocTiem].HoTen), x.MaNhanVien,
                singles.Where(s => s.MaDangKy == x.MaDangKy).Sum(s => s.SoLuong * s.GiaTaiThoiDiemDangKy) +
                packages.Where(p => p.MaDangKy == x.MaDangKy).Sum(p => p.SoLuongGoi * p.GiaTaiThoiDiemDangKy), "VND",
                PreferredDate(x), x.PhiKhamTuVan, x.KenhThanhToanDuKien)).ToArray();
            await tx.CommitAsync(ct);
            return Results.Ok(new RegistrationListResponse(items));
        }).WithName("ListRegistrations").Produces<RegistrationListResponse>();

        group.MapGet("/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireActor(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await Scope(db, actor).SingleOrDefaultAsync(x => x.MaDangKy == id, ct);
            if (row is null) throw new IdentityFailure(actor.IsCustomer ? 403 : 404,
                actor.IsCustomer ? "permissionDenied" : "resourceNotFound");
            var dto = await Detail(db, row, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(dto);
        }).WithName("GetRegistration").Produces<RegistrationDto>();
    }

    private static async Task<IdentityActor> RequireActor(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (!actor.IsCustomer && actor.Employee?.ChucDanh != "LeTan") throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    private static Task<bool> HasLink(TiemChungDbContext db, IdentityActor actor, int? customerId, int? recipientId, CancellationToken ct) =>
        db.Set<AccountProfileLink>().AnyAsync(x => x.AccountId == actor.Account.MaTaiKhoan && x.State == "Active" &&
            (customerId != null ? x.CustomerId == customerId : x.RecipientId == recipientId), ct);

    internal static IQueryable<DangKyDichVu> Scope(TiemChungDbContext db, IdentityActor actor)
    {
        var query = db.Registrations.AsNoTracking();
        if (!actor.IsCustomer) return query;
        return query.Where(r => db.Set<AccountProfileLink>().Any(l => l.AccountId == actor.Account.MaTaiKhoan && l.State == "Active" && l.CustomerId == r.MaKhachHang) &&
            db.Set<AccountProfileLink>().Any(l => l.AccountId == actor.Account.MaTaiKhoan && l.State == "Active" && l.RecipientId == r.MaNguoiDuocTiem) &&
            db.Recipients.Any(p => p.MaNguoiDuocTiem == r.MaNguoiDuocTiem && p.MaKhachHang == r.MaKhachHang));
    }

    private static void Validate(CreateRegistrationRequest req)
    {
        if (req.CustomerId <= 0 || req.RecipientId <= 0 || req.Items is null || req.Items.Length is < 1 or > 100 ||
            req.Items.Any(x => x is null || x.Kind is not ("vaccine" or "package") || x.ServiceId <= 0 || x.Quantity is < 1 or > 100 ||
                x.ExpectedUnitPrice < 0 || x.ExpectedUnitPrice > MaxMoney || decimal.Truncate(x.ExpectedUnitPrice) != x.ExpectedUnitPrice) ||
            req.Items.Select(x => (x.Kind, x.ServiceId)).Distinct().Count() != req.Items.Length)
            throw new IdentityFailure(400, "validation");
        foreach (var item in req.Items) {
            if (item.Kind == "vaccine" && item.ExpectedComponents != null || item.Kind == "package" &&
                (item.ExpectedComponents is null || item.ExpectedComponents.Length is < 1 or > 100 ||
                 item.ExpectedComponents.Any(x => x is null || x.VaccineId <= 0 || x.Quantity is < 1 or > 100) ||
                 item.ExpectedComponents.Select(x => x.VaccineId).Distinct().Count() != item.ExpectedComponents.Length))
                throw new IdentityFailure(400, "validation");
        }
    }

    public static bool IsPreferredDateValid(DateOnly? preferredDate, DateTimeOffset now) =>
        preferredDate >= VaccinationEndpoints.FacilityDate(now);

    private static decimal ReadConsultationFee(string? configured)
    {
        if (configured is null) return 0;
        if (!decimal.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out var fee) || fee > MaxMoney)
            throw new IdentityFailure(503, "dependencyUnavailable");
        return fee;
    }

    private static RegistrationContactDto ValidateContact(RegistrationContactRequest? input)
    {
        var name = input?.FullName?.Trim();
        var phone = input?.PhoneNumber;
        var email = input?.Email?.Trim();
        var province = input?.ProvinceCity?.Trim();
        var ward = input?.WardCommune?.Trim();
        var address = input?.AddressLine?.Trim();
        if (name is not { Length: >= 1 and <= 150 } || phone is not { Length: 10 or 11 } ||
            phone.Any(c => c is < '0' or > '9') || email is not { Length: >= 1 and <= 254 } ||
            !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email ||
            province is not { Length: >= 1 and <= 100 } || ward is not { Length: >= 1 and <= 100 } ||
            address is not { Length: >= 1 and <= 250 })
            throw new IdentityFailure(400, "validation");
        return new(name, phone, email, province, ward, address);
    }

    internal static DateOnly? PreferredDate(DangKyDichVu row) =>
        row.ThoiGianDuKien is { } value ? DateOnly.FromDateTime(value) : null;

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    internal static async Task<RegistrationDto> Detail(TiemChungDbContext db, DangKyDichVu row, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleAsync(x => x.MaKhachHang == row.MaKhachHang, ct);
        var recipient = await db.Recipients.AsNoTracking().SingleAsync(x => x.MaNguoiDuocTiem == row.MaNguoiDuocTiem, ct);
        var singles = await (from item in db.RegistrationVaccineItems.AsNoTracking()
            join vaccine in db.Vaccines on item.MaVacXin equals vaccine.MaVacXin
            where item.MaDangKy == row.MaDangKy orderby item.MaChiTietVacXin
            select new RegistrationItemDto(item.MaChiTietVacXin, "vaccine", item.MaVacXin, vaccine.TenVacXin,
                item.SoLuong, item.GiaTaiThoiDiemDangKy, item.SoLuong * item.GiaTaiThoiDiemDangKy, null)).ToArrayAsync(ct);
        var packages = await (from item in db.RegistrationPackageItems.AsNoTracking()
            join package in db.Set<GoiVacXin>() on item.MaGoi equals package.MaGoi
            where item.MaDangKy == row.MaDangKy orderby item.MaChiTietGoi
            select new { Item = item, package.TenGoi }).ToArrayAsync(ct);
        var packageIds = packages.Select(x => x.Item.MaGoi).ToArray();
        var components = await (from link in db.Set<GoiVacXin_VacXin>().AsNoTracking()
            join vaccine in db.Vaccines on link.MaVacXin equals vaccine.MaVacXin
            where packageIds.Contains(link.MaGoi) orderby link.MaVacXin
            select new { link.MaGoi, Component = new RegistrationComponentDto(link.MaVacXin, link.SoLuong, vaccine.TenVacXin) }).ToArrayAsync(ct);
        var items = singles.Concat(packages.Select(x => new RegistrationItemDto(x.Item.MaChiTietGoi, "package", x.Item.MaGoi, x.TenGoi,
            x.Item.SoLuongGoi, x.Item.GiaTaiThoiDiemDangKy, x.Item.SoLuongGoi * x.Item.GiaTaiThoiDiemDangKy,
            components.Where(c => c.MaGoi == x.Item.MaGoi).Select(c => c.Component).ToArray()))).ToArray();
        var contact = row.TenNguoiLienHe is null && row.SoDienThoaiLienHe is null && row.EmailLienHe is null &&
            row.TinhThanhLienHe is null && row.PhuongXaLienHe is null && row.DiaChiLienHe is null ? null :
            new RegistrationContactDto(row.TenNguoiLienHe, row.SoDienThoaiLienHe, row.EmailLienHe,
                row.TinhThanhLienHe, row.PhuongXaLienHe, row.DiaChiLienHe);
        return new(row.MaDangKy, Utc(row.NgayDangKy), row.TrangThai, new(customer.MaKhachHang, customer.HoTen),
            new(recipient.MaNguoiDuocTiem, recipient.HoTen), row.MaNhanVien, items, items.Sum(x => x.LineTotal), "VND",
            PreferredDate(row), contact, row.PhiKhamTuVan, row.KenhThanhToanDuKien);
    }
}
