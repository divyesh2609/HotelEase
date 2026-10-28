using System;
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
    public class PaymentsViewModel : ViewModelBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IReservationService _reservationService;

        private string _searchText = string.Empty;
        private Payment? _selectedPayment;
        private decimal _totalCollected;
        private bool _isLoading;

        public ObservableCollection<Payment> Payments { get; } = new();
        public ICollectionView PaymentsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PaymentsView.Refresh();
                }
            }
        }

        public Payment? SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        public decimal TotalCollected
        {
            get => _totalCollected;
            set => SetProperty(ref _totalCollected, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand RecordPaymentCommand { get; }
        public ICommand ViewInvoiceCommand { get; }

        public Func<Payment?, Task<bool>>? ShowPaymentDialogAsync { get; set; }
        public Func<Reservation, Task>? ShowInvoiceDialogAsync { get; set; }

        public PaymentsViewModel(IPaymentService paymentService, IReservationService reservationService)
        {
            _paymentService = paymentService;
            _reservationService = reservationService;

            PaymentsView = CollectionViewSource.GetDefaultView(Payments);
            PaymentsView.Filter = FilterPayments;

            RefreshCommand = new AsyncRelayCommand(LoadPaymentsAsync);
            RecordPaymentCommand = new AsyncRelayCommand(OnRecordPaymentAsync);
            ViewInvoiceCommand = new AsyncRelayCommand(OnViewInvoiceAsync, () => SelectedPayment != null);
        }

        public async Task LoadPaymentsAsync()
        {
            try
            {
                IsLoading = true;
                var list = await _paymentService.GetAllPaymentsAsync();

                Payments.Clear();
                decimal total = 0;
                foreach (var p in list)
                {
                    Payments.Add(p);
                    total += p.Amount;
                }
                TotalCollected = total;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterPayments(object obj)
        {
            if (obj is not Payment p) return false;

            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var q = SearchText.Trim().ToLowerInvariant();
            return p.InvoiceNumber.ToLowerInvariant().Contains(q) ||
                   p.BookingReference.ToLowerInvariant().Contains(q) ||
                   p.GuestName.ToLowerInvariant().Contains(q) ||
                   p.PaymentMethod.ToLowerInvariant().Contains(q);
        }

        private async Task OnRecordPaymentAsync()
        {
            if (ShowPaymentDialogAsync != null)
            {
                var newPay = new Payment
                {
                    Amount = 0,
                    PaymentMethod = "Card",
                    PaymentStatus = "Paid"
                };

                if (await ShowPaymentDialogAsync(newPay))
                {
                    try
                    {
                        var inv = await _paymentService.RecordPaymentAsync(newPay);
                        await LoadPaymentsAsync();
                        MessageBox.Show($"Payment recorded successfully!\n\nInvoice: {inv}", "Payment Processed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Processing Payment", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async Task OnViewInvoiceAsync()
        {
            if (SelectedPayment == null || ShowInvoiceDialogAsync == null) return;

            var res = await _reservationService.GetReservationByIdAsync(SelectedPayment.ReservationId);
            if (res != null)
            {
                await ShowInvoiceDialogAsync(res);
            }
            else
            {
                MessageBox.Show("Linked reservation not found.", "Invoice Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
