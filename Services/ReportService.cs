using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotelEase.Data;
using HotelEase.Models;
using Microsoft.Data.Sqlite;

namespace HotelEase.Services
{
    public class ReportService : IReportService
    {
        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var metrics = new DashboardMetrics();
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            using var conn = DatabaseHelper.CreateConnection();

            // Rooms breakdown
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        COUNT(*),
                        COALESCE(SUM(CASE WHEN Status = 'Available' THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN Status = 'Occupied' THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN Status = 'Reserved' THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN Status = 'Maintenance' THEN 1 ELSE 0 END), 0)
                    FROM Rooms WHERE IsActive = 1;";

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    metrics.TotalRooms = reader.GetInt32(0);
                    metrics.AvailableRooms = reader.GetInt32(1);
                    metrics.OccupiedRooms = reader.GetInt32(2);
                    metrics.ReservedRooms = reader.GetInt32(3);
                    metrics.MaintenanceRooms = reader.GetInt32(4);
                }
            }

            // Check-ins today
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Reservations WHERE CheckInDate = @today AND BookingStatus IN ('Confirmed', 'Checked In');";
                cmd.Parameters.AddWithValue("@today", today);
                metrics.TodayCheckIns = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Check-outs today
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Reservations WHERE CheckOutDate = @today AND BookingStatus IN ('Checked In', 'Checked Out');";
                cmd.Parameters.AddWithValue("@today", today);
                metrics.TodayCheckOuts = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total bookings
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Reservations;";
                metrics.TotalBookings = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total recorded revenue from payments
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(SUM(Amount), 0.0) FROM Payments;";
                var rev = await cmd.ExecuteScalarAsync();
                metrics.TotalRevenue = Convert.ToDecimal(rev);
            }

            return metrics;
        }

        public async Task<List<RoomTypeDistribution>> GetRoomTypeDistributionAsync()
        {
            var list = new List<RoomTypeDistribution>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT 
                    r.RoomType,
                    COUNT(r.RoomId) AS TotalCount,
                    COALESCE(SUM(CASE WHEN r.Status = 'Occupied' THEN 1 ELSE 0 END), 0) AS OccupiedCount,
                    COALESCE((
                        SELECT SUM(res.TotalAmount)
                        FROM Reservations res
                        JOIN Rooms rm2 ON res.RoomId = rm2.RoomId
                        WHERE rm2.RoomType = r.RoomType AND res.BookingStatus != 'Cancelled'
                    ), 0.0) AS TotalRevenue
                FROM Rooms r
                WHERE r.IsActive = 1
                GROUP BY r.RoomType
                ORDER BY TotalCount DESC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RoomTypeDistribution
                {
                    RoomType = reader.GetString(0),
                    TotalCount = reader.GetInt32(1),
                    OccupiedCount = reader.GetInt32(2),
                    TotalRevenue = reader.GetDecimal(3)
                });
            }

            return list;
        }

        public async Task<List<Reservation>> GetReservationsReportAsync(DateTime? fromDate = null, DateTime? toDate = null, string? status = null)
        {
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE 1=1 ";

            if (fromDate.HasValue)
            {
                sql += " AND r.CheckInDate >= @from ";
                cmd.Parameters.AddWithValue("@from", fromDate.Value.ToString("yyyy-MM-dd"));
            }

            if (toDate.HasValue)
            {
                sql += " AND r.CheckInDate <= @to ";
                cmd.Parameters.AddWithValue("@to", toDate.Value.ToString("yyyy-MM-dd"));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                sql += " AND r.BookingStatus = @status ";
                cmd.Parameters.AddWithValue("@status", status);
            }

            sql += " ORDER BY r.CheckInDate DESC;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(20);

                list.Add(new Reservation
                {
                    ReservationId = reader.GetInt32(0),
                    BookingReference = reader.GetString(1),
                    GuestId = reader.GetInt32(2),
                    RoomId = reader.GetInt32(3),
                    CheckInDate = DateTime.Parse(reader.GetString(4)),
                    CheckOutDate = DateTime.Parse(reader.GetString(5)),
                    NumberOfGuests = reader.GetInt32(6),
                    NumberOfNights = reader.GetInt32(7),
                    PricePerNight = reader.GetDecimal(8),
                    Discount = reader.GetDecimal(9),
                    AdditionalCharges = reader.GetDecimal(10),
                    TotalAmount = total,
                    BookingStatus = reader.GetString(12),
                    SpecialRequests = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    CreatedAt = DateTime.Parse(reader.GetString(14)),
                    GuestName = reader.GetString(15),
                    GuestPhone = reader.GetString(16),
                    GuestEmail = reader.GetString(17),
                    RoomNumber = reader.GetString(18),
                    RoomType = reader.GetString(19),
                    PaidAmount = paid,
                    PaymentStatus = paid >= total ? "Paid" : (paid > 0 ? "Partially Paid" : "Pending")
                });
            }

            return list;
        }

        public async Task<List<Payment>> GetRevenueReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<Payment>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT p.PaymentId, p.InvoiceNumber, p.ReservationId, p.PaymentDate, p.Amount,
                       p.PaymentMethod, p.PaymentStatus, p.Notes,
                       r.BookingReference, r.TotalAmount,
                       g.FullName, rm.RoomNumber
                FROM Payments p
                JOIN Reservations r ON p.ReservationId = r.ReservationId
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE 1=1 ";

            if (fromDate.HasValue)
            {
                sql += " AND p.PaymentDate >= @from ";
                cmd.Parameters.AddWithValue("@from", fromDate.Value.ToString("yyyy-MM-dd 00:00:00"));
            }

            if (toDate.HasValue)
            {
                sql += " AND p.PaymentDate <= @to ";
                cmd.Parameters.AddWithValue("@to", toDate.Value.ToString("yyyy-MM-dd 23:59:59"));
            }

            sql += " ORDER BY p.PaymentDate DESC;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Payment
                {
                    PaymentId = reader.GetInt32(0),
                    InvoiceNumber = reader.GetString(1),
                    ReservationId = reader.GetInt32(2),
                    PaymentDate = DateTime.Parse(reader.GetString(3)),
                    Amount = reader.GetDecimal(4),
                    PaymentMethod = reader.GetString(5),
                    PaymentStatus = reader.GetString(6),
                    Notes = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    BookingReference = reader.GetString(8),
                    ReservationTotal = reader.GetDecimal(9),
                    GuestName = reader.GetString(10),
                    RoomNumber = reader.GetString(11)
                });
            }

            return list;
        }

        public async Task ExportToCsvAsync(string filePath, IEnumerable<object> items, string reportTitle)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {reportTitle} - Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                sb.AppendLine("No records found.");
                await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
                return;
            }

            var firstItem = enumerator.Current;
            var properties = firstItem.GetType().GetProperties();

            // Header row
            var headers = new List<string>();
            foreach (var prop in properties)
            {
                headers.Add(EscapeCsv(prop.Name));
            }
            sb.AppendLine(string.Join(",", headers));

            // Write first item
            WriteItem(firstItem, properties, sb);

            // Write remaining items
            while (enumerator.MoveNext())
            {
                WriteItem(enumerator.Current, properties, sb);
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static void WriteItem(object item, System.Reflection.PropertyInfo[] properties, StringBuilder sb)
        {
            var values = new List<string>();
            foreach (var prop in properties)
            {
                var val = prop.GetValue(item);
                string strVal = val switch
                {
                    null => string.Empty,
                    DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                    decimal dec => dec.ToString("F2"),
                    double d => d.ToString("F2"),
                    _ => val.ToString() ?? string.Empty
                };
                values.Add(EscapeCsv(strVal));
            }
            sb.AppendLine(string.Join(",", values));
        }

        private static string EscapeCsv(string text)
        {
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }
    }
}
