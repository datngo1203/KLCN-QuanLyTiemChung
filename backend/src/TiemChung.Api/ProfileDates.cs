namespace TiemChung.Api;

public static class ProfileDates
{
    public static DateOnly Today(DateTimeOffset utc) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc,"Asia/Ho_Chi_Minh").DateTime);
}
