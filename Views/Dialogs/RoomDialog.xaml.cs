using System;
using System.Windows;
using System.Windows.Controls;
using HotelEase.Models;

namespace HotelEase.Views.Dialogs
{
    public partial class RoomDialog : Window
    {
        public Room Room { get; }
        private readonly bool _isEdit;

        public RoomDialog(Room room, bool isEdit = false)
        {
            InitializeComponent();
            Room = room;
            _isEdit = isEdit;

            TxtTitle.Text = _isEdit ? "Edit Room Details" : "Add New Room";

            // Populate fields
            TxtRoomNumber.Text = room.RoomNumber;
            TxtFloor.Text = room.Floor.ToString();
            TxtCapacity.Text = room.Capacity.ToString();
            TxtPrice.Text = room.PricePerNight.ToString("F2");
            TxtDescription.Text = room.Description;

            // Select Room Type
            foreach (ComboBoxItem item in CmbRoomType.Items)
            {
                if (string.Equals(item.Content?.ToString(), room.RoomType, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Select Status
            foreach (ComboBoxItem item in CmbStatus.Items)
            {
                if (string.Equals(item.Content?.ToString(), room.Status, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                    break;
                }
            }

            if (_isEdit)
            {
                // In edit mode, don't allow accidental room number rename if not needed, but keep editable with check
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var roomNum = TxtRoomNumber.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomNum))
            {
                MessageBox.Show("Please enter a room number (e.g. 101, 202).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtRoomNumber.Focus();
                return;
            }

            if (!int.TryParse(TxtFloor.Text?.Trim(), out var floor) || floor < 0 || floor > 100)
            {
                MessageBox.Show("Please enter a valid floor number (0 to 100).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFloor.Focus();
                return;
            }

            if (!int.TryParse(TxtCapacity.Text?.Trim(), out var cap) || cap <= 0 || cap > 20)
            {
                MessageBox.Show("Please enter a valid room capacity (1 to 20 guests).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCapacity.Focus();
                return;
            }

            if (!decimal.TryParse(TxtPrice.Text?.Trim(), out var price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid positive price per night.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrice.Focus();
                return;
            }

            // Apply values
            Room.RoomNumber = roomNum;
            Room.Floor = floor;
            Room.Capacity = cap;
            Room.PricePerNight = price;
            Room.Description = TxtDescription.Text?.Trim() ?? string.Empty;
            Room.RoomType = (CmbRoomType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Standard";
            Room.Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Available";

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
