namespace TiemChung.Contracts;

public static class ProfileRules
{
    public static IReadOnlyList<string> Salutations { get; } = Array.AsReadOnly(new[] { "Ông", "Bà", "Anh", "Chị", "Khác" });
    public static IReadOnlyList<string> Relationships { get; } = Array.AsReadOnly(new[]
    {
        "Bản thân", "Con", "Anh", "Chị", "Em", "Cha", "Mẹ", "Vợ", "Chồng", "Ông", "Bà", "Cùng hộ khẩu"
    });

    public static DateOnly Today(DateTimeOffset utc) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc,"Asia/Ho_Chi_Minh").DateTime);

    public static bool BirthDatesAgree(string relation, DateOnly recipient, DateOnly? customer) =>
        customer is not DateOnly date || relation switch
        {
            "Bản thân" => recipient == date,
            "Con" => recipient > date,
            "Cha" or "Mẹ" or "Ông" or "Bà" => recipient < date,
            "Anh" or "Chị" => recipient <= date,
            "Em" => recipient >= date,
            _ => true
        };
}
