namespace TiemChung.Infrastructure.Data;

// AUTHZ-R1: employee titles are the runtime authority. Legacy grants are retained only for compatibility.
public static class EmployeeAuthorization
{
    public const string ProfilesWrite = "profiles.write";
    public const string Registrations = "registrations.manage";
    public const string Screening = "screening.manage";
    public const string Vaccination = "vaccinations.record";
    public const string Aftercare = "aftercare.record";
    private static readonly IReadOnlyDictionary<string, string[]> Matrix = new Dictionary<string, string[]>(StringComparer.Ordinal) {
        ["Admin"] = ["XemTongQuan", "QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia", "QuanLyKhuyenMai", "XemBaoCao", "QuanLyTaiKhoan"],
        ["LeTan"] = ["XemTongQuan", "XemHoSo", ProfilesWrite, Registrations, "QuanLyLichHen", "GuiNhacLich"],
        ["BacSi"] = ["XemTongQuan", "XemHoSo", Screening],
        ["DieuDuong"] = ["XemTongQuan", "XemHoSo", Vaccination, Aftercare],
        ["ThuNgan"] = ["XemTongQuan", "XemThanhToan", "GhiThanhToan"],
        ["ThuKho"] = ["XemTongQuan", "XemKho", IdentityPermissions.InventoryReceive]
    };
    public static IReadOnlyList<string> Roles => Matrix.Keys.ToArray();
    public static bool IsRole(string? role) => role is not null && Matrix.ContainsKey(role);
    public static string[] PermissionsFor(string? role, string? duty = null) =>
        role is not null && Matrix.TryGetValue(role, out var codes)
            ? codes.Where(code => duty != "Director" || code != "QuanLyTaiKhoan").ToArray() : [];
    public static EmployeeCapabilities CapabilitiesFor(string? role, string? duty = null)
    {
        var codes = PermissionsFor(role, duty);
        bool Has(string code) => codes.Contains(code);
        return new() {
            Overview = Has("XemTongQuan"), ChangePassword = IsRole(role),
            ProfilesRead = Has("XemHoSo"), ProfilesCreate = Has(ProfilesWrite), ProfilesUpdate = Has(ProfilesWrite),
            Registrations = Has(Registrations), Screening = Has(Screening), Vaccinations = Has(Vaccination), Aftercare = Has(Aftercare),
            PaymentsRead = Has("XemThanhToan"), PaymentsWrite = Has("GhiThanhToan"),
            InventoryRead = Has("XemKho"), InventoryReceive = Has(IdentityPermissions.InventoryReceive),
            Appointments = Has("QuanLyLichHen"), Catalog = Has("QuanLyDanhMucChuyenMon"),
            Promotions = Has("QuanLyKhuyenMai"), Reports = Has("XemBaoCao"), Accounts = Has("QuanLyTaiKhoan")
        };
    }
}

public sealed record EmployeeCapabilities
{
    public bool Overview { get; init; }
    public bool ChangePassword { get; init; }
    public bool ProfilesRead { get; init; }
    public bool ProfilesCreate { get; init; }
    public bool ProfilesUpdate { get; init; }
    public bool SelfClaim { get; init; }
    public bool Representatives { get; init; }
    public bool Registrations { get; init; }
    public bool Screening { get; init; }
    public bool Vaccinations { get; init; }
    public bool Aftercare { get; init; }
    public bool PaymentsRead { get; init; }
    public bool PaymentsWrite { get; init; }
    public bool InventoryRead { get; init; }
    public bool InventoryReceive { get; init; }
    public bool Appointments { get; init; }
    public bool Catalog { get; init; }
    public bool Promotions { get; init; }
    public bool Reports { get; init; }
    public bool Accounts { get; init; }
}
