using System;
using System.Windows;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.Views.Dialogs
{
    public partial class InvoicePrintDialog : Window
    {
        public InvoicePrintDialog(Reservation res)
        {
            InitializeComponent();

            // Populate folio with actual reservation data
            TxtInvoiceRef.Text = $"INV-{res.ReservationId:D4}";
            TxtInvoiceDate.Text = $"Date: {DateTime.Now:MMM dd, yyyy}";

            TxtGuestName.Text = res.GuestName;
            TxtGuestContact.Text = $"Phone: {res.GuestPhone}";
            TxtGuestEmail.Text = res.GuestEmail;

            TxtBookingRef.Text = $"Booking Ref: {res.BookingReference}";
            TxtRoomAssigned.Text = $"Room {res.RoomNumber} ({res.RoomType}) — {res.NumberOfGuests} Guests";
            TxtStayDates.Text = $"{res.CheckInDate:MMM dd, yyyy} → {res.CheckOutDate:MMM dd, yyyy} ({res.NumberOfNights} nights)";

            TxtLine1Desc.Text = $"Room Lodging ({res.RoomType} Room {res.RoomNumber})";
            TxtLine1Qty.Text = res.NumberOfNights.ToString();
            TxtLine1Rate.Text = $"${res.PricePerNight:N2}";
            TxtLine1Amount.Text = $"${res.NumberOfNights * res.PricePerNight:N2}";

            TxtLine2Amount.Text = res.Discount > 0 ? $"-${res.Discount:N2}" : "$0.00";
            TxtLine3Amount.Text = $"${res.AdditionalCharges:N2}";

            TxtSummaryTotal.Text = $"${res.TotalAmount:N2}";
            TxtSummaryPaid.Text = $"${res.PaidAmount:N2}";
            TxtSummaryBalance.Text = $"${res.BalanceAmount:N2}";
        }

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            try
            {
                bool printed = PrintService.PrintVisual(InvoicePrintArea, $"HotelEase-Folio-{TxtInvoiceRef.Text}");
                if (printed)
                {
                    MessageBox.Show("Folio sent to printer successfully.", "Print Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to print: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
