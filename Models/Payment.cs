using System;

namespace HotelEase.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int ReservationId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, UPI
        public string PaymentStatus { get; set; } = "Paid"; // Paid, Partially Paid, Pending
        public string Notes { get; set; } = string.Empty;

        // Joined View Properties
        public string BookingReference { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal ReservationTotal { get; set; }
    }
}
