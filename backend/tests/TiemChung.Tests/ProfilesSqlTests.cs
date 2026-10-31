using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;
namespace TiemChung.Tests;
public sealed class ProfilesSqlTests
{
    [Fact, Trait("Category","SqlIntegration")]
    public async Task CustomerOwnCrudMultipleChildrenScopeValidationAndReferencedDelete()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        var client=f.Client;
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/v1/customers",new {})).StatusCode);
        var password=Guid.NewGuid().ToString("N");
        var accounts=new List<TaiKhoan>();
        for(var i=0;i<2;i++) {
            var account=new TaiKhoan { AccountType="Customer",LoginEmail=$"s3-{i}@example.invalid",NormalizedEmail=$"S3-{i}@EXAMPLE.INVALID",TenDangNhap=$"s3-{i}",TrangThai="Active",EmailVerifiedAt=DateTimeOffset.UtcNow,CreatedAt=DateTimeOffset.UtcNow };
            account.MatKhauHash=new Microsoft.AspNetCore.Identity.PasswordHasher<TaiKhoan>().HashPassword(account,password);
            f.Db.Add(account); accounts.Add(account);
        }
        await f.Db.SaveChangesAsync();
        async Task Login(int i) {
            using var response=await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier=accounts[i].LoginEmail,password,clientType="native"});
            response.EnsureSuccessStatusCode();
            var token=(await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        }
        var customerBody=new CreateCustomerRequest("Synthetic S3",null,"0900000000","s3@example.invalid");
        await Login(0);
        var created=await client.PostAsJsonAsync("/api/v1/customers",customerBody); Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        var own=(await created.Content.ReadFromJsonAsync<CustomerDto>())!; Assert.Null(own.IdentityNumber);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/v1/customers",customerBody)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/v1/customers/{own.Id}",customerBody with { FullName="Updated" })).StatusCode);
        foreach(var bad in new[]{customerBody with { FullName="" },customerBody with { IdentityNumber="１２３４５６７８９０１２" },customerBody with { PhoneNumber="abc" },customerBody with { Email="invalid" }})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/v1/customers/{own.Id}",bad)).StatusCode);
        var children=new List<RecipientDto>();
        for(var i=0;i<2;i++) {
            var response=await client.PostAsJsonAsync("/api/v1/recipients",new CreateRecipientRequest(own.Id,"Child "+i,null,new(2020,1,1),"Khác","Con"));
            Assert.Equal(HttpStatusCode.Created,response.StatusCode); children.Add((await response.Content.ReadFromJsonAsync<RecipientDto>())!);
        }
        Assert.Equal(2,(await client.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items.Count);
        var update=new UpdateRecipientRequest("Updated child",null,new(2021,2,3),"Nữ","Con");
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/v1/recipients/{children[0].Id}",update)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/v1/recipients/{children[0].Id}",update with { DateOfBirth=new(2999,1,1) })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/v1/customers/{own.Id}")).StatusCode);
        Assert.True(await f.Db.Set<AccountProfileLink>().AnyAsync(l=>l.CustomerId==own.Id && l.State=="Active"));
        await Login(1);
        Assert.Empty((await client.GetFromJsonAsync<CustomerListResponse>("/api/v1/customers"))!.Items);
        Assert.Empty((await client.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/v1/customers/{own.Id}",customerBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/v1/customers/{own.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/v1/recipients/{children[0].Id}",update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/v1/recipients/{children[0].Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/recipients",new CreateRecipientRequest(own.Id,"Other child",null,new(2020,1,1),"Nam","Con"))).StatusCode);
        var otherResponse=await client.PostAsJsonAsync("/api/v1/customers",customerBody); Assert.Equal(HttpStatusCode.Created,otherResponse.StatusCode);
        var other=(await otherResponse.Content.ReadFromJsonAsync<CustomerDto>())!;
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/v1/customers/{other.Id}",customerBody with { IdentityNumber="000000000123" })).StatusCode);
        await Login(0);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync($"/api/v1/customers/{own.Id}",customerBody with { IdentityNumber="000000000123" })).StatusCode);
        Assert.Equal("Updated",(await f.Db.Customers.AsNoTracking().SingleAsync(c=>c.MaKhachHang==own.Id)).HoTen);
        Assert.Equal("Updated child",(await f.Db.Recipients.AsNoTracking().SingleAsync(r=>r.MaNguoiDuocTiem==children[0].Id)).HoTen);
        var recipient=children[0].Id;
        f.Db.Add(new LichHenTiem { MaNguoiDuocTiem=recipient,ThoiGianHen=DateTime.UtcNow,TrangThai="Planned" }); await f.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/v1/recipients/{recipient}")).StatusCode);
        Assert.True(await f.Db.Set<AccountProfileLink>().AnyAsync(l=>l.RecipientId==recipient && l.State=="Active"));
        await f.Db.Set<LichHenTiem>().Where(x=>x.MaNguoiDuocTiem==recipient).ExecuteDeleteAsync();
        foreach(var child in children) Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/v1/recipients/{child.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/v1/customers/{own.Id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<CustomerListResponse>("/api/v1/customers"))!.Items);
        Assert.All(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="profiles.updated" || x.Action=="profiles.deleted").ToArrayAsync(),a=>Assert.NotNull(a.ActorAccountId));
        });
    }

    [Fact, Trait("Category","SqlIntegration")]
    public async Task WindowsCommandsPersistCustomersAndChildrenAndShowApiDenial()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        await f.Db.Set<NhanVien>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ChucDanh,"LeTan"));
        using var http=f.Factory.CreateClient(); var identity=new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
        var vm=new ProfilesViewModel(new ProfilesApiService(identity), identity.CanWriteProfiles); await vm.LoadAsync(); Assert.Empty(vm.Customers);
        async Task Command(RelayCommand command) {
            Assert.True(command.CanExecute(null)); command.Execute(null);
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while(!vm.CanInteract) await Task.Delay(10,timeout.Token);
        }
        vm.Name="Windows S3"; vm.Phone="0900000000"; vm.Email="windows-s3@example.invalid";
        await Command(vm.LuuKhachHangCommand); Assert.Contains("Đã lưu",vm.Feedback);
        var id=Assert.Single(vm.Customers).Id; vm.Name="Windows updated"; await Command(vm.LuuKhachHangCommand);
        vm.RecipientName="Child"; vm.BirthDate=new(2020,1,1); vm.Gender="Nam"; vm.Relation="Con";
        await Command(vm.ThemNguoiDuocTiemCommand); Assert.Contains("Đã lưu",vm.Feedback);
        vm.RecipientName="Child updated"; await Command(vm.ThemNguoiDuocTiemCommand);
        await vm.LoadAsync(); Assert.Equal("Windows updated",Assert.Single(vm.Customers).Name); Assert.Equal("Child updated",Assert.Single(vm.Recipients).Name);
        Assert.Null((await f.Db.Customers.AsNoTracking().SingleAsync(x=>x.MaKhachHang==id)).CCCD);
        vm.SelectedCustomer=vm.Customers.Single(); await Command(vm.DeleteCustomerCommand); Assert.Contains("đang được sử dụng",vm.Feedback);
        vm.SelectedRecipient=vm.Recipients.Single(); await Command(vm.DeleteRecipientCommand); Assert.Empty(vm.Recipients);
        await Command(vm.DeleteCustomerCommand); Assert.Empty(vm.Customers);
        foreach (var name in new[] { "[DEMO] consecutive first", "[DEMO] consecutive second" }) {
            await Command(vm.TaoHoSoMoiCommand);
            Assert.Null(vm.SelectedCustomer); Assert.Null(vm.SelectedRecipient); Assert.Equal("", vm.Name);
            vm.Name=name; vm.Phone="0900000000"; vm.Email="next@example.invalid";
            await Command(vm.SaveAndNewCustomerCommand);
            Assert.Contains("Đã lưu khách hàng",vm.Feedback);
            Assert.Null(vm.SelectedCustomer); Assert.Equal("",vm.Name);
        }
        Assert.Equal(2,await f.Db.Customers.CountAsync());
        await vm.LoadAsync(); Assert.Equal(2,vm.Customers.Count);
        vm.SelectedCustomer=vm.Customers.First(); Assert.False(vm.SaveAndNewCustomerCommand.CanExecute(null));
        await identity.LogoutAsync(); vm.Name="Denied"; vm.Phone="0900000000"; vm.Email="windows-s3@example.invalid"; await Command(vm.LuuKhachHangCommand); Assert.DoesNotContain("Đã lưu",vm.Feedback); Assert.Equal("Denied",vm.Name);
        });
    }
}
