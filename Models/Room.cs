using System;

namespace HotelEase.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = "Standard"; // Standard, Deluxe, Suite, Family
        public int Floor { get; set; } = 1;
        public int Capacity { get; set; } = 2;
        public decimal PricePerNight { get; set; } = 80.00m;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Reserved, Occupied, Maintenance
        public bool IsActive { get; set; } = true;

        private string _displayText = string.Empty;
        public string DisplayText
        {
            get => !string.IsNullOrEmpty(_displayText) 
                ? _displayText 
                : $"Room {RoomNumber} - {RoomType} (Floor {Floor}, Capacity: {Capacity}, ${PricePerNight:N2}/night)";
            set => _displayText = value;
        }
    }
}
