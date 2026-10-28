using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using HotelEase.Commands;
using HotelEase.Services;

namespace HotelEase.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;

        private object _currentViewModel;
        private string _activeSection = "Dashboard";
        private string _pageTitle = "Hotel Overview";
        private string _pageSubtitle = "Real-time occupancy metrics and reception status";
        private string _currentDateTime = string.Empty;

        // Child ViewModels
        public DashboardViewModel DashboardVM { get; }
        public RoomsViewModel RoomsVM { get; }
        public GuestsViewModel GuestsVM { get; }
        public ReservationsViewModel ReservationsVM { get; }
        public CheckInCheckOutViewModel CheckInCheckOutVM { get; }
        public PaymentsViewModel PaymentsVM { get; }
        public ReportsViewModel ReportsVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string ActiveSection
        {
            get => _activeSection;
            set => SetProperty(ref _activeSection, value);
        }

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public string PageSubtitle
        {
            get => _pageSubtitle;
            set => SetProperty(ref _pageSubtitle, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        // Navigation Commands
        public ICommand NavigateCommand { get; }
        public ICommand QuickNewBookingCommand { get; }
        public ICommand QuickNewGuestCommand { get; }
        public ICommand RefreshAllCommand { get; }

        public MainViewModel()
        {
            // Initialize Services
            var roomService = new RoomService();
            var guestService = new GuestService();
            var reservationService = new ReservationService(roomService);
            var paymentService = new PaymentService();
            var reportService = new ReportService();

            // Initialize ViewModels
            DashboardVM = new DashboardViewModel(reportService, reservationService, roomService);
            RoomsVM = new RoomsViewModel(roomService);
            GuestsVM = new GuestsViewModel(guestService);
            ReservationsVM = new ReservationsViewModel(reservationService, roomService, guestService);
            CheckInCheckOutVM = new CheckInCheckOutViewModel(reservationService, paymentService);
            PaymentsVM = new PaymentsViewModel(paymentService, reservationService);
            ReportsVM = new ReportsViewModel(reportService, reservationService, roomService);
            SettingsVM = new SettingsViewModel();

            // Link cross-view events
            DashboardVM.RequestNavigation += NavigateToSection;
            SettingsVM.RequestAppRefresh += RefreshCurrentDataAsync;

            // Default View
            _currentViewModel = DashboardVM;

            // Live Time Timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm:ss tt");
            };
            _timer.Start();
            CurrentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm:ss tt");

            // Navigation Command
            NavigateCommand = new RelayCommand(p =>
            {
                if (p is string section)
                {
                    NavigateToSection(section);
                }
            });

            // Quick Topbar Actions
            QuickNewBookingCommand = new RelayCommand(async () =>
            {
                NavigateToSection("Reservations");
                if (ReservationsVM.NewReservationCommand.CanExecute(null))
                {
                    ReservationsVM.NewReservationCommand.Execute(null);
                }
            });

            QuickNewGuestCommand = new RelayCommand(async () =>
            {
                NavigateToSection("Guests");
                if (GuestsVM.AddGuestCommand.CanExecute(null))
                {
                    GuestsVM.AddGuestCommand.Execute(null);
                }
            });

            RefreshAllCommand = new AsyncRelayCommand(RefreshCurrentDataAsync);
        }

        public async Task InitializeAsync()
        {
            await DashboardVM.LoadDataAsync();
        }

        public void NavigateToSection(string section)
        {
            ActiveSection = section;

            switch (section)
            {
                case "Dashboard":
                    CurrentViewModel = DashboardVM;
                    PageTitle = "Hotel Overview & Dashboard";
                    PageSubtitle = "Real-time room occupancy, arrivals, departures, and key operations";
                    _ = DashboardVM.LoadDataAsync();
                    break;

                case "Rooms":
                    CurrentViewModel = RoomsVM;
                    PageTitle = "Room Inventory & Management";
                    PageSubtitle = "Inspect room availability, pricing tiers, specifications, and maintenance logs";
                    _ = RoomsVM.LoadRoomsAsync();
                    break;

                case "Guests":
                    CurrentViewModel = GuestsVM;
                    PageTitle = "Guest Registry & Profiles";
                    PageSubtitle = "Manage registered guests, verify contacts, and explore stay history";
                    _ = GuestsVM.LoadGuestsAsync();
                    break;

                case "Reservations":
                    CurrentViewModel = ReservationsVM;
                    PageTitle = "Reservation & Booking Central";
                    PageSubtitle = "Create bookings, check date availability, edit stays, and handle cancellations";
                    _ = ReservationsVM.LoadReservationsAsync();
                    break;

                case "CheckInCheckOut":
                    CurrentViewModel = CheckInCheckOutVM;
                    PageTitle = "Front Desk — Check-In & Check-Out";
                    PageSubtitle = "Express arrivals processing, key allocation, and checkout settlement";
                    _ = CheckInCheckOutVM.LoadDataAsync();
                    break;

                case "Payments":
                    CurrentViewModel = PaymentsVM;
                    PageTitle = "Billing & Financial Records";
                    PageSubtitle = "Track payments, inspect balances, and view official invoices";
                    _ = PaymentsVM.LoadPaymentsAsync();
                    break;

                case "Reports":
                    CurrentViewModel = ReportsVM;
                    PageTitle = "Reports & Business Intelligence";
                    PageSubtitle = "Generate audit reports, room occupancy metrics, and export data to CSV";
                    _ = ReportsVM.GenerateReportAsync();
                    break;

                case "Settings":
                    CurrentViewModel = SettingsVM;
                    PageTitle = "System Configuration & Database";
                    PageSubtitle = "Hotel profile, persistent SQLite management, and syllabus documentation";
                    break;
            }
        }

        private async Task RefreshCurrentDataAsync()
        {
            switch (ActiveSection)
            {
                case "Dashboard":
                    await DashboardVM.LoadDataAsync();
                    break;
                case "Rooms":
                    await RoomsVM.LoadRoomsAsync();
                    break;
                case "Guests":
                    await GuestsVM.LoadGuestsAsync();
                    break;
                case "Reservations":
                    await ReservationsVM.LoadReservationsAsync();
                    break;
                case "CheckInCheckOut":
                    await CheckInCheckOutVM.LoadDataAsync();
                    break;
                case "Payments":
                    await PaymentsVM.LoadPaymentsAsync();
                    break;
                case "Reports":
                    await ReportsVM.GenerateReportAsync();
                    break;
            }
        }
    }
}
