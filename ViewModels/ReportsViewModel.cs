using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Models;
using HotelEase.Services;
using Microsoft.Win32;

namespace HotelEase.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly IReservationService _reservationService;
        private readonly IRoomService _roomService;

        private string _selectedReportType = "All Reservations";
        private DateTime _fromDate = DateTime.Today.AddDays(-30);
        private DateTime _toDate = DateTime.Today.AddDays(30);

        private decimal _totalRevenue;
        private int _totalRecordCount;
        private double _occupancyRate;
        private bool _isLoading;

        public List<string> ReportTypes { get; } = new()
        {
            "All Reservations",
            "Today's Arrivals",
            "Today's Departures",
            "Currently Occupied Rooms",
            "Available Rooms",
            "Revenue & Payments"
        };

        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (SetProperty(ref _selectedReportType, value))
                {
                    _ = GenerateReportAsync();
                }
            }
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public int TotalRecordCount
        {
            get => _totalRecordCount;
            set => SetProperty(ref _totalRecordCount, value);
        }

        public double OccupancyRate
        {
            get => _occupancyRate;
            set => SetProperty(ref _occupancyRate, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<object> ReportRows { get; } = new();
        public ObservableCollection<RoomTypeDistribution> TypeDistribution { get; } = new();

        public ICommand GenerateReportCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand QuickFilterCommand { get; }

        public ReportsViewModel(IReportService reportService, IReservationService reservationService, IRoomService roomService)
        {
            _reportService = reportService;
            _reservationService = reservationService;
            _roomService = roomService;

            GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
            ExportCsvCommand = new AsyncRelayCommand(OnExportCsvAsync);
            QuickFilterCommand = new RelayCommand(p =>
            {
                if (p is string filter)
                {
                    ApplyQuickFilter(filter);
                }
            });
        }

        public void ApplyQuickFilter(string filter)
        {
            var today = DateTime.Today;
            switch (filter)
            {
                case "Today":
                    FromDate = today;
                    ToDate = today;
                    break;
                case "Week":
                    FromDate = today.AddDays(-(int)today.DayOfWeek);
                    ToDate = FromDate.AddDays(7);
                    break;
                case "Month":
                    FromDate = new DateTime(today.Year, today.Month, 1);
                    ToDate = FromDate.AddMonths(1).AddDays(-1);
                    break;
                case "All":
                    FromDate = today.AddYears(-1);
                    ToDate = today.AddYears(1);
                    break;
            }
            _ = GenerateReportAsync();
        }

        public async Task GenerateReportAsync()
        {
            try
            {
                IsLoading = true;
                ReportRows.Clear();

                // Load distribution for visual chart
                TypeDistribution.Clear();
                var dist = await _reportService.GetRoomTypeDistributionAsync();
                foreach (var d in dist)
                {
                    TypeDistribution.Add(d);
                }

                // General metrics
                var metrics = await _reportService.GetDashboardMetricsAsync();
                OccupancyRate = metrics.OccupancyPercentage;
                TotalRevenue = metrics.TotalRevenue;

                switch (SelectedReportType)
                {
                    case "All Reservations":
                        var resList = await _reportService.GetReservationsReportAsync(FromDate, ToDate);
                        foreach (var r in resList) ReportRows.Add(r);
                        TotalRecordCount = resList.Count;
                        break;

                    case "Today's Arrivals":
                        var arrList = await _reservationService.GetTodayArrivalsAsync();
                        foreach (var a in arrList) ReportRows.Add(a);
                        TotalRecordCount = arrList.Count;
                        break;

                    case "Today's Departures":
                        var depList = await _reservationService.GetTodayDeparturesAsync();
                        foreach (var d in depList) ReportRows.Add(d);
                        TotalRecordCount = depList.Count;
                        break;

                    case "Currently Occupied Rooms":
                        var allRooms = await _roomService.GetAllRoomsAsync();
                        var occRooms = allRooms.FindAll(r => r.Status == "Occupied");
                        foreach (var r in occRooms) ReportRows.Add(r);
                        TotalRecordCount = occRooms.Count;
                        break;

                    case "Available Rooms":
                        var allR = await _roomService.GetAllRoomsAsync();
                        var availRooms = allR.FindAll(r => r.Status == "Available");
                        foreach (var r in availRooms) ReportRows.Add(r);
                        TotalRecordCount = availRooms.Count;
                        break;

                    case "Revenue & Payments":
                        var payList = await _reportService.GetRevenueReportAsync(FromDate, ToDate);
                        foreach (var p in payList) ReportRows.Add(p);
                        TotalRecordCount = payList.Count;
                        break;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnExportCsvAsync()
        {
            if (ReportRows.Count == 0)
            {
                MessageBox.Show("There are no records in the current report to export.", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Report to CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"HotelEase_{SelectedReportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _reportService.ExportToCsvAsync(dialog.FileName, ReportRows, SelectedReportType);
                    MessageBox.Show($"Report exported successfully!\n\nFile saved to:\n{dialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
