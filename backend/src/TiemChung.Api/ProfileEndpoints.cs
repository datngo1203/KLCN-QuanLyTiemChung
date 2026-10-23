using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Profiles");

        // GET /api/v1/customers
        group.MapGet("/customers", async (HttpContext context, IdentityAuthorization auth, string? search, int? page, int? pageSize, TiemChungDbContext db, CancellationToken ct) =>
        {
            var actor = await auth.RequireAsync(context, false, ct);
            if (!actor.IsCustomer && !actor.CanReadProfiles) throw new IdentityFailure(403,"permissionDenied");
            ValidateList(search,page,pageSize);
            var query = db.Customers.AsNoTracking().AsQueryable();
            if (actor.IsCustomer) query = query.Where(c => db.Set<AccountProfileLink>().Any(l =>
                l.AccountId == actor.Account.MaTaiKhoan && l.State == "Active" && l.CustomerId == c.MaKhachHang));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(c =>
                    c.HoTen.Contains(keyword) ||
                    c.SoDienThoai.Contains(keyword) ||
                    c.CCCD.Contains(keyword));
            }

            var items = await query
                .OrderByDescending(c => c.MaKhachHang)
                .Skip(((page ?? 1)-1)*(pageSize ?? 25)).Take(pageSize ?? 25)
                .Select(c => new CustomerDto(c.MaKhachHang, c.HoTen, c.CCCD, c.SoDienThoai, c.Email))
                .ToListAsync(ct);

            return Results.Ok(new CustomerListResponse(items));
        }).WithName("ListCustomers");

        // POST /api/v1/customers
        group.MapPost("/customers", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var transaction = await IdentityTransactions.BeginAsync(db,ct);
            var actor = await auth.RequireAsync(context,true,ct);
            if (!actor.CanCreateProfiles) throw new IdentityFailure(403,"permissionDenied");
            var body = await IdentityEndpoints.Payload(context,["fullName","identityNumber","phoneNumber","email"],ct);
            var req = new CreateCustomerRequest(IdentityEndpoints.Value(body,"fullName"),IdentityEndpoints.Value(body,"identityNumber"),
                IdentityEndpoints.Value(body,"phoneNumber"),IdentityEndpoints.Value(body,"email"));
            var errors = ValidateCustomer(req);
            if (errors.Count > 0) throw new IdentityFailure(400,"validation") { Errors=errors };

            var normalizedCccd = req.IdentityNumber.Trim();
            var cccdExists = await db.Customers.AnyAsync(c => c.CCCD == normalizedCccd, ct);
            if (cccdExists)
            {
                throw new IdentityFailure(409,"duplicateIdentity");
            }

            var entity = new KhachHang
            {
                HoTen = req.FullName.Trim(),
                CCCD = normalizedCccd,
                SoDienThoai = req.PhoneNumber.Trim(),
                Email = req.Email.Trim()
            };

            db.Customers.Add(entity);
            try { await db.SaveChangesAsync(ct); }
            catch(DbUpdateException e) when(e.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                sql.Number is 2601 or 2627 && sql.Message.Contains("IX_KhachHang_CCCD",StringComparison.Ordinal)) {
                throw new IdentityFailure(409,"duplicateIdentity");
            }
            var audit = IdentityTransactions.Audit(db,"profiles.created",context.TraceIdentifier,actor.Account.MaTaiKhoan);
            audit.ResourceType="Customer"; audit.ResourceId=entity.MaKhachHang.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var dto = new CustomerDto(entity.MaKhachHang, entity.HoTen, entity.CCCD, entity.SoDienThoai, entity.Email);
            return Results.Created($"/api/v1/customers/{dto.Id}", dto);
        }).WithName("CreateCustomer");

        // GET /api/v1/recipients
        group.MapGet("/recipients", async (HttpContext context, IdentityAuthorization auth, int? customerId, string? search, int? page, int? pageSize, TiemChungDbContext db, CancellationToken ct) =>
        {
            var actor = await auth.RequireAsync(context,false,ct);
            if (!actor.IsCustomer && !actor.CanReadProfiles) throw new IdentityFailure(403,"permissionDenied");
            ValidateList(search,page,pageSize);
            if(customerId is <=0) throw new IdentityFailure(400,"validation");
            var query = db.Recipients.AsNoTracking().AsQueryable();
            if(actor.IsCustomer) query=query.Where(r=>db.Set<AccountProfileLink>().Any(l=>
                l.AccountId==actor.Account.MaTaiKhoan && l.State=="Active" && l.RecipientId==r.MaNguoiDuocTiem));

            if (customerId.HasValue)
            {
                query = query.Where(r => r.MaKhachHang == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(r => r.HoTen.Contains(keyword) || (r.CCCD != null && r.CCCD.Contains(keyword)));
            }

            var items = await query
                .OrderByDescending(r => r.MaNguoiDuocTiem)
                .Skip(((page ?? 1)-1)*(pageSize ?? 25)).Take(pageSize ?? 25)
                .Select(r => new RecipientDto(
                    r.MaNguoiDuocTiem,
                    r.MaKhachHang,
                    r.HoTen,
                    r.CCCD,
                    r.NgaySinh,
                    r.GioiTinh,
                    r.QuanHeVoiKhachHang))
                .ToListAsync(ct);

            return Results.Ok(new RecipientListResponse(items));
        }).WithName("ListRecipients");

        // POST /api/v1/recipients
        group.MapPost("/recipients", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var transaction=await IdentityTransactions.BeginAsync(db,ct);
            var actor=await auth.RequireAsync(context,true,ct);
            if(!actor.CanCreateProfiles) throw new IdentityFailure(403,"permissionDenied");
            var body=await IdentityEndpoints.Payload(context,["customerId","fullName","identityNumber","dateOfBirth","gender","relationshipToCustomer"],ct);
            if(!body.TryGetProperty("customerId",out var customer) || customer.ValueKind!=System.Text.Json.JsonValueKind.Number || !customer.TryGetInt32(out var customerId)
                || !DateOnly.TryParseExact(IdentityEndpoints.Value(body,"dateOfBirth"),"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,out var dob)) throw new IdentityFailure(400,"validation");
            var req=new CreateRecipientRequest(customerId,IdentityEndpoints.Value(body,"fullName"),
                body.TryGetProperty("identityNumber",out var number) && number.ValueKind!=System.Text.Json.JsonValueKind.Null ? IdentityEndpoints.Value(body,"identityNumber") : null,
                dob,IdentityEndpoints.Value(body,"gender"),IdentityEndpoints.Value(body,"relationshipToCustomer"));
            var errors = ValidateRecipient(req.FullName, req.IdentityNumber, req.DateOfBirth, req.Gender, req.RelationshipToCustomer);
            if (req.CustomerId <= 0)
            {
                errors["customerId"] = ["Mã khách hàng bảo trợ phải là số nguyên dương."];
            }
            if (errors.Count > 0) throw new IdentityFailure(400,"validation") { Errors=errors };

            var customerExists = await db.Customers.AnyAsync(c => c.MaKhachHang == req.CustomerId, ct);
            if (!customerExists)
            {
                throw new IdentityFailure(404,"resourceNotFound");
            }

            var entity = new NguoiDuocTiem
            {
                MaKhachHang = req.CustomerId,
                HoTen = req.FullName.Trim(),
                CCCD = string.IsNullOrWhiteSpace(req.IdentityNumber) ? null : req.IdentityNumber.Trim(),
                NgaySinh = req.DateOfBirth,
                GioiTinh = req.Gender.Trim(),
                QuanHeVoiKhachHang = req.RelationshipToCustomer.Trim()
            };

            db.Recipients.Add(entity);
            await db.SaveChangesAsync(ct);
            var audit=IdentityTransactions.Audit(db,"profiles.created",context.TraceIdentifier,actor.Account.MaTaiKhoan);
            audit.ResourceType="Recipient"; audit.ResourceId=entity.MaNguoiDuocTiem.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);

            var dto = new RecipientDto(
                entity.MaNguoiDuocTiem,
                entity.MaKhachHang,
                entity.HoTen,
                entity.CCCD,
                entity.NgaySinh,
                entity.GioiTinh,
                entity.QuanHeVoiKhachHang);

            return Results.Created($"/api/v1/recipients/{dto.Id}", dto);
        }).WithName("CreateRecipient");

        // PUT /api/v1/recipients/{id}
        group.MapPut("/recipients/{id:int}", async (HttpContext context, IdentityAuthorization auth, int id, TiemChungDbContext db, CancellationToken ct) =>
        {
            var actor=await auth.RequireAsync(context,false,ct);
            if(!actor.CanCreateProfiles) throw new IdentityFailure(403,"permissionDenied");

            var entity = await db.Recipients.FirstOrDefaultAsync(r => r.MaNguoiDuocTiem == id, ct);
            if (entity is null)
            {
                throw new IdentityFailure(404,"resourceNotFound");
            }

            var body=await IdentityEndpoints.Payload(context,["fullName","identityNumber","dateOfBirth","gender","relationshipToCustomer"],ct);
            if(!DateOnly.TryParseExact(IdentityEndpoints.Value(body,"dateOfBirth"),"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,out var dob)) throw new IdentityFailure(400,"validation");
            var identity=body.TryGetProperty("identityNumber",out var number) && number.ValueKind!=System.Text.Json.JsonValueKind.Null
                ? IdentityEndpoints.Value(body,"identityNumber") : null;
            var errors=ValidateRecipient(IdentityEndpoints.Value(body,"fullName"),identity,dob,IdentityEndpoints.Value(body,"gender"),IdentityEndpoints.Value(body,"relationshipToCustomer"));
            if(errors.Count>0) throw new IdentityFailure(400,"validation") { Errors=errors };
            throw new IdentityFailure(409,"identityApprovalRequired");
        }).WithName("UpdateRecipient");

        return app;
    }

    private static void ValidateList(string? search,int? page,int? size)
    {
        if(search?.Trim().Length>150 || page is <=0 || size is <=0 or >100 ||
            (long)((page??1)-1)*(size??25)>int.MaxValue) throw new IdentityFailure(400,"validation");
    }

    private static Dictionary<string, string[]> ValidateCustomer(CreateCustomerRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length > 150)
            errors["fullName"] = ["Họ tên khách hàng là bắt buộc và tối đa 150 ký tự."];

        var cccd = req.IdentityNumber?.Trim() ?? string.Empty;
        if (cccd.Length != 12 || !cccd.All(char.IsAsciiDigit))
            errors["identityNumber"] = ["CCCD phải gồm đúng 12 chữ số."];

        var phone = req.PhoneNumber?.Trim() ?? string.Empty;
        if (phone.Length is <10 or >11 || !phone.All(char.IsAsciiDigit))
            errors["phoneNumber"] = ["Số điện thoại không hợp lệ (chỉ chứa chữ số, tối đa 15 ký tự)."];

        var email = req.Email?.Trim() ?? string.Empty;
        if (email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email,out var address) || address.Address!=email)
            errors["email"] = ["Email không hợp lệ."];

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRecipient(
        string fullName,
        string? identityNumber,
        DateOnly dateOfBirth,
        string gender,
        string relationship)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 150)
            errors["fullName"] = ["Họ tên người tiêm là bắt buộc và tối đa 150 ký tự."];

        if (!string.IsNullOrWhiteSpace(identityNumber))
        {
            var cccd = identityNumber.Trim();
            if (cccd.Length != 12 || !cccd.All(char.IsAsciiDigit))
                errors["identityNumber"] = ["CCCD (nếu nhập) phải gồm đúng 12 chữ số."];
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"Asia/Ho_Chi_Minh").DateTime);
        if (dateOfBirth == DateOnly.MinValue || dateOfBirth > today)
            errors["dateOfBirth"] = ["Ngày sinh không được lớn hơn ngày hiện tại."];

        if (gender is not ("Nam" or "Nữ" or "Khác"))
            errors["gender"] = ["Giới tính là bắt buộc và tối đa 20 ký tự."];

        if (string.IsNullOrWhiteSpace(relationship) || relationship.Trim().Length > 50)
            errors["relationshipToCustomer"] = ["Quan hệ với khách hàng là bắt buộc và tối đa 50 ký tự."];

        return errors;
    }
}
