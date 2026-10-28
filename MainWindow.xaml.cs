using System.Threading.Tasks;
using System.Windows;
using HotelEase.Models;
using HotelEase.Services;
using HotelEase.ViewModels;
using HotelEase.Views.Dialogs;

namespace HotelEase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Demonstrates MVVM View-ViewModel binding, Dialog Service abstraction, and Routed Events.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();

            _mainViewModel = new MainViewModel();
            DataContext = _mainViewModel;

            // Wire up View Dialog Callbacks
            ConfigureDialogHandlers();

            Loaded += async (s, e) =>
            {
                await _mainViewModel.InitializeAsync();
            };
        }

        private void ConfigureDialogHandlers()
        {
            // Room Dialog Handler
            _mainViewModel.RoomsVM.ShowRoomDialogAsync = room =>
            {
                if (room == null) return Task.FromResult(false);
                var isEdit = room.RoomId > 0;
                var dialog = new RoomDialog(room, isEdit) { Owner = this };
                var result = dialog.ShowDialog() == true;
                return Task.FromResult(result);
            };

            // Guest Dialog Handler
            _mainViewModel.GuestsVM.ShowGuestDialogAsync = guest =>
            {
                if (guest == null) return Task.FromResult(false);
                var isEdit = guest.GuestId > 0;
                var dialog = new GuestDialog(guest, isEdit) { Owner = this };
                var result = dialog.ShowDialog() == true;
                return Task.FromResult(result);
            };

            // Guest History Dialog Handler
            _mainViewModel.GuestsVM.ShowHistoryDialogAsync = guest =>
            {
                var guestService = new GuestService();
                var dialog = new GuestHistoryDialog(guest, guestService) { Owner = this };
                dialog.ShowDialog();
                return Task.CompletedTask;
            };

            // Reservation Dialog Handler
            _mainViewModel.ReservationsVM.ShowReservationDialogAsync = res =>
            {
                if (res == null) return Task.FromResult(false);
                var roomService = new RoomService();
                var guestService = new GuestService();
                var resService = new ReservationService(roomService);

                var isEdit = res.ReservationId > 0;
                var dialog = new ReservationDialog(res, resService, guestService, roomService, isEdit) { Owner = this };
                var result = dialog.ShowDialog() == true;
                return Task.FromResult(result);
            };

            // Invoice / Folio Dialog Handlers
            _mainViewModel.ReservationsVM.ShowInvoiceDialogAsync = res =>
            {
                var dialog = new InvoicePrintDialog(res) { Owner = this };
                dialog.ShowDialog();
                return Task.CompletedTask;
            };

            _mainViewModel.CheckInCheckOutVM.ShowInvoiceDialogAsync = res =>
            {
                var dialog = new InvoicePrintDialog(res) { Owner = this };
                dialog.ShowDialog();
                return Task.CompletedTask;
            };

            _mainViewModel.PaymentsVM.ShowInvoiceDialogAsync = res =>
            {
                var dialog = new InvoicePrintDialog(res) { Owner = this };
                dialog.ShowDialog();
                return Task.CompletedTask;
            };

            // Payment Dialog Handler
            _mainViewModel.PaymentsVM.ShowPaymentDialogAsync = pay =>
            {
                if (pay == null) return Task.FromResult(false);
                var roomService = new RoomService();
                var resService = new ReservationService(roomService);
                var dialog = new PaymentDialog(pay, resService) { Owner = this };
                var result = dialog.ShowDialog() == true;
                return Task.FromResult(result);
            };
        }
    }
}
