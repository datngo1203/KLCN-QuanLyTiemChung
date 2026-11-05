using TiemChung.Desktop.ViewsV2;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly IdentityApiService _identity;
    private readonly int _sessionRevision;
    private NavigationGroup _selectedGroup;
    private string _currentUserDisplayName;
    private bool _isNavigationOpen;
    private bool _isSearchOpen;
    private bool _isProfileOpen;
    private string _searchQuery = "";
    private string _feedbackMessage = "";

    public ShellViewModel(string userName, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current;
        _sessionRevision = _identity.SessionRevision;
        _currentUserDisplayName = userName;
        NavigationGroups = NavigationRegistry.VisibleGroups(_identity);
        _selectedGroup = NavigationGroups.FirstOrDefault() ?? new NavigationGroup("Tổng quan", []);
        _selectedScreen = NavigationRegistry.Groups[0].Screens[0];
        Overview = new OverviewViewModel(Navigate, canAccess: CanAccess);
        _Screening = new(() => new ScreeningViewModel(identity: _identity));
        _Profiles = new(() => new ProfilesViewModel(canWrite: _identity.CanWriteProfiles));
        _Registration = new(() => new RegistrationViewModel(identity: _identity));
        _Payment = new(() => new PaymentViewModel(identity: _identity));
        _Catalog = new(() => new CatalogViewModel(permissions: _identity.Permissions));
        _Inventory = new(() => new InventoryViewModel(identity: _identity));
        _Promotion = new(() => new PromotionViewModel(identity: _identity));
        _Vaccination = new(() => new VaccinationViewModel(identity: _identity));
        _Aftercare = new(() => new AftercareViewModel(identity: _identity));
        _Appointments = new(() => new AppointmentViewModel(identity: _identity));
        _Reports = new(() => new ReportViewModel());
        _Accounts = new(() => new AccountViewModel(userName, _identity));
        _ChangePassword = new(() => new ChangePasswordViewModel());
        CurrentView = CanAccess("A01") ? NavigationRegistry.CreatePage("A01", this) : "Phiên không có quyền truy cập ứng dụng. Vui lòng đăng nhập lại.";
        SelectGroupCommand = new RelayCommand(p =>
        {
            if (p is not NavigationGroup group || !CanNavigate || !NavigationGroups.Contains(group)) return;
            if (group.Screens.Count == 1) Navigate(group.Screens[0].Id);
            else { SelectedGroup = group; IsNavigationOpen = false; }
        });
        NavigateCommand = new RelayCommand(p => { if (p is NavigationItem item) Navigate(item.Id); });
        ToggleNavigationCommand = new RelayCommand(_ => { IsNavigationOpen = !IsNavigationOpen; IsSearchOpen = false; IsProfileOpen = false; });
        ToggleSearchCommand = new RelayCommand(_ => { IsSearchOpen = !IsSearchOpen; IsNavigationOpen = false; IsProfileOpen = false; });
        ToggleProfileCommand = new RelayCommand(_ => { IsProfileOpen = !IsProfileOpen; IsNavigationOpen = false; IsSearchOpen = false; });
        OpenProfileCommand = new RelayCommand(_ => { IsProfileOpen = false; ShowFeedback($"Tài khoản đang thao tác: {CurrentUserDisplayName}. Trang phiên A13 chưa triển khai trong đợt này."); });
        OpenPasswordCommand = new RelayCommand(_ => Navigate("A14"));
        SignOutCommand = new RelayCommand(_ => { ClosePanels(); SignOutRequested?.Invoke(this, EventArgs.Empty); });
        DismissFeedbackCommand = new RelayCommand(_ => FeedbackMessage = "");
        ClosePanelsCommand = new RelayCommand(_ => ClosePanels());
    }

    public event EventHandler? SignOutRequested;
    public IReadOnlyList<NavigationGroup> NavigationGroups { get; }
    public bool CanNavigate => _identity.IsSignedIn && _identity.SessionRevision == _sessionRevision;
    public bool HasSecondaryMenu => SelectedGroup.Screens.Count > 1;
    public NavigationGroup SelectedGroup { get => _selectedGroup; private set { if (Set(ref _selectedGroup, value)) OnPropertyChanged(nameof(HasSecondaryMenu)); } }
    private NavigationItem _selectedScreen = null!;
    public NavigationItem SelectedScreen { get => _selectedScreen; private set => Set(ref _selectedScreen, value); }
    public OverviewViewModel Overview { get; }
    private readonly Lazy<ScreeningViewModel> _Screening;
    public ScreeningViewModel Screening => _Screening.Value;
    private readonly Lazy<ProfilesViewModel> _Profiles;
    public ProfilesViewModel Profiles => _Profiles.Value;
    private readonly Lazy<RegistrationViewModel> _Registration;
    public RegistrationViewModel Registration => _Registration.Value;
    private readonly Lazy<PaymentViewModel> _Payment;
    public PaymentViewModel Payment => _Payment.Value;
    private readonly Lazy<CatalogViewModel> _Catalog;
    public CatalogViewModel Catalog => _Catalog.Value;
    private readonly Lazy<InventoryViewModel> _Inventory;
    public InventoryViewModel Inventory => _Inventory.Value;
    private readonly Lazy<PromotionViewModel> _Promotion;
    public PromotionViewModel Promotion => _Promotion.Value;
    private readonly Lazy<VaccinationViewModel> _Vaccination;
    public VaccinationViewModel Vaccination => _Vaccination.Value;
    private readonly Lazy<AftercareViewModel> _Aftercare;
    public AftercareViewModel Aftercare => _Aftercare.Value;
    private readonly Lazy<AppointmentViewModel> _Appointments;
    public AppointmentViewModel Appointments => _Appointments.Value;
    private readonly Lazy<ReportViewModel> _Reports;
    public ReportViewModel Reports => _Reports.Value;
    private readonly Lazy<AccountViewModel> _Accounts;
    public AccountViewModel Accounts => _Accounts.Value;
    private readonly Lazy<ChangePasswordViewModel> _ChangePassword;
    public ChangePasswordViewModel ChangePassword => _ChangePassword.Value;
    private object _currentView = null!;
    public object CurrentView { get => _currentView; private set => Set(ref _currentView, value); }
    public string CurrentUserDisplayName { get => _currentUserDisplayName; private set => Set(ref _currentUserDisplayName, value); }
    public bool IsNavigationOpen { get => _isNavigationOpen; set => Set(ref _isNavigationOpen, value); }
    public bool IsSearchOpen { get => _isSearchOpen; private set => Set(ref _isSearchOpen, value); }
    public bool IsProfileOpen { get => _isProfileOpen; private set => Set(ref _isProfileOpen, value); }
    public string SearchQuery { get => _searchQuery; set { if (Set(ref _searchQuery, value)) { OnPropertyChanged(nameof(SearchResults)); OnPropertyChanged(nameof(HasNoSearchResults)); } } }
    public IReadOnlyList<NavigationItem> SearchResults => (CanNavigate ? NavigationGroups : []).SelectMany(g => g.Screens).Where(s =>
        $"{s.Id} {s.Title}".Contains(SearchQuery.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
    public bool HasNoSearchResults => SearchResults.Count == 0;
    public string FeedbackMessage { get => _feedbackMessage; private set { if (Set(ref _feedbackMessage, value)) OnPropertyChanged(nameof(HasFeedback)); } }
    public bool HasFeedback => !string.IsNullOrEmpty(FeedbackMessage);
    public RelayCommand SelectGroupCommand { get; }
    public RelayCommand NavigateCommand { get; }
    public RelayCommand ToggleNavigationCommand { get; }
    public RelayCommand ToggleSearchCommand { get; }
    public RelayCommand ToggleProfileCommand { get; }
    public RelayCommand OpenProfileCommand { get; }
    public RelayCommand OpenPasswordCommand { get; }
    public RelayCommand SignOutCommand { get; }
    public RelayCommand DismissFeedbackCommand { get; }
    public RelayCommand ClosePanelsCommand { get; }

    public bool CanAccess(string id) => CanNavigate && NavigationGroups.Any(g => g.Screens.Any(s => s.Id == id));

    public void Navigate(string id)
    {
        if (!CanAccess(id)) return;
        var group = NavigationGroups.FirstOrDefault(g => g.Screens.Any(s => s.Id == id));
        if (group is null) return;
        ClosePanels();
        SelectedGroup = group;
        var item = group.Screens.First(s => s.Id == id);
        SelectedScreen = item;
        if (id == "A10") _ = Promotion.LoadAsync();
        if (id == "A11") _ = Reports.GenerateAsync();
        if (id == "A12") _ = Accounts.LoadAsync();
        CurrentView = NavigationRegistry.CreatePage(id, this);
        FeedbackMessage = "";
    }

    private void ShowFeedback(string message) => FeedbackMessage = message;
    private void ClosePanels() { IsNavigationOpen = false; IsSearchOpen = false; IsProfileOpen = false; }
}

public sealed record NavigationGroup(string Title, IReadOnlyList<NavigationItem> Screens);
public sealed record NavigationItem(string Id, string Title, string RequiredCapability = "");
