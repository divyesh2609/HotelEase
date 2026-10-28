using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Data;
using HotelEase.Models;
using Microsoft.Data.Sqlite;

namespace HotelEase.Services
{
    public class PaymentService : IPaymentService
    {
        public async Task<List<Payment>> GetAllPaymentsAsync(string? searchQuery = null)
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

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                sql += " AND (p.InvoiceNumber LIKE @q OR r.BookingReference LIKE @q OR g.FullName LIKE @q OR p.PaymentMethod LIKE @q) ";
                cmd.Parameters.AddWithValue("@q", $"%{searchQuery.Trim()}%");
            }

            sql += " ORDER BY p.PaymentDate DESC, p.PaymentId DESC;";
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

        public async Task<List<Payment>> GetPaymentsForReservationAsync(int reservationId)
        {
            var list = new List<Payment>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT p.PaymentId, p.InvoiceNumber, p.ReservationId, p.PaymentDate, p.Amount,
                       p.PaymentMethod, p.PaymentStatus, p.Notes,
                       r.BookingReference, r.TotalAmount,
                       g.FullName, rm.RoomNumber
                FROM Payments p
                JOIN Reservations r ON p.ReservationId = r.ReservationId
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE p.ReservationId = @id
                ORDER BY p.PaymentDate DESC;";

            cmd.Parameters.AddWithValue("@id", reservationId);

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

        public async Task<string> RecordPaymentAsync(Payment payment)
        {
            if (payment.ReservationId <= 0)
                throw new ArgumentException("Valid reservation selection is required.");

            if (payment.Amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.");

            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Generate Invoice Number
                var year = DateTime.Now.Year;
                using var countCmd = conn.CreateCommand();
                countCmd.Transaction = transaction;
                countCmd.CommandText = "SELECT COUNT(*) FROM Payments;";
                var count = Convert.ToInt64(await countCmd.ExecuteScalarAsync()) + 1;
                payment.InvoiceNumber = $"INV-{year}-{count:D4}";

                // Insert Payment
                using var insertCmd = conn.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO Payments (InvoiceNumber, ReservationId, PaymentDate, Amount, PaymentMethod, PaymentStatus, Notes)
                    VALUES (@inv, @resId, @date, @amount, @method, @status, @notes);
                    SELECT last_insert_rowid();";

                insertCmd.Parameters.AddWithValue("@inv", payment.InvoiceNumber);
                insertCmd.Parameters.AddWithValue("@resId", payment.ReservationId);
                insertCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                insertCmd.Parameters.AddWithValue("@amount", (double)payment.Amount);
                insertCmd.Parameters.AddWithValue("@method", payment.PaymentMethod);
                insertCmd.Parameters.AddWithValue("@status", payment.PaymentStatus);
                insertCmd.Parameters.AddWithValue("@notes", (object?)payment.Notes ?? DBNull.Value);

                var id = await insertCmd.ExecuteScalarAsync();
                payment.PaymentId = Convert.ToInt32(id);

                transaction.Commit();
                return payment.InvoiceNumber;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByInvoiceAsync(string invoiceNumber)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT p.PaymentId, p.InvoiceNumber, p.ReservationId, p.PaymentDate, p.Amount,
                       p.PaymentMethod, p.PaymentStatus, p.Notes,
                       r.BookingReference, r.TotalAmount,
                       g.FullName, rm.RoomNumber
                FROM Payments p
                JOIN Reservations r ON p.ReservationId = r.ReservationId
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE UPPER(p.InvoiceNumber) = UPPER(@inv);";

            cmd.Parameters.AddWithValue("@inv", invoiceNumber.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Payment
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
                };
            }
            return null;
        }
    }
}
