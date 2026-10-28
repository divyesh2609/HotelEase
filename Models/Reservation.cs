using System;

namespace HotelEase.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public int GuestId { get; set; }
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; } = DateTime.Today;
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);
        public int NumberOfGuests { get; set; } = 1;
        public int NumberOfNights { get; set; } = 1;
        public decimal PricePerNight { get; set; } = 0.0m;
        public decimal Discount { get; set; } = 0.0m;
        public decimal AdditionalCharges { get; set; } = 0.0m;
        public decimal TotalAmount { get; set; } = 0.0m;
        public string BookingStatus { get; set; } = "Confirmed"; // Confirmed, Checked In, Checked Out, Cancelled
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ActualCheckInTime { get; set; }
        public DateTime? ActualCheckOutTime { get; set; }

        // Navigation and Joined View Properties
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; } = 0.0m;

        private decimal _balanceAmount;
        public decimal BalanceAmount
        {
            get => Math.Max(0, TotalAmount - PaidAmount);
            set => _balanceAmount = value;
        }

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Partially Paid, Paid

        private string _formattedDates = string.Empty;
        public string FormattedDates
        {
            get => !string.IsNullOrEmpty(_formattedDates) 
                ? _formattedDates 
                : $"{CheckInDate:MMM dd, yyyy} - {CheckOutDate:MMM dd, yyyy} ({NumberOfNights} {(NumberOfNights == 1 ? "night" : "nights")})";
            set => _formattedDates = value;
        }
    }
}
