using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using TiemChung.Desktop.ViewModels.Samples;

namespace TiemChung.Desktop.ViewModels;

/// <summary>A02 presentation and form validation. Persistence and authorization remain unavailable.</summary>
public sealed class ScreeningViewModel : ViewModelBase
{
    private string _searchText = "";
    private string _statusFilter = "Tất cả";
    private ScreeningQueueSample? _selectedEncounter;
    private string _pulseText = "";
    private string _systolicText = "";
    private string _diastolicText = "";
    private string _temperatureText = "";
    private string _allergyHistory = "";
    private string _chronicConditions = "";
    private string _doctorNote = "";
    private string _conclusion = "";
    private string _deferReason = "";
    private string _feedback = "";
    private bool _isDirty;
    private bool _isDangerousAllergy;
    private int _safetyConfirmationStep;

    public ScreeningViewModel()
    {
        Queue = new ObservableCollection<ScreeningQueueSample>(ScreeningSampleData.CreateQueue());
        Queue.CollectionChanged += (_, _) => { if (SelectedEncounter is not null && !Queue.Contains(SelectedEncounter)) { IsDirty = false; SelectedEncounter = null; } RefreshFilter(); };
        VaccineCatalog = ScreeningSampleData.CreateVaccineCatalog();
        QueueView = CollectionViewSource.GetDefaultView(Queue);
        QueueView.Filter = MatchesFilter;
        Indications = new ObservableCollection<ScreeningIndicationDraft>();
        Indications.CollectionChanged += (_, _) => { ObserveIndications(); DraftChanged(); };

        SelectEncounterCommand = new RelayCommand(p => SelectEncounter(p as ScreeningQueueSample));
        RefreshQueueCommand = new RelayCommand(_ => { QueueView.Refresh(); Feedback = "Đã áp dụng lại bộ lọc trên hàng đợi."; });
        ResetDraftCommand = new RelayCommand(_ => ResetDraft(), _ => IsDirty);
        AddIndicationCommand = new RelayCommand(_ => AddIndication(), _ => AreIndicationsEnabled);
        RemoveIndicationCommand = new RelayCommand(p => { if (p is ScreeningIndicationDraft row) Indications.Remove(row); },
            p => AreIndicationsEnabled && p is ScreeningIndicationDraft);
        ConfirmSafetyCommand = new RelayCommand(_ => { SafetyConfirmationStep++; Feedback = SafetyConfirmationStep == 1 ? "Đã đọc cảnh báo. Xác nhận lần hai trước khi lưu." : "Đã xác nhận hai bước về cảnh báo dị ứng."; }, _ => IsDangerousAllergy && SafetyConfirmationStep < 2);
        SetConclusionCommand = new RelayCommand(p => Conclusion = p as string ?? "");
        LuuPhieuKhamVaChiDinhCommand = new RelayCommand(_ => SaveDraftFeedback(), _ => CanSave);
        ObserveIndications();
    }

    public ObservableCollection<ScreeningQueueSample> Queue { get; }
    public ICollectionView QueueView { get; }
    public IReadOnlyList<VaccineCatalogSample> VaccineCatalog { get; }
    public ObservableCollection<ScreeningIndicationDraft> Indications { get; }
    public IReadOnlyList<string> StatusFilters { get; } = ["Tất cả", "Chờ khám"];
    public IReadOnlyList<string> ConclusionOptions { get; } = ["Đủ điều kiện", "Tạm hoãn", "Chống chỉ định"];
    public RelayCommand SelectEncounterCommand { get; }
    public RelayCommand RefreshQueueCommand { get; }
    public RelayCommand ResetDraftCommand { get; }
    public RelayCommand AddIndicationCommand { get; }
    public RelayCommand RemoveIndicationCommand { get; }
    public RelayCommand ConfirmSafetyCommand { get; }
    public RelayCommand SetConclusionCommand { get; }
    public RelayCommand LuuPhieuKhamVaChiDinhCommand { get; }

