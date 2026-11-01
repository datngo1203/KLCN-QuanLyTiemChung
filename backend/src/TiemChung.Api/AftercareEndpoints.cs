using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class AftercareEndpoints
{
    private static async Task<IdentityActor> RequireNurse(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor=await auth.RequireAsync(context,write,ct);
        if(actor.IsCustomer || actor.Employee?.ChucDanh!="DieuDuong")throw new IdentityFailure(403,"permissionDenied");
        return actor;
    }
    private static IQueryable<MuiTiem> Sources(TiemChungDbContext db) =>
        from m in db.Set<MuiTiem>().AsNoTracking()
        join i in VaccinationEndpoints.Sources(db) on m.MaChiDinh equals i.MaChiDinh
        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
        join l in db.Set<LoVacXin>() on m.SoLo equals l.SoLo
        where m.TrangThai=="DaTiem" && m.MaNguoiDuocTiem==p.MaNguoiDuocTiem && l.MaVacXin==i.MaVacXin
        select m;
    private static IQueryable<VaccinationDto> Query(TiemChungDbContext db,IQueryable<MuiTiem> rows) =>
        from m in rows
        join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
        join r in db.Recipients on m.MaNguoiDuocTiem equals r.MaNguoiDuocTiem
        join v in db.Vaccines on i.MaVacXin equals v.MaVacXin
        join n in db.Set<NhanVien>() on m.MaNhanVien equals n.MaNhanVien
        select new VaccinationDto(m.MaMuiTiem,i.MaChiDinh,p.MaLichHen!.Value,p.MaDangKy,
            new(r.MaNguoiDuocTiem,r.HoTen),new(v.MaVacXin,v.TenVacXin),m.SoLo,m.ViTriTiem,new(n.MaNhanVien,n.HoTen),
            new DateTimeOffset(DateTime.SpecifyKind(m.ThoiDiemTiem,DateTimeKind.Utc)),m.TrangThai);
    private static Task<FollowUpDto?> Result(TiemChungDbContext db,int id,CancellationToken ct) =>
        (from f in db.Set<TheoDoiSauTiem>().AsNoTracking()
         join n in db.Set<NhanVien>() on f.MaNhanVien equals n.MaNhanVien into nurses
         from n in nurses.DefaultIfEmpty()
         where f.MaMuiTiem==id
         select new FollowUpDto(f.MaTheoDoi,f.MaMuiTiem,f.TinhTrang,f.MucDoPhanUng,f.HuongXuTri,
            n==null?null:new RegistrationProfileDto(n.MaNhanVien,n.HoTen),
            new DateTimeOffset(DateTime.SpecifyKind(f.ThoiDiem,DateTimeKind.Utc)))).SingleOrDefaultAsync(ct);
    private static async Task<VaccinationDto> Source(TiemChungDbContext db,int id,CancellationToken ct)
    {
        if(id<=0)throw new IdentityFailure(400,"validation");
        if(!await db.Set<MuiTiem>().AnyAsync(m=>m.MaMuiTiem==id,ct))throw new IdentityFailure(404,"resourceNotFound");
        return await Query(db,Sources(db).Where(m=>m.MaMuiTiem==id)).SingleOrDefaultAsync(ct)
            ??throw new IdentityFailure(409,"followUpSourceConflict");
    }
    public static void MapAftercareEndpoints(this IEndpointRouteBuilder app)
    {
        var group=app.MapGroup("/api/v1/vaccinations").WithTags("Aftercare");
        group.AddEndpointFilter(async(invocation,next)=>{
            try{return await next(invocation);}
            catch(DbUpdateException e)when(e.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
                sql.Message.Contains("IX_TheoDoiSauTiem_MaMuiTiem",StringComparison.Ordinal)){
                throw new IdentityFailure(409,"followUpAlreadyRecorded");
            }
            catch(DbUpdateException){throw new IdentityFailure(503,"dependencyUnavailable");}
        });
        group.MapGet("/follow-up-pending",async(HttpContext context,IdentityAuthorization auth,TiemChungDbContext db,
            string? search,int? page,int? pageSize,CancellationToken ct)=>{
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
            await RequireNurse(context,auth,false,ct);
            search=search?.Trim();
            if(search?.Length>100 || page is <=0 || pageSize is <=0 or >100 || (long)((page??1)-1)*(pageSize??25)>int.MaxValue)
                throw new IdentityFailure(400,"validation");
            var code=search?.StartsWith("MT-",StringComparison.OrdinalIgnoreCase)==true?search[3..]:search;
            int.TryParse(code,out var id);
            var rows=Sources(db).Where(m=>!db.Set<TheoDoiSauTiem>().Any(f=>f.MaMuiTiem==m.MaMuiTiem));
            if(!string.IsNullOrEmpty(search))rows=rows.Where(m=>m.MaMuiTiem==id || db.Recipients.Any(r=>r.MaNguoiDuocTiem==m.MaNguoiDuocTiem && r.HoTen.Contains(search)));
            var items=await Query(db,rows.OrderByDescending(m=>m.MaMuiTiem).Skip(((page??1)-1)*(pageSize??25)).Take(pageSize??25)).ToArrayAsync(ct);
            await tx.CommitAsync(ct);return Results.Ok(new FollowUpPendingResponse(items));
        });
        group.MapGet("/{id:int}/follow-ups",async(int id,HttpContext context,IdentityAuthorization auth,TiemChungDbContext db,CancellationToken ct)=>{
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
            await RequireNurse(context,auth,false,ct);
            var source=await Source(db,id,ct);var result=await Result(db,id,ct);
            await tx.CommitAsync(ct);return Results.Ok(new FollowUpDetailDto(source,result));
        });
        group.MapPost("/{id:int}/follow-ups",async(int id,CreateFollowUpRequest req,HttpContext context,IdentityAuthorization auth,TiemChungDbContext db,CancellationToken ct)=>{
            await using var tx=await IdentityTransactions.BeginAsync(db,ct);
            var actor=await RequireNurse(context,auth,true,ct);
            if(id<=0 || string.IsNullOrWhiteSpace(req.Condition) || req.Condition.Trim().Length>100 ||
                string.IsNullOrWhiteSpace(req.Severity) || req.Severity.Trim().Length>100 ||
                string.IsNullOrWhiteSpace(req.Treatment) || req.Treatment.Trim().Length>500)throw new IdentityFailure(400,"validation");
            var shot=await db.Set<MuiTiem>().FromSqlInterpolated($"SELECT * FROM MuiTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaMuiTiem={id}").SingleOrDefaultAsync(ct)
                ??throw new IdentityFailure(404,"resourceNotFound");
            await Source(db,id,ct);
            if(await db.Set<TheoDoiSauTiem>().FromSqlInterpolated($"SELECT * FROM TheoDoiSauTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaMuiTiem={id}").AnyAsync(ct))
                throw new IdentityFailure(409,"followUpAlreadyRecorded");
            var now=DateTimeOffset.UtcNow;
            if(now.UtcDateTime<shot.ThoiDiemTiem)throw new IdentityFailure(409,"followUpSourceConflict");
            var row=new TheoDoiSauTiem{MaMuiTiem=id,MaNhanVien=actor.Employee!.MaNhanVien,ThoiDiem=now.UtcDateTime,
                TinhTrang=req.Condition.Trim(),MucDoPhanUng=req.Severity.Trim(),HuongXuTri=req.Treatment.Trim()};
            db.Add(row);await db.SaveChangesAsync(ct);
            var audit=IdentityTransactions.Audit(db,"aftercare.recorded",context.TraceIdentifier,actor.Account.MaTaiKhoan,session:actor.Session.Id);
            audit.ResourceType="FollowUp";audit.ResourceId=row.MaTheoDoi.ToString(CultureInfo.InvariantCulture);await db.SaveChangesAsync(ct);
            var result=await Result(db,id,ct);await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/vaccinations/{id}/follow-ups",result);
        }).Produces<FollowUpDto>(201);
    }
}
