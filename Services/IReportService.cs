using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Models;

namespace HotelEase.Services
{
    public interface IReportService
    {
        Task<DashboardMetrics> GetDashboardMetricsAsync();
        Task<List<RoomTypeDistribution>> GetRoomTypeDistributionAsync();
        Task<List<Reservation>> GetReservationsReportAsync(DateTime? fromDate = null, DateTime? toDate = null, string? status = null);
        Task<List<Payment>> GetRevenueReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task ExportToCsvAsync(string filePath, IEnumerable<object> items, string reportTitle);
    }
}
