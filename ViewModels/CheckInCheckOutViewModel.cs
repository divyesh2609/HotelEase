using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.ViewModels
{
    public class CheckInCheckOutViewModel : ViewModelBase
    {
        private readonly IReservationService _reservationService;
        private readonly IPaymentService _paymentService;

        private Reservation? _selectedCheckInItem;
        private Reservation? _selectedCheckOutItem;
        private decimal _additionalCharges;
        private string _additionalChargesNote = string.Empty;
        private bool _isLoading;

        public ObservableCollection<Reservation> PendingCheckIns { get; } = new();
        public ObservableCollection<Reservation> ActiveStays { get; } = new();

        public Reservation? SelectedCheckInItem
        {
            get => _selectedCheckInItem;
            set => SetProperty(ref _selectedCheckInItem, value);
        }

        public Reservation? SelectedCheckOutItem
        {
            get => _selectedCheckOutItem;
            set
            {
                if (SetProperty(ref _selectedCheckOutItem, value))
                {
                    AdditionalCharges = 0;
                    AdditionalChargesNote = string.Empty;
                }
            }
        }

        public decimal AdditionalCharges
        {
            get => _additionalCharges;
            set => SetProperty(ref _additionalCharges, value);
        }

        public string AdditionalChargesNote
        {
            get => _additionalChargesNote;
            set => SetProperty(ref _additionalChargesNote, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ProcessCheckInCommand { get; }
        public ICommand ProcessCheckOutCommand { get; }

        public Func<Reservation, Task>? ShowInvoiceDialogAsync { get; set; }

        public CheckInCheckOutViewModel(IReservationService reservationService, IPaymentService paymentService)
        {
            _reservationService = reservationService;
            _paymentService = paymentService;

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            ProcessCheckInCommand = new AsyncRelayCommand(OnProcessCheckInAsync, () => SelectedCheckInItem != null);
            ProcessCheckOutCommand = new AsyncRelayCommand(OnProcessCheckOutAsync, () => SelectedCheckOutItem != null);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Load all confirmed bookings
                PendingCheckIns.Clear();
                var confirmedList = await _reservationService.GetAllReservationsAsync(statusFilter: "Confirmed");
                foreach (var r in confirmedList)
                {
                    PendingCheckIns.Add(r);
                }

                // Load all checked-in active stays
                ActiveStays.Clear();
                var checkedInList = await _reservationService.GetActiveStaysAsync();
                foreach (var r in checkedInList)
                {
                    ActiveStays.Add(r);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnProcessCheckInAsync()
        {
            if (SelectedCheckInItem == null) return;

            var confirm = MessageBox.Show(
                $"Check in guest {SelectedCheckInItem.GuestName} to Room {SelectedCheckInItem.RoomNumber}?",
                "Confirm Check-in",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _reservationService.CheckInReservationAsync(SelectedCheckInItem.ReservationId);
                    await LoadDataAsync();
                    MessageBox.Show($"Check-in successful! Room {SelectedCheckInItem.RoomNumber} is now Occupied.", "Check-in Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Check-in Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnProcessCheckOutAsync()
        {
            if (SelectedCheckOutItem == null) return;

            var finalBill = SelectedCheckOutItem.TotalAmount + AdditionalCharges;
            var outstanding = finalBill - SelectedCheckOutItem.PaidAmount;

            if (outstanding > 0)
            {
                var payResult = MessageBox.Show(
                    $"Guest has an outstanding balance of ${outstanding:N2}.\n\nWould you like to record settlement payment now?",
                    "Outstanding Balance",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (payResult == MessageBoxResult.Cancel) return;

                if (payResult == MessageBoxResult.Yes)
                {
                    // Record full settlement payment
                    var payment = new Payment
                    {
                        ReservationId = SelectedCheckOutItem.ReservationId,
                        Amount = outstanding,
                        PaymentMethod = "Card",
                        PaymentStatus = "Paid",
                        Notes = $"Checkout settlement {(string.IsNullOrWhiteSpace(AdditionalChargesNote) ? "" : $"including {AdditionalChargesNote}")}"
                    };
                    await _paymentService.RecordPaymentAsync(payment);
                }
            }

            try
            {
                await _reservationService.CheckOutReservationAsync(SelectedCheckOutItem.ReservationId, AdditionalCharges);
                var checkedOutRes = SelectedCheckOutItem;

                await LoadDataAsync();

                var printResult = MessageBox.Show(
                    "Check-out completed successfully! Room has been released.\n\nWould you like to view/print the final invoice?",
                    "Check-out Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (printResult == MessageBoxResult.Yes && ShowInvoiceDialogAsync != null && checkedOutRes != null)
                {
                    await ShowInvoiceDialogAsync(checkedOutRes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Check-out Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
