using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Models;
using HotelEase.Services;

namespace HotelEase.ViewModels
{
    public class RoomsViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService;

        private string _searchText = string.Empty;
        private string _selectedTypeFilter = "All";
        private string _selectedStatusFilter = "All";
        private Room? _selectedRoom;
        private bool _isLoading;

        public ObservableCollection<Room> Rooms { get; } = new();
        public ICollectionView RoomsView { get; }

        public List<string> RoomTypeFilters { get; } = new() { "All", "Standard", "Deluxe", "Suite", "Family" };
        public List<string> RoomStatusFilters { get; } = new() { "All", "Available", "Occupied", "Reserved", "Maintenance" };

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RoomsView.Refresh();
                }
            }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    RoomsView.Refresh();
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
                    RoomsView.Refresh();
                }
            }
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand ToggleMaintenanceCommand { get; }

        public Func<Room?, Task<bool>>? ShowRoomDialogAsync { get; set; }

        public RoomsViewModel(IRoomService roomService)
        {
            _roomService = roomService;

            RoomsView = CollectionViewSource.GetDefaultView(Rooms);
            RoomsView.Filter = FilterRooms;

            RefreshCommand = new AsyncRelayCommand(LoadRoomsAsync);
            AddRoomCommand = new AsyncRelayCommand(OnAddRoomAsync);
            EditRoomCommand = new AsyncRelayCommand(OnEditRoomAsync, () => SelectedRoom != null);
            DeleteRoomCommand = new AsyncRelayCommand(OnDeleteRoomAsync, () => SelectedRoom != null);
            ToggleMaintenanceCommand = new AsyncRelayCommand(OnToggleMaintenanceAsync, () => SelectedRoom != null);
        }

        public async Task LoadRoomsAsync()
        {
            try
            {
                IsLoading = true;
                await _roomService.RecalculateRoomStatusesAsync();
                var list = await _roomService.GetAllRoomsAsync(includeInactive: false);

                Rooms.Clear();
                foreach (var r in list)
                {
                    Rooms.Add(r);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterRooms(object obj)
        {
            if (obj is not Room room) return false;

            // Room type filter
            if (SelectedTypeFilter != "All" && !string.Equals(room.RoomType, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            // Status filter
            if (SelectedStatusFilter != "All" && !string.Equals(room.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            // Search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim().ToLowerInvariant();
                var numMatch = room.RoomNumber.ToLowerInvariant().Contains(query);
                var typeMatch = room.RoomType.ToLowerInvariant().Contains(query);
                var floorMatch = $"floor {room.Floor}".Contains(query) || room.Floor.ToString().Contains(query);
                if (!numMatch && !typeMatch && !floorMatch) return false;
            }

            return true;
        }

        private async Task OnAddRoomAsync()
        {
            if (ShowRoomDialogAsync != null)
            {
                var newRoom = new Room
                {
                    RoomNumber = string.Empty,
                    RoomType = "Standard",
                    Floor = 1,
                    Capacity = 2,
                    PricePerNight = 80.00m,
                    Status = "Available",
                    IsActive = true
                };

                if (await ShowRoomDialogAsync(newRoom))
                {
                    try
                    {
                        await _roomService.AddRoomAsync(newRoom);
                        await LoadRoomsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Adding Room", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async Task OnEditRoomAsync()
        {
            if (SelectedRoom == null || ShowRoomDialogAsync == null) return;

            // Clone room for editing
            var clone = new Room
            {
                RoomId = SelectedRoom.RoomId,
                RoomNumber = SelectedRoom.RoomNumber,
                RoomType = SelectedRoom.RoomType,
                Floor = SelectedRoom.Floor,
                Capacity = SelectedRoom.Capacity,
                PricePerNight = SelectedRoom.PricePerNight,
                Description = SelectedRoom.Description,
                Status = SelectedRoom.Status,
                IsActive = SelectedRoom.IsActive
            };

            if (await ShowRoomDialogAsync(clone))
            {
                try
                {
                    await _roomService.UpdateRoomAsync(clone);
                    await LoadRoomsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Updating Room", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnDeleteRoomAsync()
        {
            if (SelectedRoom == null) return;

            bool canDelete = await _roomService.CanDeleteRoomAsync(SelectedRoom.RoomId);
            if (!canDelete)
            {
                var res = MessageBox.Show(
                    $"Room {SelectedRoom.RoomNumber} has historical booking records and cannot be permanently deleted.\n\nWould you like to set its status to 'Maintenance' or deactivate it instead?",
                    "Safe Deletion Restriction",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    SelectedRoom.Status = "Maintenance";
                    await _roomService.UpdateRoomAsync(SelectedRoom);
                    await LoadRoomsAsync();
                }
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to permanently delete Room {SelectedRoom.RoomNumber}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _roomService.DeleteRoomAsync(SelectedRoom.RoomId);
                    await LoadRoomsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Deleting Room", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnToggleMaintenanceAsync()
        {
            if (SelectedRoom == null) return;

            if (SelectedRoom.Status == "Occupied")
            {
                MessageBox.Show("Cannot set an currently occupied room to Maintenance. Please check out the guest first.", "Room Occupied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string targetStatus = SelectedRoom.Status == "Maintenance" ? "Available" : "Maintenance";
            await _roomService.SetRoomStatusAsync(SelectedRoom.RoomId, targetStatus);
            await LoadRoomsAsync();
        }
    }
}
