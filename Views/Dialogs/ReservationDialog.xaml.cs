using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.Views.Dialogs
{
    public partial class ReservationDialog : Window
    {
        private readonly IReservationService _reservationService;
        private readonly IGuestService _guestService;
        private readonly IRoomService _roomService;
        private readonly bool _isEdit;
        private bool _isInitialized;

        public Reservation Reservation { get; }
        private List<Guest> _allGuests = new();
        private List<Room> _availableRooms = new();

        public ReservationDialog(Reservation res, IReservationService reservationService, IGuestService guestService, IRoomService roomService, bool isEdit = false)
        {
            InitializeComponent();
            Reservation = res;
            _reservationService = reservationService;
            _guestService = guestService;
            _roomService = roomService;
            _isEdit = isEdit;

            TxtTitle.Text = _isEdit ? $"Edit Booking ({res.BookingReference})" : "Create New Reservation";

            Loaded += OnWindowLoaded;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Populate guests
            _allGuests = await _guestService.GetAllGuestsAsync();
            CmbGuest.ItemsSource = _allGuests;

            if (Reservation.GuestId > 0)
            {
                CmbGuest.SelectedItem = _allGuests.Find(g => g.GuestId == Reservation.GuestId);
            }

            DpCheckIn.SelectedDate = Reservation.CheckInDate;
            DpCheckOut.SelectedDate = Reservation.CheckOutDate;
            TxtNumGuests.Text = Reservation.NumberOfGuests.ToString();
            TxtDiscount.Text = Reservation.Discount.ToString("F2");
            TxtRequests.Text = Reservation.SpecialRequests;

            _isInitialized = true;

            await RefreshAvailableRoomsAsync();
            UpdateCalculations();
        }

        private async Task RefreshAvailableRoomsAsync()
        {
            if (!_isInitialized) return;

            var inDate = DpCheckIn.SelectedDate ?? DateTime.Today;
            var outDate = DpCheckOut.SelectedDate ?? DateTime.Today.AddDays(1);

            if (outDate <= inDate)
            {
                CmbRoom.ItemsSource = null;
                return;
            }

            string? typeFilter = (CmbTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (typeFilter == "All Types") typeFilter = null;

            int? excludeResId = _isEdit ? Reservation.ReservationId : null;

            _availableRooms = await _reservationService.GetAvailableRoomsAsync(inDate, outDate, typeFilter, null, excludeResId);

            // If editing and current room isn't in list (e.g. if occupied or status), ensure current room is available for selection
            if (_isEdit && Reservation.RoomId > 0 && !_availableRooms.Exists(r => r.RoomId == Reservation.RoomId))
            {
                var curRoom = await _roomService.GetRoomByIdAsync(Reservation.RoomId);
                if (curRoom != null) _availableRooms.Insert(0, curRoom);
            }

            CmbRoom.ItemsSource = _availableRooms;

            if (Reservation.RoomId > 0)
            {
                CmbRoom.SelectedItem = _availableRooms.Find(r => r.RoomId == Reservation.RoomId);
            }
            else if (_availableRooms.Count > 0)
            {
                CmbRoom.SelectedIndex = 0;
            }
        }

        private async void OnDatesOrRoomChanged(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            if (sender == DpCheckIn || sender == DpCheckOut)
            {
                await RefreshAvailableRoomsAsync();
            }

            if (CmbRoom.SelectedItem is Room selectedRoom)
            {
                TxtRate.Text = selectedRoom.PricePerNight.ToString("F2");
            }

            UpdateCalculations();
        }

        private async void OnTypeFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshAvailableRoomsAsync();
            UpdateCalculations();
        }

        private void OnCalculationInputsChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalculations();
        }

        private void UpdateCalculations()
        {
            if (!_isInitialized) return;

            var inDate = DpCheckIn.SelectedDate ?? DateTime.Today;
            var outDate = DpCheckOut.SelectedDate ?? DateTime.Today.AddDays(1);

            int nights = Math.Max(1, (int)(outDate.Date - inDate.Date).TotalDays);
            TxtNights.Text = nights.ToString();

            decimal rate = decimal.TryParse(TxtRate.Text?.Trim(), out var r) ? r : 0;
            decimal disc = decimal.TryParse(TxtDiscount.Text?.Trim(), out var d) ? d : 0;

            decimal roomCost = nights * rate;
            decimal total = Math.Max(0, roomCost - disc);

            TxtTotalAmount.Text = $"${total:N2}";
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (CmbGuest.SelectedItem is not Guest selectedGuest)
            {
                MessageBox.Show("Please select a registered guest.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbGuest.Focus();
                return;
            }

            var inDate = DpCheckIn.SelectedDate;
            var outDate = DpCheckOut.SelectedDate;

            if (!inDate.HasValue || !outDate.HasValue)
            {
                MessageBox.Show("Please specify both check-in and check-out dates.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (outDate.Value.Date <= inDate.Value.Date)
            {
                MessageBox.Show("Check-out date must be strictly after the check-in date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpCheckOut.Focus();
                return;
            }

            if (CmbRoom.SelectedItem is not Room selectedRoom)
            {
                MessageBox.Show("Please select an available room.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbRoom.Focus();
                return;
            }

            if (!int.TryParse(TxtNumGuests.Text?.Trim(), out var guests) || guests <= 0)
            {
                MessageBox.Show("Please enter a valid number of guests (at least 1).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNumGuests.Focus();
                return;
            }

            if (guests > selectedRoom.Capacity)
            {
                var prompt = MessageBox.Show(
                    $"Warning: Number of guests ({guests}) exceeds the room capacity ({selectedRoom.Capacity}).\n\nDo you want to proceed anyway?",
                    "Capacity Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (prompt != MessageBoxResult.Yes) return;
            }

            int nights = (int)(outDate.Value.Date - inDate.Value.Date).TotalDays;
            decimal rate = decimal.TryParse(TxtRate.Text?.Trim(), out var r) ? r : selectedRoom.PricePerNight;
            decimal disc = decimal.TryParse(TxtDiscount.Text?.Trim(), out var d) ? d : 0m;
            decimal roomCost = nights * rate;
            decimal total = Math.Max(0, roomCost - disc);

            // Populate Reservation model
            Reservation.GuestId = selectedGuest.GuestId;
            Reservation.RoomId = selectedRoom.RoomId;
            Reservation.CheckInDate = inDate.Value.Date;
            Reservation.CheckOutDate = outDate.Value.Date;
            Reservation.NumberOfGuests = guests;
            Reservation.NumberOfNights = nights;
            Reservation.PricePerNight = rate;
            Reservation.Discount = disc;
            Reservation.TotalAmount = total;
            Reservation.SpecialRequests = TxtRequests.Text?.Trim() ?? string.Empty;
            Reservation.GuestName = selectedGuest.FullName;
            Reservation.GuestPhone = selectedGuest.Phone;
            Reservation.RoomNumber = selectedRoom.RoomNumber;
            Reservation.RoomType = selectedRoom.RoomType;

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
