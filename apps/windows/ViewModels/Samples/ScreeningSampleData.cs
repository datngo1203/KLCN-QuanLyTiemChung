using System.ComponentModel;

namespace TiemChung.Desktop.ViewModels.Samples;

/// <summary>UI-only examples. Never submit these records to an API or clinical workflow.</summary>
public static class ScreeningSampleData
{
    // TODO: Replace with reviewed queue, recipient/customer DTOs, vaccine catalog, and authorized read services.
    public static IReadOnlyList<ScreeningQueueSample> CreateQueue() =>
    [
        new("LK-01", "Người được tiêm 01", "08:15", "Chờ khám", "DK-01", "HS-01",
            "6 tuổi", "Khách hàng 01", "0900 000 001", "Vắc xin A", "Chưa có", "12/03/2020", "Chưa xác định"),
        new("LK-02", "Người được tiêm 02", "08:30", "Chờ khám", "DK-02", "HS-02",
            "4 tuổi", "Khách hàng 02", "0900 000 002", "Gói tiêm B", "Chưa có", "06/08/2022", "Chưa xác định")
    ];

    // TODO: Replace these names and codes with the authorized vaccine catalog API response.
    public static IReadOnlyList<VaccineCatalogSample> CreateVaccineCatalog() =>
    [
        new("VAC-01", "Vắc xin A"),
        new("VAC-02", "Vắc xin B"),
        new("VAC-03", "Vắc xin C")
    ];
}

public sealed record ScreeningQueueSample(string Id, string RecipientName, string TimeLabel, string Status,
    string RegistrationId, string RecipientId, string RecipientAgeLabel, string CustomerName,
    string CustomerPhone, string RegisteredService, string RecipientNationalId, string BirthDateLabel, string Gender) : INotifyPropertyChanged
{
    private bool _isSelected;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
}

public sealed record VaccineCatalogSample(string Code, string Name);
