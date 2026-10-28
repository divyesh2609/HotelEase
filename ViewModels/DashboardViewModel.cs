using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly IReservationService _reservationService;
        private readonly IRoomService _roomService;

        private DashboardMetrics _metrics = new();
        private bool _isLoading;

        public DashboardMetrics Metrics
        {
            get => _metrics;
            set => SetProperty(ref _metrics, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<Reservation> RecentBookings { get; } = new();
        public ObservableCollection<Reservation> TodayArrivals { get; } = new();
        public ObservableCollection<Reservation> TodayDepartures { get; } = new();
        public ObservableCollection<RoomTypeDistribution> RoomTypeStats { get; } = new();

        public ICommand RefreshCommand { get; }

        public event Action<string>? RequestNavigation;

        public ICommand NavigateToCommand { get; }

        public DashboardViewModel(IReportService reportService, IReservationService reservationService, IRoomService roomService)
        {
            _reportService = reportService;
            _reservationService = reservationService;
            _roomService = roomService;

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            NavigateToCommand = new RelayCommand(p =>
            {
                if (p is string target)
                {
                    RequestNavigation?.Invoke(target);
                }
            });
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // First ensure rooms status is up-to-date
                await _roomService.RecalculateRoomStatusesAsync();

                Metrics = await _reportService.GetDashboardMetricsAsync();

                // Recent Bookings (top 6)
                RecentBookings.Clear();
                var allRes = await _reservationService.GetAllReservationsAsync();
                int count = 0;
                foreach (var res in allRes)
                {
                    if (count++ >= 6) break;
                    RecentBookings.Add(res);
                }

                // Today Arrivals
                TodayArrivals.Clear();
                var arrivals = await _reservationService.GetTodayArrivalsAsync();
                foreach (var arr in arrivals)
                {
                    TodayArrivals.Add(arr);
                }

                // Today Departures
                TodayDepartures.Clear();
                var departures = await _reservationService.GetTodayDeparturesAsync();
                foreach (var dep in departures)
                {
                    TodayDepartures.Add(dep);
                }

                // Room Type Stats
                RoomTypeStats.Clear();
                var dist = await _reportService.GetRoomTypeDistributionAsync();
                foreach (var d in dist)
                {
                    RoomTypeStats.Add(d);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