    // TODO: Bind the reviewed clinical permission from the authenticated shell session.
    public bool CanSaveScreening => true;
    public bool IsDangerousAllergy { get => _isDangerousAllergy; set { if (Set(ref _isDangerousAllergy, value)) { SafetyConfirmationStep = 0; RefreshValidation(); } } }
    public int SafetyConfirmationStep { get => _safetyConfirmationStep; private set { if (Set(ref _safetyConfirmationStep, value)) { OnPropertyChanged(nameof(SafetyConfirmationLabel)); ConfirmSafetyCommand.NotifyCanExecuteChanged(); RefreshValidation(); } } }
    public string SafetyConfirmationLabel => SafetyConfirmationStep == 0 ? "Xác nhận đã đọc cảnh báo" : "Xác nhận lần hai";
    public bool NeedsSafetyConfirmation => IsDangerousAllergy && SafetyConfirmationStep < 2;
    public bool IsEligible => Conclusion == "Đủ điều kiện";
    public bool IsDeferred => Conclusion == "Tạm hoãn";
    public bool IsContraindicated => Conclusion == "Chống chỉ định";
    public bool NeedsDeferReason => Conclusion is "Tạm hoãn" or "Chống chỉ định";
    public string DataSourceLabel => "Dữ liệu mẫu";
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) RefreshFilter(); } }
    public string StatusFilter { get => _statusFilter; set { if (Set(ref _statusFilter, value)) RefreshFilter(); } }
    public bool HasNoQueueResults => Queue.Count > 0 && QueueView.IsEmpty;
    public bool HasEmptyQueue => Queue.Count == 0;
    public string QueueCountLabel => $"{QueueView.Cast<object>().Count()} lượt";
    public ScreeningQueueSample? SelectedEncounter { get => _selectedEncounter; private set { if (Set(ref _selectedEncounter, value)) { foreach (var row in Queue) row.IsSelected = row == value; OnPropertyChanged(nameof(HasSelection)); RefreshValidation(); } } }
    public bool HasSelection => SelectedEncounter is not null;

    public string PulseText { get => _pulseText; set => SetDraft(ref _pulseText, value); }
    public string SystolicText { get => _systolicText; set => SetDraft(ref _systolicText, value); }
    public string DiastolicText { get => _diastolicText; set => SetDraft(ref _diastolicText, value); }
    public string TemperatureText { get => _temperatureText; set => SetDraft(ref _temperatureText, value); }
    public string AllergyHistory { get => _allergyHistory; set => SetDraft(ref _allergyHistory, value); }
    public string ChronicConditions { get => _chronicConditions; set => SetDraft(ref _chronicConditions, value); }
    public string DoctorNote { get => _doctorNote; set => SetDraft(ref _doctorNote, value); }
    public string Conclusion
    {
        get => _conclusion;
        set => SetDraft(ref _conclusion, value);
    }
    public string DeferReason { get => _deferReason; set => SetDraft(ref _deferReason, value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool IsDirty { get => _isDirty; private set { if (Set(ref _isDirty, value)) ResetDraftCommand.NotifyCanExecuteChanged(); } }

    public string PulseError => RequiredIntegerError(PulseText, "Mạch", out int pulse) ??
        (pulse is < 40 or > 200 ? "Mạch phải trong khoảng 40–200 lần/phút." : "");
    public string SystolicError => RequiredIntegerError(SystolicText, "Huyết áp tâm thu", out int systolic) ??
        (TryInteger(DiastolicText, out int diastolic) && systolic <= diastolic ? "Tâm thu phải lớn hơn tâm trương." : "");
    public string DiastolicError => RequiredIntegerError(DiastolicText, "Huyết áp tâm trương", out int diastolic) ??
        (TryInteger(SystolicText, out int systolic) && systolic <= diastolic ? "Tâm thu phải lớn hơn tâm trương." : "");
    public string TemperatureError => RequiredDecimalError(TemperatureText, "Nhiệt độ", out decimal temperature) ??
        (temperature is < 35m or > 42m ? "Nhiệt độ phải trong khoảng 35–42 °C." : "");
    public string TemperatureSuggestion => TryDecimal(TemperatureText, out decimal temperature) && temperature >= 38m
        ? "Gợi ý: Tạm hoãn. Đây chỉ là gợi ý, không tự đổi kết luận." : "";
    public string AllergyError => string.IsNullOrWhiteSpace(AllergyHistory) ? "Tiền sử dị ứng là bắt buộc." : "";
    public string ConclusionError => !ConclusionOptions.Contains(Conclusion) ? "Chọn một trong ba kết luận." : "";
    public string DeferReasonError => (Conclusion is "Tạm hoãn" or "Chống chỉ định") && string.IsNullOrWhiteSpace(DeferReason)
        ? "Nhập lý do khi kết luận khác “Đủ điều kiện”." : "";
    public string IndicationsError => Conclusion == "Đủ điều kiện" && Indications.Count == 0
        ? "Cần ít nhất một chỉ định khi đủ điều kiện."
        : Conclusion == "Đủ điều kiện" && Indications.Any(x => x.Vaccine is null) ? "Chọn vắc xin cho mọi dòng chỉ định." : "";
    public string AllergyAlertText => string.IsNullOrWhiteSpace(AllergyHistory)
        ? "Tiền sử dị ứng chưa được nhập hoặc đánh giá. Không diễn giải ô trống là không dị ứng."
        : "Đã khai báo tiền sử dị ứng. Bác sĩ cần đánh giá trước khi đưa ra kết luận.";
    public bool AreIndicationsEnabled => HasSelection && Conclusion == "Đủ điều kiện";
    public bool VitalsAreValid => PulseError.Length == 0 && SystolicError.Length == 0 && DiastolicError.Length == 0 && TemperatureError.Length == 0;
    public bool IsFormValid => HasSelection && VitalsAreValid && AllergyError.Length == 0 && ConclusionError.Length == 0 &&
        DeferReasonError.Length == 0 && IndicationsError.Length == 0 &&
        (Conclusion != "Đủ điều kiện" || Indications.All(x => x.Vaccine is not null));
    public bool CanSave => CanSaveScreening && IsFormValid && !NeedsSafetyConfirmation;
    public string ValidationSummary => CanSave ? "Thông tin hợp lệ theo kiểm tra trên giao diện." : "Hoàn thành các trường bắt buộc và sửa lỗi để bật nút lưu.";

    private bool SetDraft(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (!Set(ref field, value, propertyName)) return false;
        if (propertyName == nameof(AllergyHistory)) SafetyConfirmationStep = 0;
        if (propertyName == nameof(Conclusion) && Conclusion != "Đủ điều kiện") Indications.Clear();
        IsDirty = true; DraftChanged();
        return true;
    }

    private void DraftChanged()
    {
        RefreshValidation();
        IsDirty = true;
    }

    private void RefreshValidation()
    {
        OnPropertyChanged(nameof(PulseError)); OnPropertyChanged(nameof(SystolicError)); OnPropertyChanged(nameof(DiastolicError));
        OnPropertyChanged(nameof(TemperatureError)); OnPropertyChanged(nameof(TemperatureSuggestion)); OnPropertyChanged(nameof(AllergyError));
        OnPropertyChanged(nameof(AllergyAlertText));
        OnPropertyChanged(nameof(ConclusionError)); OnPropertyChanged(nameof(DeferReasonError)); OnPropertyChanged(nameof(IndicationsError));
        OnPropertyChanged(nameof(AreIndicationsEnabled)); OnPropertyChanged(nameof(IsEligible)); OnPropertyChanged(nameof(IsDeferred)); OnPropertyChanged(nameof(IsContraindicated)); OnPropertyChanged(nameof(NeedsDeferReason)); OnPropertyChanged(nameof(NeedsSafetyConfirmation)); OnPropertyChanged(nameof(VitalsAreValid)); OnPropertyChanged(nameof(IsFormValid));
        OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(ValidationSummary));
        LuuPhieuKhamVaChiDinhCommand.NotifyCanExecuteChanged(); AddIndicationCommand.NotifyCanExecuteChanged();
        RemoveIndicationCommand.NotifyCanExecuteChanged();
        ConfirmSafetyCommand.NotifyCanExecuteChanged();
    }

    private void ObserveIndications()
    {
        foreach (var row in Indications) row.PropertyChanged -= Indication_PropertyChanged;
        foreach (var row in Indications) row.PropertyChanged += Indication_PropertyChanged;
        RefreshValidation();
    }

    private void Indication_PropertyChanged(object? sender, PropertyChangedEventArgs e) => DraftChanged();

    private bool MatchesFilter(object item) => item is ScreeningQueueSample row &&
        (StatusFilter == "Tất cả" || row.Status == StatusFilter) &&
        (string.IsNullOrWhiteSpace(SearchText) || $"{row.Id} {row.RecipientName} {row.RegistrationId}".Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void RefreshFilter()
    {
        QueueView.Refresh();
        OnPropertyChanged(nameof(HasNoQueueResults)); OnPropertyChanged(nameof(HasEmptyQueue)); OnPropertyChanged(nameof(QueueCountLabel));
    }

    private void SelectEncounter(ScreeningQueueSample? row)
    {
        if (row is null || row == SelectedEncounter) return;
        if (IsDirty) { Feedback = "Bản nháp hiện tại chưa lưu. Hủy thay đổi trước khi chọn lượt khác."; return; }
        SelectedEncounter = row;
        Feedback = $"Đang xem {row.RecipientName}.";
    }

    private void AddIndication()
    {
        if (!AreIndicationsEnabled) return;
        Indications.Add(new ScreeningIndicationDraft());
        Feedback = "Đã thêm một dòng chỉ định bản nháp.";
    }

    private void ResetDraft()
    {
        PulseText = ""; SystolicText = ""; DiastolicText = ""; TemperatureText = "";
        AllergyHistory = ""; ChronicConditions = ""; DoctorNote = ""; _conclusion = ""; OnPropertyChanged(nameof(Conclusion));
        DeferReason = ""; Indications.Clear(); IsDangerousAllergy = false; SafetyConfirmationStep = 0; IsDirty = false; RefreshValidation();
        Feedback = "Đã hủy bản nháp cục bộ.";
    }

    private void SaveDraftFeedback()
    {
        if (!CanSave) return;
        // TODO: Connect a reviewed screening command/API and authorized doctor session before persistence.
        Feedback = "Thông tin hợp lệ nhưng chưa được lưu vào hệ thống.";
    }

    private static string? RequiredIntegerError(string text, string label, out int value)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; return $"{label} là bắt buộc."; }
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) ? null : $"{label} phải là số nguyên.";
    }

    private static string? RequiredDecimalError(string text, string label, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; return $"{label} là bắt buộc."; }
        return TryDecimal(text, out value) ? null : $"{label} phải là số hợp lệ.";
    }

    private static bool TryInteger(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    private static bool TryDecimal(string text, out decimal value)
    {
        const NumberStyles decimalNumber = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(text, decimalNumber, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(text, decimalNumber, CultureInfo.InvariantCulture, out value);
    }
}

public sealed class ScreeningIndicationDraft : INotifyPropertyChanged
{
    private VaccineCatalogSample? _vaccine;
    private string _note = "";
    public event PropertyChangedEventHandler? PropertyChanged;
    public VaccineCatalogSample? Vaccine { get => _vaccine; set { if (_vaccine == value) return; _vaccine = value; PropertyChanged?.Invoke(this, new(nameof(Vaccine))); } }
    public string Note { get => _note; set { if (_note == value) return; _note = value; PropertyChanged?.Invoke(this, new(nameof(Note))); } }
}
