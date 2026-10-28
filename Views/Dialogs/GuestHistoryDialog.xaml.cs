using System.Windows;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.Views.Dialogs
{
    public partial class GuestHistoryDialog : Window
    {
        private readonly IGuestService _guestService;

        public GuestHistoryDialog(Guest guest, IGuestService guestService)
        {
            InitializeComponent();
            _guestService = guestService;

            TxtGuestName.Text = guest.FullName;
            TxtGuestPhone.Text = guest.Phone;
            TxtGuestEmail.Text = guest.Email;
            TxtTotalBookingsCount.Text = $"Total Bookings: {guest.TotalBookings}";
            TxtGuestSince.Text = $"Member Since: {guest.CreatedAt:MMM yyyy}";

            Loaded += async (s, e) =>
            {
                var history = await _guestService.GetGuestHistoryAsync(guest.GuestId);
                GridHistory.ItemsSource = history;
            };
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
