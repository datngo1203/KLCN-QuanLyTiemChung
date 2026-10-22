using TiemChung.Desktop.ViewsV2;

namespace TiemChung.Desktop.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private NavigationGroup _selectedGroup;
    private string _currentUserDisplayName;
    private bool _isNavigationOpen;
    private bool _isSearchOpen;
    private bool _isProfileOpen;
    private string _searchQuery = "";
    private string _feedbackMessage = "";

    public ShellViewModel(string userName)
    {
        _currentUserDisplayName = userName;
        NavigationGroups = NavigationRegistry.Groups;
        _selectedGroup = NavigationGroups[0];
        _selectedScreen = NavigationGroups[0].Screens[0];
        Overview = new OverviewViewModel(Navigate);
        Screening = new ScreeningViewModel();
        Profiles = new ProfilesViewModel();
        Registration = new RegistrationViewModel();
        Payment = new PaymentViewModel();
        Catalog = new CatalogViewModel();
        Inventory = new InventoryViewModel();
        Promotion = new PromotionViewModel();
        Vaccination = new VaccinationViewModel();
        Appointments = new AppointmentViewModel();
        Reports = new ReportViewModel();
        Accounts = new AccountViewModel(userName);
        ChangePassword = new ChangePasswordViewModel();
        CurrentView = NavigationRegistry.CreatePage("A01", this);
        SelectGroupCommand = new RelayCommand(p =>
        {
            if (p is not NavigationGroup group) return;
            if (group.Screens.Count == 1) Navigate(group.Screens[0].Id);
            else { SelectedGroup = group; IsNavigationOpen = false; }
        });
        NavigateCommand = new RelayCommand(p => { if (p is NavigationItem item) Navigate(item.Id); });
        ToggleNavigationCommand = new RelayCommand(_ => { IsNavigationOpen = !IsNavigationOpen; IsSearchOpen = false; IsProfileOpen = false; });
        ToggleSearchCommand = new RelayCommand(_ => { IsSearchOpen = !IsSearchOpen; IsNavigationOpen = false; IsProfileOpen = false; });
        ToggleProfileCommand = new RelayCommand(_ => { IsProfileOpen = !IsProfileOpen; IsNavigationOpen = false; IsSearchOpen = false; });
        OpenPasswordCommand = new RelayCommand(_ => Navigate("A14"));
        SignOutCommand = new RelayCommand(_ => { ClosePanels(); SignOutRequested?.Invoke(this, EventArgs.Empty); });
        DismissFeedbackCommand = new RelayCommand(_ => FeedbackMessage = "");
        ClosePanelsCommand = new RelayCommand(_ => ClosePanels());
    }

    public event EventHandler? SignOutRequested;
    public IReadOnlyList<NavigationGroup> NavigationGroups { get; }
    // TODO: Bind permissions from the authenticated shell session when available.
    public bool CanNavigate => true;
    public bool HasSecondaryMenu => SelectedGroup.Screens.Count > 1;
    public NavigationGroup SelectedGroup { get => _selectedGroup; private set { if (Set(ref _selectedGroup, value)) OnPropertyChanged(nameof(HasSecondaryMenu)); } }
    private NavigationItem _selectedScreen = null!;
    public NavigationItem SelectedScreen { get => _selectedScreen; private set => Set(ref _selectedScreen, value); }
    public OverviewViewModel Overview { get; }
    public ScreeningViewModel Screening { get; }
    public ProfilesViewModel Profiles { get; }
    public RegistrationViewModel Registration { get; }
    public PaymentViewModel Payment { get; }
    public CatalogViewModel Catalog { get; }
    public InventoryViewModel Inventory { get; }
    public PromotionViewModel Promotion { get; }
    public VaccinationViewModel Vaccination { get; }
    public AppointmentViewModel Appointments { get; }
    public ReportViewModel Reports { get; }
    public AccountViewModel Accounts { get; private set; }
    public ChangePasswordViewModel ChangePassword { get; private set; }
    private object _currentView = null!;
    public object CurrentView { get => _currentView; private set => Set(ref _currentView, value); }
    public string CurrentUserDisplayName { get => _currentUserDisplayName; private set => Set(ref _currentUserDisplayName, value); }
    public bool IsNavigationOpen { get => _isNavigationOpen; set => Set(ref _isNavigationOpen, value); }
    public bool IsSearchOpen { get => _isSearchOpen; private set => Set(ref _isSearchOpen, value); }
    public bool IsProfileOpen { get => _isProfileOpen; private set => Set(ref _isProfileOpen, value); }
    public string SearchQuery { get => _searchQuery; set { if (Set(ref _searchQuery, value)) { OnPropertyChanged(nameof(SearchResults)); OnPropertyChanged(nameof(HasNoSearchResults)); } } }
    public IReadOnlyList<NavigationItem> SearchResults => NavigationGroups.SelectMany(g => g.Screens).Where(s =>
        $"{s.Id} {s.Title}".Contains(SearchQuery.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
    public bool HasNoSearchResults => SearchResults.Count == 0;
    public string FeedbackMessage { get => _feedbackMessage; private set { if (Set(ref _feedbackMessage, value)) OnPropertyChanged(nameof(HasFeedback)); } }
    public bool HasFeedback => !string.IsNullOrEmpty(FeedbackMessage);
    public RelayCommand SelectGroupCommand { get; }
    public RelayCommand NavigateCommand { get; }
    public RelayCommand ToggleNavigationCommand { get; }
    public RelayCommand ToggleSearchCommand { get; }
    public RelayCommand ToggleProfileCommand { get; }
    public RelayCommand OpenPasswordCommand { get; }
    public RelayCommand SignOutCommand { get; }
    public RelayCommand DismissFeedbackCommand { get; }
    public RelayCommand ClosePanelsCommand { get; }

    public void BeginLocalSession(string userName)
    {
        Accounts = new AccountViewModel(userName);
        ChangePassword = new ChangePasswordViewModel();
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ChangePassword));
        CurrentUserDisplayName = userName;
        SelectedGroup = NavigationGroups[0];
        SelectedScreen = NavigationGroups[0].Screens[0];
        CurrentView = NavigationRegistry.CreatePage("A01", this);
        FeedbackMessage = "";
        ClosePanels();
    }

    private void Navigate(string id)
    {
        if (id == "A14")
        {
            ClosePanels();
            SelectedGroup = new NavigationGroup("Tài khoản", []);
            SelectedScreen = new NavigationItem("A14", "Đổi mật khẩu");
            CurrentView = NavigationRegistry.CreatePage(id, this);
            FeedbackMessage = "";
            return;
        }
        var group = NavigationGroups.FirstOrDefault(g => g.Screens.Any(s => s.Id == id));
        if (group is null) return;
        ClosePanels();
        SelectedGroup = group;
        var item = group.Screens.First(s => s.Id == id);
        SelectedScreen = item;
        CurrentView = NavigationRegistry.CreatePage(id, this);
        FeedbackMessage = "";
    }

    private void ShowFeedback(string message) => FeedbackMessage = message;
    private void ClosePanels() { IsNavigationOpen = false; IsSearchOpen = false; IsProfileOpen = false; }
}

public sealed record NavigationGroup(string Title, IReadOnlyList<NavigationItem> Screens)
{
    public NavigationItem? SingleScreen => Screens.Count == 1 ? Screens[0] : null;
}
public sealed record NavigationItem(string Id, string Title);
