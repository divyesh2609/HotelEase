using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Models;

namespace HotelEase.Services
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetAllPaymentsAsync(string? searchQuery = null);
        Task<List<Payment>> GetPaymentsForReservationAsync(int reservationId);
        Task<string> RecordPaymentAsync(Payment payment);
        Task<Payment?> GetPaymentByInvoiceAsync(string invoiceNumber);
    }
}
