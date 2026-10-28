using System;

namespace HotelEase.Models
{
    public class Guest
    {
        public int GuestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string IdReference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Calculated/Display helper
        public int TotalBookings { get; set; }

        private string _displayText = string.Empty;
        public string DisplayText
        {
            get => !string.IsNullOrEmpty(_displayText) ? _displayText : $"{FullName} ({Phone})";
            set => _displayText = value;
        }
    }
}
