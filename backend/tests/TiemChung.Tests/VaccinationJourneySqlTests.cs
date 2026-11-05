using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class VaccinationJourneySqlTests
{
    private const string Route = "/api/v1/vaccination-journeys";

    [Fact, Trait("Category", "SqlIntegration")]
    public Task OwnershipRequiresBothLinksAndDetailDoesNotRevealExistence() =>
        SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, async f => {
            var registration = await f.Register();
            var path = $"{Route}/{registration.Id}";
            using var owner = await f.Base.Actor(null, false);
            var accountId = f.Base.LastAccountId;
            using var other = await f.Base.Actor(null, false);
            var customer = new AccountProfileLink { AccountId = accountId, CustomerId = f.Base.CustomerId, State = "Active" };
            var recipient = new AccountProfileLink { AccountId = accountId, RecipientId = f.Base.RecipientId, State = "Active" };
            foreach (var link in new[] { customer, recipient })
            {
                using var denied = await owner.GetAsync(path);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.Empty((await owner.GetFromJsonAsync<VaccinationJourneyListResponse>(Route))!.Items);
                f.Db.Add(link);
                await f.Db.SaveChangesAsync();
            }
            var own = await Read(owner, registration.Id);
            Assert.Equal(registration.Id, own.RegistrationId);
            Assert.Equal(registration.TotalBeforeDiscount, own.Payment.TotalDue);
            Assert.Equal(registration.Items.Select(i => i.Id), own.Payment.Items.Select(i => i.Id));
            Assert.Empty(own.Appointments);
            Assert.Empty(own.Shots);
            Assert.Equal("ChuaThanhToan", own.Payment.PaymentState);
            Assert.Equal(registration.Id, Assert.Single((await owner.GetFromJsonAsync<VaccinationJourneyListResponse>(Route))!.Items).RegistrationId);
            foreach (var id in new[] { registration.Id, int.MaxValue })
            {
                using var denied = await other.GetAsync($"{Route}/{id}");
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.DoesNotContain("totalDue", await denied.Content.ReadAsStringAsync());
            }
            foreach (var link in new[] { customer, recipient })
            {
                link.State = "Revoked";
                await f.Db.SaveChangesAsync();
                using var denied = await owner.GetAsync(path);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                link.State = "Active";
                await f.Db.SaveChangesAsync();
            }
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task ProjectsOnlyRegistrationSourcesAndReloadsPaymentAppointmentAndShots() =>
        SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, async f => {
            using var owner = await f.Base.Actor(null, false);
            var accountId = f.Base.LastAccountId;
            f.Db.AddRange(new AccountProfileLink { AccountId = accountId, CustomerId = f.Base.CustomerId, State = "Active" },
                new AccountProfileLink { AccountId = accountId, RecipientId = f.Base.RecipientId, State = "Active" });
            await f.Db.SaveChangesAsync();
            var source = await f.Source("package", [f.A, f.B]);
            var unrelated = await f.Register();
            var id = source.Registration.Id;
            var before = await Read(owner, id);
            Assert.Equal("ThanhCong", before.Payment.PaymentState);
            Assert.Null(before.NextAppointment);
            Assert.Empty(before.Shots);
            Assert.Equal(3, before.Progress.Single(d => d.VaccineId == f.A).RegisteredDoses);
            Assert.Equal(1, before.Progress.Single(d => d.VaccineId == f.B).RegisteredDoses);
            var nearRequest = f.Base.Request(source.Registration, source.Registration.Items.Single(i => i.Kind == "vaccine"))
                with { ScheduledAt = DateTimeOffset.UtcNow.AddDays(1).ToString("O") };
            using var nearResponse = await f.Base.Reception.PostAsJsonAsync("/api/v1/appointments", nearRequest);
            Assert.Equal(HttpStatusCode.Created, nearResponse.StatusCode);
            var near = (await nearResponse.Content.ReadFromJsonAsync<AppointmentDto>())!;
            Assert.Equal(near.Id, (await Read(owner, id)).NextAppointment!.Id);
            var legacy = new LichHenTiem { MaNguoiDuocTiem = f.Base.RecipientId,
                ThoiGianHen = DateTime.UtcNow.AddHours(1), DiemTiem = "Lịch cũ", TrangThai = "DaXepLich" };
            f.Db.Add(legacy);
            await f.Db.SaveChangesAsync();
            Assert.DoesNotContain((await Read(owner, id)).Appointments, a => a.Id == legacy.MaLichHen);
            foreach (var (vaccine, lot) in new[] { (f.A, "[DEMO] S5-VALID-A"), (f.B, "[DEMO] S55-VALID-B") })
            {
                using var recorded = await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations",
                    new RecordVaccinationRequest(source.Assessment.Indications.Single(i => i.VaccineId == vaccine).Id, lot, "Bắp tay trái"));
                Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
            }
            var after = await Read(owner, id);
            Assert.Equal(new[] { f.B, f.A }, after.Shots.Select(s => s.Vaccine.Id));
            Assert.Equal(2, after.ShotCount);
            Assert.Equal(1, after.Progress.Single(d => d.VaccineId == f.A).AdministeredDoses);
            Assert.Equal(2, after.Progress.Single(d => d.VaccineId == f.A).RemainingDoses);
            Assert.Equal(0, after.Progress.Single(d => d.VaccineId == f.B).RemainingDoses);
            Assert.Empty((await Read(owner, unrelated.Id)).Shots);
            await f.Db.Set<MuiTiem>().Where(s => s.MaMuiTiem == after.Shots[0].Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.MaChiDinh, (int?)null));
            Assert.Equal(f.A, Assert.Single((await Read(owner, id)).Shots).Vaccine.Id);
            await f.Db.Set<LichHenTiem>().Where(a => a.MaLichHen == source.Appointment.Id || a.MaLichHen == near.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ThoiGianHen, DateTime.UtcNow.AddDays(-1)));
            Assert.Null((await Read(owner, id)).NextAppointment);
            var free = await f.Register();
            await f.Db.Registrations.Where(r => r.MaDangKy == free.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.PhiKhamTuVan, 0));
            await f.Db.RegistrationVaccineItems.Where(r => r.MaDangKy == free.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.GiaTaiThoiDiemDangKy, 0));
            await f.Db.RegistrationPackageItems.Where(r => r.MaDangKy == free.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.GiaTaiThoiDiemDangKy, 0));
            var zero = (await Read(owner, free.Id)).Payment;
            Assert.Equal(0m, zero.TotalDue);
            Assert.Null(zero.Payment);
            var paymentPath = $"/api/v1/registrations/{unrelated.Id}/payment";
            using var center = await owner.PutAsJsonAsync(paymentPath + "/channel", new { channel = "TaiTrungTam" });
            Assert.Equal(HttpStatusCode.OK, center.StatusCode);
            Assert.Equal("TaiTrungTam", (await Read(owner, unrelated.Id)).Payment.ExpectedChannel);
            using var pending = await owner.PostAsJsonAsync(paymentPath + "/online", PaymentSqlTests.Online("ChuyenKhoanOnline"));
            Assert.Equal(HttpStatusCode.Created, pending.StatusCode);
            var payment = (await Read(owner, unrelated.Id)).Payment;
            Assert.Equal("ChoXacNhan", payment.PaymentState);
            Assert.Equal(0m, payment.PaidAmount);
            await f.Db.Payments.Where(p => p.MaDangKy == unrelated.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SoTien, p => p.SoTien + 1));
            using var conflict = await owner.GetAsync($"{Route}/{unrelated.Id}");
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        });

    private static async Task<VaccinationJourneyDto> Read(HttpClient client, int id)
    {
        using var response = await client.GetAsync($"{Route}/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        return (await response.Content.ReadFromJsonAsync<VaccinationJourneyDto>())!;
    }
}
