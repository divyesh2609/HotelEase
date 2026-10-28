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
    public class GuestsViewModel : ViewModelBase
    {
        private readonly IGuestService _guestService;

        private string _searchText = string.Empty;
        private Guest? _selectedGuest;
        private bool _isLoading;

        public ObservableCollection<Guest> Guests { get; } = new();
        public ICollectionView GuestsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    GuestsView.Refresh();
                }
            }
        }

        public Guest? SelectedGuest
        {
            get => _selectedGuest;
            set => SetProperty(ref _selectedGuest, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddGuestCommand { get; }
        public ICommand EditGuestCommand { get; }
        public ICommand DeleteGuestCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        public Func<Guest?, Task<bool>>? ShowGuestDialogAsync { get; set; }
        public Func<Guest, Task>? ShowHistoryDialogAsync { get; set; }

        public GuestsViewModel(IGuestService guestService)
        {
            _guestService = guestService;

            GuestsView = CollectionViewSource.GetDefaultView(Guests);
            GuestsView.Filter = FilterGuests;

            RefreshCommand = new AsyncRelayCommand(LoadGuestsAsync);
            AddGuestCommand = new AsyncRelayCommand(OnAddGuestAsync);
            EditGuestCommand = new AsyncRelayCommand(OnEditGuestAsync, () => SelectedGuest != null);
            DeleteGuestCommand = new AsyncRelayCommand(OnDeleteGuestAsync, () => SelectedGuest != null);
            ViewHistoryCommand = new AsyncRelayCommand(OnViewHistoryAsync, () => SelectedGuest != null);
        }

        public async Task LoadGuestsAsync()
        {
            try
            {
                IsLoading = true;
                var list = await _guestService.GetAllGuestsAsync();

                Guests.Clear();
                foreach (var g in list)
                {
                    Guests.Add(g);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterGuests(object obj)
        {
            if (obj is not Guest guest) return false;

            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var q = SearchText.Trim().ToLowerInvariant();
            return guest.FullName.ToLowerInvariant().Contains(q) ||
                   guest.Phone.ToLowerInvariant().Contains(q) ||
                   guest.Email.ToLowerInvariant().Contains(q) ||
                   guest.GuestId.ToString().Contains(q);
        }

        private async Task OnAddGuestAsync()
        {
            if (ShowGuestDialogAsync != null)
            {
                var newGuest = new Guest
                {
                    FullName = string.Empty,
                    Phone = string.Empty,
                    Email = string.Empty,
                    Address = string.Empty,
                    IdReference = string.Empty
                };

                if (await ShowGuestDialogAsync(newGuest))
                {
                    try
                    {
                        await _guestService.AddGuestAsync(newGuest);
                        await LoadGuestsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Adding Guest", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async Task OnEditGuestAsync()
        {
            if (SelectedGuest == null || ShowGuestDialogAsync == null) return;

            var clone = new Guest
            {
                GuestId = SelectedGuest.GuestId,
                FullName = SelectedGuest.FullName,
                Phone = SelectedGuest.Phone,
                Email = SelectedGuest.Email,
                Address = SelectedGuest.Address,
                IdReference = SelectedGuest.IdReference,
                CreatedAt = SelectedGuest.CreatedAt,
                TotalBookings = SelectedGuest.TotalBookings
            };

            if (await ShowGuestDialogAsync(clone))
            {
                try
                {
                    await _guestService.UpdateGuestAsync(clone);
                    await LoadGuestsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Updating Guest", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnDeleteGuestAsync()
        {
            if (SelectedGuest == null) return;

            bool canDelete = await _guestService.CanDeleteGuestAsync(SelectedGuest.GuestId);
            if (!canDelete)
            {
                MessageBox.Show(
                    $"Guest '{SelectedGuest.FullName}' cannot be deleted because they have booking history in the system.",
                    "Safe Deletion Restriction",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete guest '{SelectedGuest.FullName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _guestService.DeleteGuestAsync(SelectedGuest.GuestId);
                    await LoadGuestsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Deleting Guest", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnViewHistoryAsync()
        {
            if (SelectedGuest != null && ShowHistoryDialogAsync != null)
            {
                await ShowHistoryDialogAsync(SelectedGuest);
            }
        }
    }
}
