using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HotelEase.Data;
using HotelEase.Models;
using Microsoft.Data.Sqlite;

namespace HotelEase.Services
{
    public class GuestService : IGuestService
    {
        public async Task<List<Guest>> GetAllGuestsAsync(string? searchQuery = null)
        {
            var list = new List<Guest>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                cmd.CommandText = @"
                    SELECT g.GuestId, g.FullName, g.Phone, g.Email, g.Address, g.IdReference, g.CreatedAt,
                           (SELECT COUNT(*) FROM Reservations r WHERE r.GuestId = g.GuestId) AS BookingCount
                    FROM Guests g
                    ORDER BY g.FullName;";
            }
            else
            {
                cmd.CommandText = @"
                    SELECT g.GuestId, g.FullName, g.Phone, g.Email, g.Address, g.IdReference, g.CreatedAt,
                           (SELECT COUNT(*) FROM Reservations r WHERE r.GuestId = g.GuestId) AS BookingCount
                    FROM Guests g
                    WHERE g.FullName LIKE @q OR g.Phone LIKE @q OR g.Email LIKE @q OR CAST(g.GuestId AS TEXT) LIKE @q
                    ORDER BY g.FullName;";
                cmd.Parameters.AddWithValue("@q", $"%{searchQuery.Trim()}%");
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Guest
                {
                    GuestId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Phone = reader.GetString(2),
                    Email = reader.GetString(3),
                    Address = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    IdReference = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    TotalBookings = reader.GetInt32(7)
                });
            }

            return list;
        }

        public async Task<Guest?> GetGuestByIdAsync(int guestId)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT g.GuestId, g.FullName, g.Phone, g.Email, g.Address, g.IdReference, g.CreatedAt,
                       (SELECT COUNT(*) FROM Reservations r WHERE r.GuestId = g.GuestId) AS BookingCount
                FROM Guests g WHERE g.GuestId = @id;";
            cmd.Parameters.AddWithValue("@id", guestId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Guest
                {
                    GuestId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Phone = reader.GetString(2),
                    Email = reader.GetString(3),
                    Address = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    IdReference = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    TotalBookings = reader.GetInt32(7)
                };
            }
            return null;
        }

        public async Task<bool> AddGuestAsync(Guest guest)
        {
            ValidateGuest(guest);

            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Guests (FullName, Phone, Email, Address, IdReference, CreatedAt)
                VALUES (@name, @phone, @email, @addr, @idRef, @created);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@name", guest.FullName.Trim());
            cmd.Parameters.AddWithValue("@phone", guest.Phone.Trim());
            cmd.Parameters.AddWithValue("@email", guest.Email.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@addr", (object?)guest.Address?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@idRef", (object?)guest.IdReference?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var id = await cmd.ExecuteScalarAsync();
            if (id != null)
            {
                guest.GuestId = Convert.ToInt32(id);
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateGuestAsync(Guest guest)
        {
            ValidateGuest(guest);

            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Guests 
                SET FullName = @name, Phone = @phone, Email = @email, Address = @addr, IdReference = @idRef
                WHERE GuestId = @id;";

            cmd.Parameters.AddWithValue("@name", guest.FullName.Trim());
            cmd.Parameters.AddWithValue("@phone", guest.Phone.Trim());
            cmd.Parameters.AddWithValue("@email", guest.Email.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@addr", (object?)guest.Address?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@idRef", (object?)guest.IdReference?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", guest.GuestId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<List<Reservation>> GetGuestHistoryAsync(int guestId)
        {
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE r.GuestId = @gid
                ORDER BY r.CheckInDate DESC;";

            cmd.Parameters.AddWithValue("@gid", guestId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(17);
                string paymentStatus = paid >= total ? "Paid" : (paid > 0 ? "Partially Paid" : "Pending");

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
                    RoomNumber = reader.GetString(15),
                    RoomType = reader.GetString(16),
                    PaidAmount = paid,
                    PaymentStatus = paymentStatus
                });
            }

            return list;
        }

        public async Task<bool> CanDeleteGuestAsync(int guestId)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Reservations WHERE GuestId = @id;";
            cmd.Parameters.AddWithValue("@id", guestId);
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count == 0;
        }

        public async Task<bool> DeleteGuestAsync(int guestId)
        {
            if (!await CanDeleteGuestAsync(guestId))
            {
                throw new InvalidOperationException("Cannot delete this guest because they have booking records in the system.");
            }

            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Guests WHERE GuestId = @id;";
            cmd.Parameters.AddWithValue("@id", guestId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void ValidateGuest(Guest guest)
        {
            if (string.IsNullOrWhiteSpace(guest.FullName))
                throw new ArgumentException("Guest full name is required.");

            if (string.IsNullOrWhiteSpace(guest.Phone))
                throw new ArgumentException("Phone number is required.");

            if (string.IsNullOrWhiteSpace(guest.Email))
                throw new ArgumentException("Email address is required.");

            // Simple format regex for email
            if (!Regex.IsMatch(guest.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Please enter a valid email address (e.g. name@domain.com).");
        }
    }
}
