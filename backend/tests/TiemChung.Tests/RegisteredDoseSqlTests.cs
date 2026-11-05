using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;
using Fixture = TiemChung.Tests.VaccinationSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class RegisteredDoseSqlTests
{
    private static async Task Problem(HttpResponseMessage response, string code, HttpStatusCode status = HttpStatusCode.Conflict)
    {
        using (response) {
            Assert.Equal(status, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        }
    }
    private static async Task<RegistrationDto> Register(Fixture f, string kind, int quantity)
    {
        var components = await f.Db.Set<GoiVacXin_VacXin>().Where(c => c.MaGoi == f.PackageId)
            .Select(c => new RegistrationComponentRequest(c.MaVacXin, c.SoLuong)).ToArrayAsync();
        var request = RegistrationSqlTests.SyntheticRequest(f.Base.CustomerId, f.Base.RecipientId,
            kind == "vaccine" ? [new(kind, f.A, quantity, 150000)] :
                [new(kind, f.PackageId, quantity, 350000, components)]);
        using var response = await f.Base.Reception.PostAsJsonAsync("/api/v1/registrations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var registration = (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
        using var paid = await f.Cashier.PostAsJsonAsync($"/api/v1/registrations/{registration.Id}/payment", PaymentSqlTests.Cash(registration.TotalDue));
        Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
        return registration;
    }
    private static async Task<AppointmentDto> Appointment(Fixture f, RegistrationDto r, bool future = false)
    {
        using var response = await f.Base.Reception.PostAsJsonAsync("/api/v1/appointments", f.Base.Request(r, r.Items[0]) with {
            ScheduledAt = DateTimeOffset.UtcNow.AddHours(1).ToString("O")
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var appointment = (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
        if (!future) await f.Db.Set<LichHenTiem>().Where(a => a.MaLichHen == appointment.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ThoiGianHen, DateTime.UtcNow.AddMinutes(-1).AddTicks(appointment.Id)));
        return appointment;
    }
    private static string Clinical(int id) => $"/api/v1/clinical/appointments/{id}/assessment";
    private static async Task<int> Indication(Fixture f, AppointmentDto a, int vaccine)
    {
        using var response = await f.Doctor.PostAsJsonAsync(Clinical(a.Id), new CreateClinicalAssessmentRequest("Đủ điều kiện", null, [vaccine]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClinicalAssessmentDto>())!.Indications.Single().Id;
    }
    private static Task<HttpResponseMessage> Shot(Fixture f, int id, HttpClient? client = null, int? vaccine = null) =>
        (client ?? f.Nurse).PostAsJsonAsync("/api/v1/vaccinations", new RecordVaccinationRequest(id,
            vaccine == f.B ? "[DEMO] S55-VALID-B" : "[DEMO] S5-VALID-A", "Bắp tay trái"));

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("vaccine", 1)][InlineData("package", 2)]
    public Task EnforcesRegisteredQuantityAcrossAppointmentsAndKeepsVaccinesAndRegistrationsIndependent(string kind, int quantity) =>
        SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            if (kind == "package") await f.Db.Set<GoiVacXin_VacXin>().Where(c => c.MaGoi == f.PackageId && c.MaVacXin == f.A)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.SoLuong, 2));
            var r = await Register(f, kind, quantity);
            var quota = kind == "package" ? quantity * 2 : quantity;
            var ids = new List<int>();
            for (var n = 0; n <= quota; n++) ids.Add(await Indication(f, await Appointment(f, r), f.A));
            foreach (var id in ids.Take(quota)) {
                using var response = await Shot(f, id); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
            var before = await f.Snapshot();
            await Problem(await Shot(f, ids.Last()), "doseEntitlementExhausted");
            Assert.Equal(before, await f.Snapshot());
            var blocked = await f.Nurse.GetFromJsonAsync<VaccinationDetailDto>($"/api/v1/vaccinations/indications/{ids.Last()}");
            Assert.Equal("doseEntitlementExhausted", blocked!.BlockReason); Assert.Empty(blocked.Lots);
            var next = await Appointment(f, r);
            await Problem(await f.Doctor.PostAsJsonAsync(Clinical(next.Id), new CreateClinicalAssessmentRequest("Đủ điều kiện", null, [f.A])), "doseEntitlementExhausted");
            var clinical = (await f.Doctor.GetFromJsonAsync<ClinicalAppointmentDto>($"/api/v1/clinical/appointments/{next.Id}"))!;
            Assert.False(clinical.AllowedVaccines.Single(v => v.Id == f.A).CanIndicate);
            if (kind == "package") {
                Assert.Null(clinical.BlockReason); Assert.True(clinical.AllowedVaccines.Single(v => v.Id == f.B).CanIndicate);
                var b = await Indication(f, next, f.B); using var response = await Shot(f, b, vaccine: f.B);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            } else Assert.Equal("doseEntitlementExhausted", clinical.BlockReason);
            var other = await Register(f, "vaccine", 1);
            using var independent = await Shot(f, await Indication(f, await Appointment(f, other), f.A));
            Assert.Equal(HttpStatusCode.Created, independent.StatusCode);
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task ConcurrentLastDoseAndRollbackUseOnlyCommittedShots() =>
        SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            var r = await Register(f, "vaccine", 1);
            var first = await Indication(f, await Appointment(f, r), f.A);
            var second = await Indication(f, await Appointment(f, r), f.A);
            var before = await f.Snapshot();
            await f.Db.Database.ExecuteSqlRawAsync("ALTER TABLE SecurityAudit ADD CONSTRAINT RF05BFailure CHECK (Action <> N'vaccinations.recorded')");
            await Problem(await Shot(f, first), "dependencyUnavailable", HttpStatusCode.ServiceUnavailable);
            Assert.Equal(before, await f.Snapshot());
            await f.Db.Database.ExecuteSqlRawAsync("ALTER TABLE SecurityAudit DROP CONSTRAINT RF05BFailure");
            var responses = await Task.WhenAll(Shot(f, first), Shot(f, second, f.OtherNurse));
            try {
                Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
                await Problem(Assert.Single(responses, r => r.StatusCode != HttpStatusCode.Created), "doseEntitlementExhausted");
            } finally { foreach (var response in responses) response.Dispose(); }
            Assert.Single(await f.Db.Set<MuiTiem>().ToArrayAsync());
            Assert.Single(await f.Db.Set<BienDongKho>().ToArrayAsync());
            Assert.Equal(99, (await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(l => l.SoLo == "[DEMO] S5-VALID-A")).SoLuongTon);
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "vaccinations.recorded"));
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task FutureAppointmentBlocksEveryConclusionAndExistingIndicationUntilItsExactTime() =>
        SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            var r = await Register(f, "vaccine", 1); var a = await Appointment(f, r, future: true);
            var before = await f.Snapshot();
            foreach (var conclusion in new[] { "Đủ điều kiện", "Tạm hoãn", "Chống chỉ định" })
                await Problem(await f.Doctor.PostAsJsonAsync(Clinical(a.Id), new CreateClinicalAssessmentRequest(conclusion,
                    conclusion == "Đủ điều kiện" ? null : "Cần đánh giá lại", conclusion == "Đủ điều kiện" ? [f.A] : [])), "appointmentNotStarted");
            Assert.Equal(before, await f.Snapshot());
            Assert.Equal("appointmentNotStarted", (await f.Doctor.GetFromJsonAsync<ClinicalAppointmentDto>($"/api/v1/clinical/appointments/{a.Id}"))!.BlockReason);
            await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, DateTime.UtcNow.AddSeconds(-1)));
            var indication = await Indication(f, a, f.A);
            await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, DateTime.UtcNow.AddMinutes(10)));
            before = await f.Snapshot();
            await Problem(await Shot(f, indication), "appointmentNotStarted"); Assert.Equal(before, await f.Snapshot());
            Assert.Equal("appointmentNotStarted", (await f.Nurse.GetFromJsonAsync<VaccinationDetailDto>($"/api/v1/vaccinations/indications/{indication}"))!.BlockReason);
            await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, DateTime.UtcNow.AddSeconds(-1)));
            using var valid = await Shot(f, indication); Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task JourneyReportsCertainProgressAndOnlyConfirmedNextAppointmentWithLegacyFailClosed() =>
        SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            using var customer = await f.Base.Actor(null, false); var account = f.Base.LastAccountId;
            f.Db.AddRange(new AccountProfileLink { AccountId = account, CustomerId = f.Base.CustomerId, State = "Active" },
                new AccountProfileLink { AccountId = account, RecipientId = f.Base.RecipientId, State = "Active" }); await f.Db.SaveChangesAsync();
            var r = await Register(f, "package", 2); var a = await Appointment(f, r);
            var indication = await Indication(f, a, f.A);
            using var shot = await Shot(f, indication); Assert.Equal(HttpStatusCode.Created, shot.StatusCode);
            var future = await Appointment(f, r, future: true);
            async Task<VaccinationJourneyDto> Read() => (await customer.GetFromJsonAsync<VaccinationJourneyDto>($"/api/v1/vaccination-journeys/{r.Id}"))!;
            var journey = await Read();
            var progress = journey.Progress.Single(d => d.VaccineId == f.A);
            Assert.Equal(2, progress.RegisteredDoses); Assert.Equal(1, progress.AdministeredDoses); Assert.Equal(1, progress.RemainingDoses);
            Assert.Equal(future.Id, progress.NextAppointment!.Id); Assert.Equal(future.Id, journey.NextAppointment!.Id);
            Assert.Equal(0, journey.Progress.Single(d => d.VaccineId == f.B).AdministeredDoses);
            await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == future.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Legacy"));
            journey = await Read(); Assert.Null(journey.NextAppointment); Assert.All(journey.Progress, d => Assert.Null(d.NextAppointment));
            // Legacy component quantities outside the current contract are not reliable entitlement.
            var next = await Appointment(f, r);
            var pending = await Indication(f, next, f.A);
            var unassessed = await Appointment(f, r);
            await f.Db.Set<GoiVacXin_VacXin>().Where(x => x.MaGoi == f.PackageId && x.MaVacXin == f.A)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, 101));
            journey = await Read();
            Assert.All(journey.Progress, d => { Assert.Null(d.RegisteredDoses); Assert.Null(d.RemainingDoses); Assert.Null(d.NextAppointment); });
            var before = await f.Snapshot();
            await Problem(await Shot(f, pending), "doseEntitlementUnavailable");
            Assert.Equal(before, await f.Snapshot());
            await Problem(await f.Doctor.PostAsJsonAsync(Clinical(unassessed.Id), new CreateClinicalAssessmentRequest("Đủ điều kiện", null, [f.A])), "doseEntitlementUnavailable");
        });
}
