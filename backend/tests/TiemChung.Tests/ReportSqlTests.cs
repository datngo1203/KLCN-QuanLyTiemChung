using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class ReportSqlTests
{
    private const string MonthlyRevenueRoute = "/api/v1/reports/monthly-revenue";
    private const string VaccineDosesRoute = "/api/v1/reports/vaccine-doses";
    private const string RegistrationStatusRoute = "/api/v1/reports/registration-statuses";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MonthlyRevenue_CalculatesCorrectSumAndGroupingByMonthUtc7()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();
            var customer = new KhachHang { HoTen = "Customer Test", CCCD = "123456789012", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var reg1 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            var reg2 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            var reg3 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            f.Db.AddRange(reg1, reg2, reg3);
            await f.Db.SaveChangesAsync();

            // Oct VN: 2026-10-15 10:00 VN = 2026-10-15 03:00 UTC
            f.Db.Add(new ThanhToan { MaDangKy = reg1.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 150_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc) });
            f.Db.Add(new ThanhToan { MaDangKy = reg2.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 250_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = new DateTime(2026, 10, 20, 5, 0, 0, DateTimeKind.Utc) });
            // Sep VN: 2026-09-10 10:00 VN = 2026-09-10 03:00 UTC
            f.Db.Add(new ThanhToan { MaDangKy = reg3.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 100_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = new DateTime(2026, 9, 10, 3, 0, 0, DateTimeKind.Utc) });
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{MonthlyRevenueRoute}?from=2026-09&to=2026-10");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());

            var report = await response.Content.ReadFromJsonAsync<MonthlyRevenueReportDto>();
            Assert.NotNull(report);
            Assert.True(report.IsSimulated);
            Assert.Equal(500_000m, report.TotalRevenue);
            Assert.Equal(2, report.Items.Count);

            var sep = report.Items.Single(x => x.Month == 9);
            Assert.Equal(2026, sep.Year);
            Assert.Equal("09/2026", sep.Label);
            Assert.Equal(100_000m, sep.Revenue);
            Assert.Equal(1, sep.TransactionCount);

            var oct = report.Items.Single(x => x.Month == 10);
            Assert.Equal(2026, oct.Year);
            Assert.Equal("10/2026", oct.Label);
            Assert.Equal(400_000m, oct.Revenue);
            Assert.Equal(2, oct.TransactionCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MonthlyRevenue_GuardsAgainstDoubleCountingWithMultipleItems()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();
            var customer = new KhachHang { HoTen = "Customer Test", CCCD = "123456789012", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var reg = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            f.Db.Add(reg);
            await f.Db.SaveChangesAsync();

            var vaccine = new VacXin
            {
                Code = "VX-SYNTHETIC",
                TenVacXin = "Vaccine Test",
                NhaSanXuat = "Demo",
                XuatXu = "VN",
                LieuLuong = "0.5ml",
                PhanUngSauTiem = "None",
                Gia = 100_000m,
                TrangThai = "DangSuDung"
            };
            f.Db.Add(vaccine);
            await f.Db.SaveChangesAsync();

            // Add 3 separate item details to the registration
            f.Db.Add(new ChiTietVacXinLe { MaDangKy = reg.MaDangKy, MaVacXin = vaccine.MaVacXin, SoLuong = 1, GiaTaiThoiDiemDangKy = 200_000m });
            f.Db.Add(new ChiTietVacXinLe { MaDangKy = reg.MaDangKy, MaVacXin = vaccine.MaVacXin, SoLuong = 1, GiaTaiThoiDiemDangKy = 300_000m });
            f.Db.Add(new ChiTietVacXinLe { MaDangKy = reg.MaDangKy, MaVacXin = vaccine.MaVacXin, SoLuong = 1, GiaTaiThoiDiemDangKy = 150_000m });

            // Exactly 1 payment of 650,000 VND
            f.Db.Add(new ThanhToan { MaDangKy = reg.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 650_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = new DateTime(2026, 10, 10, 5, 0, 0, DateTimeKind.Utc) });
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{MonthlyRevenueRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<MonthlyRevenueReportDto>();
            Assert.NotNull(report);
            // Must equal exactly 650,000 VND, NOT multiplied by detailsCount!
            Assert.Equal(650_000m, report.TotalRevenue);
            Assert.Single(report.Items);
            Assert.Equal(650_000m, report.Items[0].Revenue);
            Assert.Equal(1, report.Items[0].TransactionCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MonthlyRevenue_RespectsUtc7Boundary()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();
            var customer = new KhachHang { HoTen = "Customer Test", CCCD = "123456789012", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var reg = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            f.Db.Add(reg);
            await f.Db.SaveChangesAsync();

            // 2026-10-31 23:30:00 UTC corresponds to 2026-11-01 06:30:00 in UTC+07:00
            var boundaryUtcTime = new DateTime(2026, 10, 31, 23, 30, 0, DateTimeKind.Utc);
            f.Db.Add(new ThanhToan { MaDangKy = reg.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 300_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = boundaryUtcTime });
            await f.Db.SaveChangesAsync();

            // Query only October 2026 (VN time: 2026-10-01 to 2026-10-31)
            using var responseOct = await f.Client.GetAsync($"{MonthlyRevenueRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, responseOct.StatusCode);
            var reportOct = await responseOct.Content.ReadFromJsonAsync<MonthlyRevenueReportDto>();
            Assert.NotNull(reportOct);
            Assert.Equal(0m, reportOct.TotalRevenue);
            Assert.Empty(reportOct.Items);

            // Query November 2026 up to today (VN time: 2026-11-01 to 2026-11-02)
            using var responseNov = await f.Client.GetAsync($"{MonthlyRevenueRoute}?from=2026-11-01&to=2026-11-02");
            Assert.Equal(HttpStatusCode.OK, responseNov.StatusCode);
            var reportNov = await responseNov.Content.ReadFromJsonAsync<MonthlyRevenueReportDto>();
            Assert.NotNull(reportNov);
            Assert.Equal(300_000m, reportNov.TotalRevenue);
            Assert.Single(reportNov.Items);
            Assert.Equal(11, reportNov.Items[0].Month);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MonthlyRevenue_ExcludesNonSuccessPayments()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();
            var customer = new KhachHang { HoTen = "Customer Test", CCCD = "123456789012", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var reg1 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            var reg2 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            var reg3 = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" };
            f.Db.AddRange(reg1, reg2, reg3);
            await f.Db.SaveChangesAsync();

            var date = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc);
            f.Db.Add(new ThanhToan { MaDangKy = reg1.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 500_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = date });
            f.Db.Add(new ThanhToan { MaDangKy = reg2.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 999_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "DangXuLy", ThoiDiem = date });
            f.Db.Add(new ThanhToan { MaDangKy = reg3.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 888_000m, PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "DaHuy", ThoiDiem = date });
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{MonthlyRevenueRoute}?from=2026-10&to=2026-10");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<MonthlyRevenueReportDto>();
            Assert.NotNull(report);
            Assert.Equal(500_000m, report.TotalRevenue);
            Assert.Single(report.Items);
            Assert.Equal(1, report.Items[0].TransactionCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MonthlyRevenue_EnforcesAuthorization()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            // Anonymous -> 401
            using var unauthClient = f.Factory.CreateClient();
            using var respAnon = await unauthClient.GetAsync(MonthlyRevenueRoute);
            Assert.Equal(HttpStatusCode.Unauthorized, respAnon.StatusCode);

            // Staff without XemBaoCao -> 403
            var nurse = new NhanVien { HoTen = "[DEMO] Dieu Duong", ChucDanh = "DieuDuong", TrangThai = "Active" };
            f.Db.Add(nurse);
            await f.Db.SaveChangesAsync();
            var nurseAccount = new TaiKhoan
            {
                AccountType = "Employee",
                MaNhanVien = nurse.MaNhanVien,
                TenDangNhap = "nurse.test",
                NormalizedUsername = "NURSE.TEST",
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                TrangThai = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            };
            nurseAccount.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(nurseAccount, "Password@123456");
            f.Db.Add(nurseAccount);
            await f.Db.SaveChangesAsync();

            var session = new IdentitySession
            {
                Id = Guid.NewGuid(),
                AccountId = nurseAccount.MaTaiKhoan,
                SecurityVersion = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };
            f.Db.Add(session);
            await f.Db.SaveChangesAsync();

            var nurseClient = f.Factory.CreateClient();
            nurseClient.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(f.Config).Issue(nurseAccount.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            using var respNurse = await nurseClient.GetAsync(MonthlyRevenueRoute);
            Assert.Equal(HttpStatusCode.Forbidden, respNurse.StatusCode);

            // Admin with XemBaoCao -> 200
            await f.Login();
            using var respAdmin = await f.Client.GetAsync(MonthlyRevenueRoute);
            Assert.Equal(HttpStatusCode.OK, respAdmin.StatusCode);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("?from=invalid")]
    [InlineData("?to=2099-01-01")]
    [InlineData("?from=2026-11&to=2026-10")]
    public async Task MonthlyRevenue_ValidatesParameters(string query)
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            using var response = await f.Client.GetAsync(MonthlyRevenueRoute + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            Assert.Equal("validation", json.RootElement.GetProperty("code").GetString());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineDoses_CalculatesCorrectCountAndRanking()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();

            var vacA = new VacXin { Code="VX-SYNTHETIC", TenVacXin = "AstraZeneca", NhaSanXuat = "AZ", XuatXu = "UK", LieuLuong = "0.5ml", PhanUngSauTiem = "None", Gia = 100_000m, TrangThai = "DangSuDung" };
            var vacB = new VacXin { Code="VX-PFIZER", TenVacXin = "Pfizer", NhaSanXuat = "PF", XuatXu = "US", LieuLuong = "0.3ml", PhanUngSauTiem = "None", Gia = 200_000m, TrangThai = "DangSuDung" };
            f.Db.AddRange(vacA, vacB);
            await f.Db.SaveChangesAsync();

            var lotA = new LoVacXin { SoLo = "LOT-A-01", MaVacXin = vacA.MaVacXin, NgaySanXuat = new DateOnly(2025, 1, 1), HanSuDung = new DateOnly(2027, 1, 1), SoLuongBanDau = 100, SoLuongTon = 100, TrangThai = "DangSuDung" };
            var lotB = new LoVacXin { SoLo = "LOT-B-01", MaVacXin = vacB.MaVacXin, NgaySanXuat = new DateOnly(2025, 1, 1), HanSuDung = new DateOnly(2027, 1, 1), SoLuongBanDau = 100, SoLuongTon = 100, TrangThai = "DangSuDung" };
            f.Db.AddRange(lotA, lotB);
            await f.Db.SaveChangesAsync();

            var customer = new KhachHang { HoTen = "Customer Test", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var shot1 = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lotA.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = new DateTime(2026, 10, 10, 3, 0, 0, DateTimeKind.Utc), ViTriTiem = "Bắp tay trái", TrangThai = "DaTiem" };
            var shot2 = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lotA.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc), ViTriTiem = "Bắp tay phải", TrangThai = "DaTiem" };
            var shot3 = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lotA.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc), ViTriTiem = "Bắp tay trái", TrangThai = "DaTiem" };
            var shot4 = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lotB.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = new DateTime(2026, 10, 22, 3, 0, 0, DateTimeKind.Utc), ViTriTiem = "Bắp tay trái", TrangThai = "DaTiem" };
            f.Db.AddRange(shot1, shot2, shot3, shot4);
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{VaccineDosesRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<VaccineDosesReportDto>();
            Assert.NotNull(report);
            Assert.Equal(4, report.TotalDoses);
            Assert.Equal(2, report.Items.Count);

            Assert.Equal(vacA.MaVacXin, report.Items[0].VaccineId);
            Assert.Equal("AstraZeneca", report.Items[0].VaccineName);
            Assert.Equal(3, report.Items[0].DoseCount);

            Assert.Equal(vacB.MaVacXin, report.Items[1].VaccineId);
            Assert.Equal("Pfizer", report.Items[1].VaccineName);
            Assert.Equal(1, report.Items[1].DoseCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineDoses_DoesNotCountAppointmentsOrIndicationsWithoutShot()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();

            var vac = new VacXin { Code="VX-SYNTHETIC", TenVacXin = "Vaccine Test", NhaSanXuat = "AZ", XuatXu = "UK", LieuLuong = "0.5ml", PhanUngSauTiem = "None", Gia = 100_000m, TrangThai = "DangSuDung" };
            f.Db.Add(vac);
            await f.Db.SaveChangesAsync();

            var customer = new KhachHang { HoTen = "Customer Test", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var reg = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaDangKy" };
            f.Db.Add(reg);
            await f.Db.SaveChangesAsync();

            var item = new ChiTietVacXinLe { MaDangKy = reg.MaDangKy, MaVacXin = vac.MaVacXin, SoLuong = 1, GiaTaiThoiDiemDangKy = 100_000m };
            f.Db.Add(item);
            await f.Db.SaveChangesAsync();

            var appt = new LichHenTiem { MaDangKy = reg.MaDangKy, MaChiTietVacXin = item.MaChiTietVacXin, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, ThoiGianHen = DateTime.UtcNow, TrangThai = "DaXepLich" };
            f.Db.Add(appt);
            await f.Db.SaveChangesAsync();

            var phieu = new PhieuKhamSangLoc { MaDangKy = reg.MaDangKy, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, MaNhanVien = employee.MaNhanVien, MaLichHen = appt.MaLichHen, ThoiDiemKham = DateTime.UtcNow, KetLuan = "Đủ điều kiện" };
            f.Db.Add(phieu);
            await f.Db.SaveChangesAsync();

            var indication = new ChiDinhTiem { MaPhieuKham = phieu.MaPhieuKham, MaVacXin = vac.MaVacXin, KetQuaChiDinh = "DuocTiem", ThoiGian = DateTime.UtcNow };
            f.Db.Add(indication);
            await f.Db.SaveChangesAsync();

            // Notice: NO MuiTiem was created!
            using var response = await f.Client.GetAsync($"{VaccineDosesRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<VaccineDosesReportDto>();
            Assert.NotNull(report);
            Assert.Equal(0, report.TotalDoses);
            Assert.Empty(report.Items);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineDoses_RespectsUtc7Boundary()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();

            var vac = new VacXin { Code="VX-SYNTHETIC", TenVacXin = "Vaccine Boundary", NhaSanXuat = "AZ", XuatXu = "UK", LieuLuong = "0.5ml", PhanUngSauTiem = "None", Gia = 100_000m, TrangThai = "DangSuDung" };
            f.Db.Add(vac);
            await f.Db.SaveChangesAsync();

            var lot = new LoVacXin { SoLo = "LOT-BD-01", MaVacXin = vac.MaVacXin, NgaySanXuat = new DateOnly(2025, 1, 1), HanSuDung = new DateOnly(2027, 1, 1), SoLuongBanDau = 100, SoLuongTon = 100, TrangThai = "DangSuDung" };
            f.Db.Add(lot);
            await f.Db.SaveChangesAsync();

            var customer = new KhachHang { HoTen = "Customer Test", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            // 2026-10-31 23:30:00 UTC corresponds to 2026-11-01 06:30:00 in UTC+07:00
            var boundaryUtcTime = new DateTime(2026, 10, 31, 23, 30, 0, DateTimeKind.Utc);
            var shot = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lot.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = boundaryUtcTime, ViTriTiem = "Bắp tay trái", TrangThai = "DaTiem" };
            f.Db.Add(shot);
            await f.Db.SaveChangesAsync();

            // Query October 2026 (VN time: 2026-10-01 to 2026-10-31) -> 0 doses
            using var responseOct = await f.Client.GetAsync($"{VaccineDosesRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, responseOct.StatusCode);
            var reportOct = await responseOct.Content.ReadFromJsonAsync<VaccineDosesReportDto>();
            Assert.NotNull(reportOct);
            Assert.Equal(0, reportOct.TotalDoses);
            Assert.Empty(reportOct.Items);

            // Query November 2026 up to today (VN time: 2026-11-01 to 2026-11-02) -> 1 dose
            using var responseNov = await f.Client.GetAsync($"{VaccineDosesRoute}?from=2026-11-01&to=2026-11-02");
            Assert.Equal(HttpStatusCode.OK, responseNov.StatusCode);
            var reportNov = await responseNov.Content.ReadFromJsonAsync<VaccineDosesReportDto>();
            Assert.NotNull(reportNov);
            Assert.Equal(1, reportNov.TotalDoses);
            Assert.Single(reportNov.Items);
            Assert.Equal(vac.MaVacXin, reportNov.Items[0].VaccineId);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineDoses_ExcludesNonDaTiemShots()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var employee = await f.Db.Set<NhanVien>().FirstAsync();

            var vac = new VacXin { Code="VX-SYNTHETIC", TenVacXin = "Vaccine Test", NhaSanXuat = "AZ", XuatXu = "UK", LieuLuong = "0.5ml", PhanUngSauTiem = "None", Gia = 100_000m, TrangThai = "DangSuDung" };
            f.Db.Add(vac);
            await f.Db.SaveChangesAsync();

            var lot = new LoVacXin { SoLo = "LOT-NDT-01", MaVacXin = vac.MaVacXin, NgaySanXuat = new DateOnly(2025, 1, 1), HanSuDung = new DateOnly(2027, 1, 1), SoLuongBanDau = 100, SoLuongTon = 100, TrangThai = "DangSuDung" };
            f.Db.Add(lot);
            await f.Db.SaveChangesAsync();

            var customer = new KhachHang { HoTen = "Customer Test", SoDienThoai = "0901234567", Email = "test@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            var date = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc);
            var shotValid = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lot.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = date, ViTriTiem = "Bắp tay trái", TrangThai = "DaTiem" };
            var shotCanceled = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lot.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = date, ViTriTiem = "Bắp tay phải", TrangThai = "DaHuy" };
            var shotPending = new MuiTiem { MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, SoLo = lot.SoLo, MaNhanVien = employee.MaNhanVien, ThoiDiemTiem = date, ViTriTiem = "Bắp tay trái", TrangThai = "ChuaTiem" };
            f.Db.AddRange(shotValid, shotCanceled, shotPending);
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{VaccineDosesRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<VaccineDosesReportDto>();
            Assert.NotNull(report);
            Assert.Equal(1, report.TotalDoses);
            Assert.Single(report.Items);
            Assert.Equal(1, report.Items[0].DoseCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineDoses_EnforcesAuthorization()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            // Anonymous -> 401
            using var unauthClient = f.Factory.CreateClient();
            using var respAnon = await unauthClient.GetAsync(VaccineDosesRoute);
            Assert.Equal(HttpStatusCode.Unauthorized, respAnon.StatusCode);

            // Staff without XemBaoCao -> 403
            var nurse = new NhanVien { HoTen = "[DEMO] Dieu Duong", ChucDanh = "DieuDuong", TrangThai = "Active" };
            f.Db.Add(nurse);
            await f.Db.SaveChangesAsync();
            var nurseAccount = new TaiKhoan
            {
                AccountType = "Employee",
                MaNhanVien = nurse.MaNhanVien,
                TenDangNhap = "nurse.doses",
                NormalizedUsername = "NURSE.DOSES",
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                TrangThai = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            };
            nurseAccount.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(nurseAccount, "Password@123456");
            f.Db.Add(nurseAccount);
            await f.Db.SaveChangesAsync();

            var session = new IdentitySession
            {
                Id = Guid.NewGuid(),
                AccountId = nurseAccount.MaTaiKhoan,
                SecurityVersion = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };
            f.Db.Add(session);
            await f.Db.SaveChangesAsync();

            var nurseClient = f.Factory.CreateClient();
            nurseClient.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(f.Config).Issue(nurseAccount.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            using var respNurse = await nurseClient.GetAsync(VaccineDosesRoute);
            Assert.Equal(HttpStatusCode.Forbidden, respNurse.StatusCode);

            // Admin with XemBaoCao -> 200
            await f.Login();
            using var respAdmin = await f.Client.GetAsync(VaccineDosesRoute);
            Assert.Equal(HttpStatusCode.OK, respAdmin.StatusCode);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("?from=invalid")]
    [InlineData("?to=2099-01-01")]
    [InlineData("?from=2026-11&to=2026-10")]
    public async Task VaccineDoses_ValidatesParameters(string query)
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            using var response = await f.Client.GetAsync(VaccineDosesRoute + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            Assert.Equal("validation", json.RootElement.GetProperty("code").GetString());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RegistrationStatus_CalculatesCorrectCountsAndPercentages()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var customer = new KhachHang { HoTen = "Customer Status Test", CCCD = "123456789099", SoDienThoai = "0901234599", Email = "status@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Status Test", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            // Seed 5 DaDangKy, 3 DaThanhToan, 2 DaHuy on 2026-10-15 (03:00 UTC = 10:00 VN)
            var testTimeUtc = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 5; i++)
            {
                f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = testTimeUtc, TrangThai = "DaDangKy" });
            }
            for (var i = 0; i < 3; i++)
            {
                f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = testTimeUtc, TrangThai = "DaThanhToan" });
            }
            for (var i = 0; i < 2; i++)
            {
                f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = testTimeUtc, TrangThai = "DaHuy" });
            }
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{RegistrationStatusRoute}?from=2026-10-01&to=2026-10-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());

            var report = await response.Content.ReadFromJsonAsync<RegistrationStatusReportDto>();
            Assert.NotNull(report);
            Assert.Equal(new DateOnly(2026, 10, 1), report.From);
            Assert.Equal(new DateOnly(2026, 10, 31), report.To);
            Assert.Equal(10, report.TotalRegistrations);
            Assert.Equal(3, report.Items.Count);

            // Ordered descending: DaDangKy (5, 50.0%), DaThanhToan (3, 30.0%), DaHuy (2, 20.0%)
            Assert.Equal("DaDangKy", report.Items[0].Status);
            Assert.Equal(5, report.Items[0].RegistrationCount);
            Assert.Equal(50.0, report.Items[0].Percentage);

            Assert.Equal("DaThanhToan", report.Items[1].Status);
            Assert.Equal(3, report.Items[1].RegistrationCount);
            Assert.Equal(30.0, report.Items[1].Percentage);

            Assert.Equal("DaHuy", report.Items[2].Status);
            Assert.Equal(2, report.Items[2].RegistrationCount);
            Assert.Equal(20.0, report.Items[2].Percentage);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RegistrationStatus_DoesNotCreateArtificialStatuses()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var customer = new KhachHang { HoTen = "Customer Scope Guard", CCCD = "123456789098", SoDienThoai = "0901234598", Email = "scopeguard@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Scope Guard", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            // Insert ONLY 4 DaDangKy in this date range
            var testTimeUtc = new DateTime(2026, 9, 20, 4, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 4; i++)
            {
                f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = testTimeUtc, TrangThai = "DaDangKy" });
            }
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{RegistrationStatusRoute}?from=2026-09-01&to=2026-09-30");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<RegistrationStatusReportDto>();
            Assert.NotNull(report);
            Assert.Equal(4, report.TotalRegistrations);
            // Must ONLY contain the statuses actually present in DB, strictly 1 item
            Assert.Single(report.Items);
            Assert.Equal("DaDangKy", report.Items[0].Status);
            Assert.Equal(4, report.Items[0].RegistrationCount);
            Assert.Equal(100.0, report.Items[0].Percentage);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RegistrationStatus_RespectsUtc7Boundary()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var customer = new KhachHang { HoTen = "Customer Utc7", CCCD = "123456789097", SoDienThoai = "0901234597", Email = "utc7@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Utc7", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            // Registered at 2026-10-31 23:30 UTC -> 2026-11-01 06:30 VN
            var boundaryUtc = new DateTime(2026, 10, 31, 23, 30, 0, DateTimeKind.Utc);
            f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = boundaryUtc, TrangThai = "DaDangKy" });
            await f.Db.SaveChangesAsync();

            // Querying October 2026 should NOT include this registration
            using var respOct = await f.Client.GetAsync($"{RegistrationStatusRoute}?from=2026-10-01&to=2026-10-31");
            var reportOct = await respOct.Content.ReadFromJsonAsync<RegistrationStatusReportDto>();
            Assert.NotNull(reportOct);
            Assert.Equal(0, reportOct.TotalRegistrations);

            // Querying November 2026 MUST include this registration
            using var respNov = await f.Client.GetAsync($"{RegistrationStatusRoute}?from=2026-11-01&to=2026-11-01");
            var reportNov = await respNov.Content.ReadFromJsonAsync<RegistrationStatusReportDto>();
            Assert.NotNull(reportNov);
            Assert.Equal(1, reportNov.TotalRegistrations);
            Assert.Equal("DaDangKy", reportNov.Items[0].Status);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RegistrationStatus_FiltersByDateRange()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            var customer = new KhachHang { HoTen = "Customer Range", CCCD = "123456789096", SoDienThoai = "0901234596", Email = "range@example.invalid" };
            f.Db.Add(customer);
            await f.Db.SaveChangesAsync();

            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "Recipient Range", NgaySinh = new DateOnly(2020, 1, 1), GioiTinh = "Nam", QuanHeVoiKhachHang = "Con" };
            f.Db.Add(recipient);
            await f.Db.SaveChangesAsync();

            // In range: 2026-08-15
            f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = new DateTime(2026, 8, 15, 3, 0, 0, DateTimeKind.Utc), TrangThai = "DaDangKy" });
            // Out of range: 2026-07-10 (before), 2026-09-05 (after)
            f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = new DateTime(2026, 7, 10, 3, 0, 0, DateTimeKind.Utc), TrangThai = "DaDangKy" });
            f.Db.Add(new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = new DateTime(2026, 9, 5, 3, 0, 0, DateTimeKind.Utc), TrangThai = "DaDangKy" });
            await f.Db.SaveChangesAsync();

            using var response = await f.Client.GetAsync($"{RegistrationStatusRoute}?from=2026-08-01&to=2026-08-31");
            var report = await response.Content.ReadFromJsonAsync<RegistrationStatusReportDto>();
            Assert.NotNull(report);
            Assert.Equal(1, report.TotalRegistrations);
            Assert.Equal(1, report.Items[0].RegistrationCount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RegistrationStatus_EnforcesAuthorization()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            // Anonymous -> 401
            using var respAnon = await f.Factory.CreateClient().GetAsync(RegistrationStatusRoute);
            Assert.Equal(HttpStatusCode.Unauthorized, respAnon.StatusCode);

            // Staff without XemBaoCao -> 403
            var nurse = new NhanVien { HoTen = "[DEMO] Dieu Duong Status", ChucDanh = "DieuDuong", TrangThai = "Active" };
            f.Db.Add(nurse);
            await f.Db.SaveChangesAsync();
            var nurseAccount = new TaiKhoan
            {
                AccountType = "Employee",
                MaNhanVien = nurse.MaNhanVien,
                TenDangNhap = "nurse.status",
                NormalizedUsername = "NURSE.STATUS",
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                TrangThai = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            };
            nurseAccount.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(nurseAccount, "Password@123456");
            f.Db.Add(nurseAccount);
            await f.Db.SaveChangesAsync();

            var session = new IdentitySession
            {
                Id = Guid.NewGuid(),
                AccountId = nurseAccount.MaTaiKhoan,
                SecurityVersion = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };
            f.Db.Add(session);
            await f.Db.SaveChangesAsync();

            var nurseClient = f.Factory.CreateClient();
            nurseClient.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(f.Config).Issue(nurseAccount.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            using var respNurse = await nurseClient.GetAsync(RegistrationStatusRoute);
            Assert.Equal(HttpStatusCode.Forbidden, respNurse.StatusCode);

            // Admin with XemBaoCao -> 200
            await f.Login();
            using var respAdmin = await f.Client.GetAsync(RegistrationStatusRoute);
            Assert.Equal(HttpStatusCode.OK, respAdmin.StatusCode);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("?from=invalid")]
    [InlineData("?to=2099-01-01")]
    [InlineData("?from=2026-11&to=2026-10")]
    public async Task RegistrationStatus_ValidatesParameters(string query)
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            await f.Login();
            using var response = await f.Client.GetAsync(RegistrationStatusRoute + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            Assert.Equal("validation", json.RootElement.GetProperty("code").GetString());
        });
    }
}
