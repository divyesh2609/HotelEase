using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.Views.Dialogs
{
    public class ReservationPaymentOption
    {
        public Reservation Reservation { get; set; } = null!;

        private string _formattedOption = string.Empty;
        public string FormattedOption
        {
            get => !string.IsNullOrEmpty(_formattedOption)
                ? _formattedOption
                : $"{Reservation.BookingReference} - {Reservation.GuestName} (Room {Reservation.RoomNumber}, Balance: ${Reservation.BalanceAmount:N2})";
            set => _formattedOption = value;
        }
    }

    public partial class PaymentDialog : Window
    {
        private readonly IReservationService _reservationService;
        public Payment Payment { get; }

        public PaymentDialog(Payment payment, IReservationService reservationService)
        {
            InitializeComponent();
            Payment = payment;
            _reservationService = reservationService;

            Loaded += OnWindowLoaded;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var resList = await _reservationService.GetAllReservationsAsync();
            var options = new List<ReservationPaymentOption>();

            foreach (var r in resList)
            {
                if (r.BookingStatus != "Cancelled")
                {
                    options.Add(new ReservationPaymentOption { Reservation = r });
                }
            }

            CmbReservation.ItemsSource = options;

            if (Payment.ReservationId > 0)
            {
                var match = options.Find(o => o.Reservation.ReservationId == Payment.ReservationId);
                if (match != null) CmbReservation.SelectedItem = match;
            }
            else if (options.Count > 0)
            {
                CmbReservation.SelectedIndex = 0;
            }
        }

        private void OnReservationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbReservation.SelectedItem is ReservationPaymentOption opt)
            {
                // Auto-suggest remaining balance
                TxtAmount.Text = opt.Reservation.BalanceAmount.ToString("F2");
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (CmbReservation.SelectedItem is not ReservationPaymentOption opt)
            {
                MessageBox.Show("Please select a reservation.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbReservation.Focus();
                return;
            }

            if (!decimal.TryParse(TxtAmount.Text?.Trim(), out var amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid positive payment amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAmount.Focus();
                return;
            }

            Payment.ReservationId = opt.Reservation.ReservationId;
            Payment.Amount = amount;
            Payment.PaymentMethod = (CmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Card";
            Payment.PaymentStatus = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Paid";
            Payment.Notes = TxtNotes.Text?.Trim() ?? string.Empty;

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
