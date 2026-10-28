using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.ViewModels
{
    public class ReservationsViewModel : ViewModelBase
    {
        private readonly IReservationService _reservationService;
        private readonly IRoomService _roomService;
        private readonly IGuestService _guestService;

        private string _searchText = string.Empty;
        private string _selectedStatusFilter = "All";
        private Reservation? _selectedReservation;
        private bool _isLoading;

        public ObservableCollection<Reservation> Reservations { get; } = new();
        public ICollectionView ReservationsView { get; }

        public List<string> StatusFilters { get; } = new() { "All", "Confirmed", "Checked In", "Checked Out", "Cancelled" };

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ReservationsView.Refresh();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ReservationsView.Refresh();
                }
            }
        }

        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set => SetProperty(ref _selectedReservation, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewReservationCommand { get; }
        public ICommand EditReservationCommand { get; }
        public ICommand CancelReservationCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand PrintInvoiceCommand { get; }

        public Func<Reservation?, Task<bool>>? ShowReservationDialogAsync { get; set; }
        public Func<Reservation, Task>? ShowInvoiceDialogAsync { get; set; }

        public ReservationsViewModel(IReservationService reservationService, IRoomService roomService, IGuestService guestService)
        {
            _reservationService = reservationService;
            _roomService = roomService;
            _guestService = guestService;

            ReservationsView = CollectionViewSource.GetDefaultView(Reservations);
            ReservationsView.Filter = FilterReservations;

            RefreshCommand = new AsyncRelayCommand(LoadReservationsAsync);
            NewReservationCommand = new AsyncRelayCommand(OnNewReservationAsync);
            EditReservationCommand = new AsyncRelayCommand(OnEditReservationAsync, () => SelectedReservation != null && SelectedReservation.BookingStatus == "Confirmed");
            CancelReservationCommand = new AsyncRelayCommand(OnCancelReservationAsync, () => SelectedReservation != null && SelectedReservation.BookingStatus == "Confirmed");
            CheckInCommand = new AsyncRelayCommand(OnCheckInAsync, () => SelectedReservation != null && SelectedReservation.BookingStatus == "Confirmed");
            CheckOutCommand = new AsyncRelayCommand(OnCheckOutAsync, () => SelectedReservation != null && SelectedReservation.BookingStatus == "Checked In");
            PrintInvoiceCommand = new AsyncRelayCommand(OnPrintInvoiceAsync, () => SelectedReservation != null);
        }

        public async Task LoadReservationsAsync()
        {
            try
            {
                IsLoading = true;
                await _roomService.RecalculateRoomStatusesAsync();
                var list = await _reservationService.GetAllReservationsAsync();

                Reservations.Clear();
                foreach (var res in list)
                {
                    Reservations.Add(res);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterReservations(object obj)
        {
            if (obj is not Reservation r) return false;

            if (SelectedStatusFilter != "All" && !string.Equals(r.BookingStatus, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SearchText.Trim().ToLowerInvariant();
                var refMatch = r.BookingReference.ToLowerInvariant().Contains(q);
                var guestMatch = r.GuestName.ToLowerInvariant().Contains(q);
                var phoneMatch = r.GuestPhone.ToLowerInvariant().Contains(q);
                var roomMatch = r.RoomNumber.ToLowerInvariant().Contains(q);
                if (!refMatch && !guestMatch && !phoneMatch && !roomMatch) return false;
            }

            return true;
        }

        private async Task OnNewReservationAsync()
        {
            if (ShowReservationDialogAsync != null)
            {
                var newRes = new Reservation
                {
                    CheckInDate = DateTime.Today,
                    CheckOutDate = DateTime.Today.AddDays(1),
                    NumberOfGuests = 1,
                    NumberOfNights = 1,
                    BookingStatus = "Confirmed"
                };

                if (await ShowReservationDialogAsync(newRes))
                {
                    try
                    {
                        var refCode = await _reservationService.CreateReservationAsync(newRes);
                        await LoadReservationsAsync();
                        MessageBox.Show($"Reservation successfully created!\n\nBooking Reference: {refCode}", "Booking Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Creating Reservation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async Task OnEditReservationAsync()
        {
            if (SelectedReservation == null || ShowReservationDialogAsync == null) return;

            var clone = new Reservation
            {
                ReservationId = SelectedReservation.ReservationId,
                BookingReference = SelectedReservation.BookingReference,
                GuestId = SelectedReservation.GuestId,
                RoomId = SelectedReservation.RoomId,
                CheckInDate = SelectedReservation.CheckInDate,
                CheckOutDate = SelectedReservation.CheckOutDate,
                NumberOfGuests = SelectedReservation.NumberOfGuests,
                NumberOfNights = SelectedReservation.NumberOfNights,
                PricePerNight = SelectedReservation.PricePerNight,
                Discount = SelectedReservation.Discount,
                AdditionalCharges = SelectedReservation.AdditionalCharges,
                TotalAmount = SelectedReservation.TotalAmount,
                BookingStatus = SelectedReservation.BookingStatus,
                SpecialRequests = SelectedReservation.SpecialRequests,
                GuestName = SelectedReservation.GuestName,
                RoomNumber = SelectedReservation.RoomNumber,
                RoomType = SelectedReservation.RoomType
            };

            if (await ShowReservationDialogAsync(clone))
            {
                try
                {
                    await _reservationService.UpdateReservationAsync(clone);
                    await LoadReservationsAsync();
                    MessageBox.Show("Reservation successfully updated.", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Updating Reservation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnCancelReservationAsync()
        {
            if (SelectedReservation == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to cancel booking {SelectedReservation.BookingReference} for {SelectedReservation.GuestName}?\n\nThis will release Room {SelectedReservation.RoomNumber} for other guests.",
                "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _reservationService.CancelReservationAsync(SelectedReservation.ReservationId, "Cancelled by user");
                    await LoadReservationsAsync();
                    MessageBox.Show("Reservation has been cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Cancelling Reservation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnCheckInAsync()
        {
            if (SelectedReservation == null) return;

            var confirm = MessageBox.Show(
                $"Confirm check-in for {SelectedReservation.GuestName} into Room {SelectedReservation.RoomNumber}?",
                "Confirm Check-in",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _reservationService.CheckInReservationAsync(SelectedReservation.ReservationId);
                    await LoadReservationsAsync();
                    MessageBox.Show($"Check-in completed successfully! Room {SelectedReservation.RoomNumber} is now Occupied.", "Checked In", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Checking In", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnCheckOutAsync()
        {
            if (SelectedReservation == null) return;

            var confirm = MessageBox.Show(
                $"Proceed to check-out for {SelectedReservation.GuestName} from Room {SelectedReservation.RoomNumber}?\nTotal Stay Bill: ${SelectedReservation.TotalAmount:N2}",
                "Confirm Check-out",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _reservationService.CheckOutReservationAsync(SelectedReservation.ReservationId);
                    await LoadReservationsAsync();
                    MessageBox.Show($"Check-out completed successfully! Room {SelectedReservation.RoomNumber} has been released.", "Checked Out", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Checking Out", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnPrintInvoiceAsync()
        {
            if (SelectedReservation != null && ShowInvoiceDialogAsync != null)
            {
                await ShowInvoiceDialogAsync(SelectedReservation);
            }
        }
    }
}
