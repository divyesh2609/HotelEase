using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Data;
using HotelEase.Models;
using Microsoft.Data.Sqlite;

namespace HotelEase.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IRoomService _roomService;

        public ReservationService(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task<List<Reservation>> GetAllReservationsAsync(string? searchQuery = null, string? statusFilter = null)
        {
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                sql += " AND r.BookingStatus = @status ";
                cmd.Parameters.AddWithValue("@status", statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                sql += " AND (r.BookingReference LIKE @q OR g.FullName LIKE @q OR rm.RoomNumber LIKE @q OR g.Phone LIKE @q) ";
                cmd.Parameters.AddWithValue("@q", $"%{searchQuery.Trim()}%");
            }

            sql += " ORDER BY r.CheckInDate DESC, r.ReservationId DESC;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(22);
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
                    ActualCheckInTime = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15)),
                    ActualCheckOutTime = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = paid,
                    PaymentStatus = paymentStatus
                });
            }

            return list;
        }

        public async Task<Reservation?> GetReservationByIdAsync(int reservationId)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE r.ReservationId = @id;";

            cmd.Parameters.AddWithValue("@id", reservationId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(22);
                string paymentStatus = paid >= total ? "Paid" : (paid > 0 ? "Partially Paid" : "Pending");

                return new Reservation
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
                    ActualCheckInTime = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15)),
                    ActualCheckOutTime = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = paid,
                    PaymentStatus = paymentStatus
                };
            }
            return null;
        }

        public async Task<Reservation?> GetReservationByReferenceAsync(string reference)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE UPPER(r.BookingReference) = UPPER(@ref);";

            cmd.Parameters.AddWithValue("@ref", reference.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(22);
                string paymentStatus = paid >= total ? "Paid" : (paid > 0 ? "Partially Paid" : "Pending");

                return new Reservation
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
                    ActualCheckInTime = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15)),
                    ActualCheckOutTime = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = paid,
                    PaymentStatus = paymentStatus
                };
            }
            return null;
        }

        public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, string? roomType = null, int? capacity = null, int? excludeReservationId = null)
        {
            var inStr = checkIn.ToString("yyyy-MM-dd");
            var outStr = checkOut.ToString("yyyy-MM-dd");

            var rooms = new List<Room>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT r.RoomId, r.RoomNumber, r.RoomType, r.Floor, r.Capacity, r.PricePerNight, r.Description, r.Status, r.IsActive
                FROM Rooms r
                WHERE r.IsActive = 1
                  AND r.Status != 'Maintenance'
                  AND r.RoomId NOT IN (
                      SELECT res.RoomId
                      FROM Reservations res
                      WHERE res.BookingStatus IN ('Confirmed', 'Checked In')
                        AND @inDate < res.CheckOutDate
                        AND @outDate > res.CheckInDate ";

            if (excludeReservationId.HasValue)
            {
                sql += " AND res.ReservationId != @excludeId ";
                cmd.Parameters.AddWithValue("@excludeId", excludeReservationId.Value);
            }

            sql += " ) ";

            if (!string.IsNullOrWhiteSpace(roomType) && roomType != "All")
            {
                sql += " AND r.RoomType = @type ";
                cmd.Parameters.AddWithValue("@type", roomType);
            }

            if (capacity.HasValue && capacity.Value > 0)
            {
                sql += " AND r.Capacity >= @cap ";
                cmd.Parameters.AddWithValue("@cap", capacity.Value);
            }

            sql += " ORDER BY r.RoomNumber;";
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@inDate", inStr);
            cmd.Parameters.AddWithValue("@outDate", outStr);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rooms.Add(new Room
                {
                    RoomId = reader.GetInt32(0),
                    RoomNumber = reader.GetString(1),
                    RoomType = reader.GetString(2),
                    Floor = reader.GetInt32(3),
                    Capacity = reader.GetInt32(4),
                    PricePerNight = reader.GetDecimal(5),
                    Description = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Status = reader.GetString(7),
                    IsActive = reader.GetInt32(8) == 1
                });
            }

            return rooms;
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        {
            var inStr = checkIn.ToString("yyyy-MM-dd");
            var outStr = checkOut.ToString("yyyy-MM-dd");

            using var conn = DatabaseHelper.CreateConnection();

            // First verify room exists, active, and not under maintenance
            using (var roomCheck = conn.CreateCommand())
            {
                roomCheck.CommandText = "SELECT Status, IsActive FROM Rooms WHERE RoomId = @rid;";
                roomCheck.Parameters.AddWithValue("@rid", roomId);
                using var reader = await roomCheck.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return false;
                var status = reader.GetString(0);
                var active = reader.GetInt32(1) == 1;
                if (!active || status == "Maintenance") return false;
            }

            // Check reservation overlap: [checkIn, checkOut)
            using var cmd = conn.CreateCommand();
            var sql = @"
                SELECT COUNT(*) FROM Reservations
                WHERE RoomId = @rid
                  AND BookingStatus IN ('Confirmed', 'Checked In')
                  AND @inDate < CheckOutDate
                  AND @outDate > CheckInDate ";

            if (excludeReservationId.HasValue)
            {
                sql += " AND ReservationId != @excludeId ";
                cmd.Parameters.AddWithValue("@excludeId", excludeReservationId.Value);
            }

            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@rid", roomId);
            cmd.Parameters.AddWithValue("@inDate", inStr);
            cmd.Parameters.AddWithValue("@outDate", outStr);

            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count == 0;
        }

        public async Task<string> CreateReservationAsync(Reservation res)
        {
            ValidateReservation(res);

            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Verify room availability inside transaction
                var inStr = res.CheckInDate.ToString("yyyy-MM-dd");
                var outStr = res.CheckOutDate.ToString("yyyy-MM-dd");

                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText = @"
                        SELECT COUNT(*) FROM Reservations
                        WHERE RoomId = @rid
                          AND BookingStatus IN ('Confirmed', 'Checked In')
                          AND @inDate < CheckOutDate
                          AND @outDate > CheckInDate;";
                    checkCmd.Parameters.AddWithValue("@rid", res.RoomId);
                    checkCmd.Parameters.AddWithValue("@inDate", inStr);
                    checkCmd.Parameters.AddWithValue("@outDate", outStr);

                    var overlapCount = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                    if (overlapCount > 0)
                    {
                        throw new InvalidOperationException("Selected room is already booked for these dates. Please choose another room or alternate dates.");
                    }
                }

                // Generate booking reference
                var refCode = await GenerateBookingReferenceAsync(conn, transaction);
                res.BookingReference = refCode;

                // Insert reservation
                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO Reservations (
                            BookingReference, GuestId, RoomId, CheckInDate, CheckOutDate,
                            NumberOfGuests, NumberOfNights, PricePerNight, Discount, AdditionalCharges,
                            TotalAmount, BookingStatus, SpecialRequests, CreatedAt, ActualCheckInTime, ActualCheckOutTime
                        ) VALUES (
                            @ref, @gid, @rid, @in, @out,
                            @guests, @nights, @price, @disc, @add,
                            @total, @status, @requests, @created, @actIn, @actOut
                        );
                        SELECT last_insert_rowid();";

                    insertCmd.Parameters.AddWithValue("@ref", res.BookingReference);
                    insertCmd.Parameters.AddWithValue("@gid", res.GuestId);
                    insertCmd.Parameters.AddWithValue("@rid", res.RoomId);
                    insertCmd.Parameters.AddWithValue("@in", inStr);
                    insertCmd.Parameters.AddWithValue("@out", outStr);
                    insertCmd.Parameters.AddWithValue("@guests", res.NumberOfGuests);
                    insertCmd.Parameters.AddWithValue("@nights", res.NumberOfNights);
                    insertCmd.Parameters.AddWithValue("@price", (double)res.PricePerNight);
                    insertCmd.Parameters.AddWithValue("@disc", (double)res.Discount);
                    insertCmd.Parameters.AddWithValue("@add", (double)res.AdditionalCharges);
                    insertCmd.Parameters.AddWithValue("@total", (double)res.TotalAmount);
                    insertCmd.Parameters.AddWithValue("@status", res.BookingStatus);
                    insertCmd.Parameters.AddWithValue("@requests", (object?)res.SpecialRequests ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    insertCmd.Parameters.AddWithValue("@actIn", res.ActualCheckInTime.HasValue ? res.ActualCheckInTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@actOut", res.ActualCheckOutTime.HasValue ? res.ActualCheckOutTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

                    var id = await insertCmd.ExecuteScalarAsync();
                    res.ReservationId = Convert.ToInt32(id);
                }

                transaction.Commit();

                // Recalculate room statuses
                await _roomService.RecalculateRoomStatusesAsync();

                return res.BookingReference;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateReservationAsync(Reservation res)
        {
            ValidateReservation(res);

            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                var inStr = res.CheckInDate.ToString("yyyy-MM-dd");
                var outStr = res.CheckOutDate.ToString("yyyy-MM-dd");

                // Check overlap excluding this reservation
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText = @"
                        SELECT COUNT(*) FROM Reservations
                        WHERE RoomId = @rid
                          AND ReservationId != @id
                          AND BookingStatus IN ('Confirmed', 'Checked In')
                          AND @inDate < CheckOutDate
                          AND @outDate > CheckInDate;";
                    checkCmd.Parameters.AddWithValue("@rid", res.RoomId);
                    checkCmd.Parameters.AddWithValue("@id", res.ReservationId);
                    checkCmd.Parameters.AddWithValue("@inDate", inStr);
                    checkCmd.Parameters.AddWithValue("@outDate", outStr);

                    var overlapCount = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                    if (overlapCount > 0)
                    {
                        throw new InvalidOperationException("Selected room is already booked for these modified dates. Please choose alternate dates or rooms.");
                    }
                }

                using (var updateCmd = conn.CreateCommand())
                {
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                        UPDATE Reservations
                        SET GuestId = @gid, RoomId = @rid, CheckInDate = @in, CheckOutDate = @out,
                            NumberOfGuests = @guests, NumberOfNights = @nights, PricePerNight = @price,
                            Discount = @disc, AdditionalCharges = @add, TotalAmount = @total,
                            BookingStatus = @status, SpecialRequests = @requests
                        WHERE ReservationId = @id;";

                    updateCmd.Parameters.AddWithValue("@gid", res.GuestId);
                    updateCmd.Parameters.AddWithValue("@rid", res.RoomId);
                    updateCmd.Parameters.AddWithValue("@in", inStr);
                    updateCmd.Parameters.AddWithValue("@out", outStr);
                    updateCmd.Parameters.AddWithValue("@guests", res.NumberOfGuests);
                    updateCmd.Parameters.AddWithValue("@nights", res.NumberOfNights);
                    updateCmd.Parameters.AddWithValue("@price", (double)res.PricePerNight);
                    updateCmd.Parameters.AddWithValue("@disc", (double)res.Discount);
                    updateCmd.Parameters.AddWithValue("@add", (double)res.AdditionalCharges);
                    updateCmd.Parameters.AddWithValue("@total", (double)res.TotalAmount);
                    updateCmd.Parameters.AddWithValue("@status", res.BookingStatus);
                    updateCmd.Parameters.AddWithValue("@requests", (object?)res.SpecialRequests ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@id", res.ReservationId);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                await _roomService.RecalculateRoomStatusesAsync();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CancelReservationAsync(int reservationId, string? reason = null)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                UPDATE Reservations 
                SET BookingStatus = 'Cancelled',
                    SpecialRequests = CASE WHEN @reason IS NOT NULL AND @reason != '' 
                                           THEN SpecialRequests || ' [Cancelled: ' || @reason || ']'
                                           ELSE SpecialRequests END
                WHERE ReservationId = @id AND BookingStatus = 'Confirmed';";

            cmd.Parameters.AddWithValue("@id", reservationId);
            cmd.Parameters.AddWithValue("@reason", (object?)reason ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                await _roomService.RecalculateRoomStatusesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> CheckInReservationAsync(int reservationId)
        {
            var res = await GetReservationByIdAsync(reservationId);
            if (res == null) throw new InvalidOperationException("Reservation not found.");
            if (res.BookingStatus == "Cancelled") throw new InvalidOperationException("Cannot check in a cancelled reservation.");
            if (res.BookingStatus == "Checked In") throw new InvalidOperationException("Guest is already checked in.");
            if (res.BookingStatus == "Checked Out") throw new InvalidOperationException("Reservation is already checked out.");

            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Update reservation status and actual check-in time
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        UPDATE Reservations
                        SET BookingStatus = 'Checked In',
                            ActualCheckInTime = @actIn
                        WHERE ReservationId = @id;";
                    cmd.Parameters.AddWithValue("@actIn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Update room to Occupied
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = "UPDATE Rooms SET Status = 'Occupied' WHERE RoomId = @rid;";
                    cmd.Parameters.AddWithValue("@rid", res.RoomId);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CheckOutReservationAsync(int reservationId, decimal additionalCharges = 0)
        {
            var res = await GetReservationByIdAsync(reservationId);
            if (res == null) throw new InvalidOperationException("Reservation not found.");
            if (res.BookingStatus != "Checked In")
                throw new InvalidOperationException("Cannot check out a booking that has not been checked in.");

            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                var newAddCharges = res.AdditionalCharges + additionalCharges;
                var newTotal = (res.NumberOfNights * res.PricePerNight) - res.Discount + newAddCharges;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        UPDATE Reservations
                        SET BookingStatus = 'Checked Out',
                            ActualCheckOutTime = @actOut,
                            AdditionalCharges = @add,
                            TotalAmount = @total
                        WHERE ReservationId = @id;";
                    cmd.Parameters.AddWithValue("@actOut", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@add", (double)newAddCharges);
                    cmd.Parameters.AddWithValue("@total", (double)newTotal);
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();

                // Recalculate room status based on remaining reservations
                await _roomService.RecalculateRoomStatusesAsync();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<Reservation>> GetTodayArrivalsAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE r.CheckInDate = @today AND r.BookingStatus = 'Confirmed'
                ORDER BY rm.RoomNumber;";

            cmd.Parameters.AddWithValue("@today", today);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
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
                    TotalAmount = reader.GetDecimal(11),
                    BookingStatus = reader.GetString(12),
                    SpecialRequests = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    CreatedAt = DateTime.Parse(reader.GetString(14)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = reader.GetDecimal(22)
                });
            }

            return list;
        }

        public async Task<List<Reservation>> GetTodayDeparturesAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE r.CheckOutDate = @today AND r.BookingStatus = 'Checked In'
                ORDER BY rm.RoomNumber;";

            cmd.Parameters.AddWithValue("@today", today);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
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
                    TotalAmount = reader.GetDecimal(11),
                    BookingStatus = reader.GetString(12),
                    SpecialRequests = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    CreatedAt = DateTime.Parse(reader.GetString(14)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = reader.GetDecimal(22)
                });
            }

            return list;
        }

        public async Task<List<Reservation>> GetActiveStaysAsync()
        {
            var list = new List<Reservation>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT r.ReservationId, r.BookingReference, r.GuestId, r.RoomId,
                       r.CheckInDate, r.CheckOutDate, r.NumberOfGuests, r.NumberOfNights,
                       r.PricePerNight, r.Discount, r.AdditionalCharges, r.TotalAmount,
                       r.BookingStatus, r.SpecialRequests, r.CreatedAt,
                       r.ActualCheckInTime, r.ActualCheckOutTime,
                       g.FullName, g.Phone, g.Email,
                       rm.RoomNumber, rm.RoomType,
                       COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.ReservationId = r.ReservationId), 0.0) AS PaidAmount
                FROM Reservations r
                JOIN Guests g ON r.GuestId = g.GuestId
                JOIN Rooms rm ON r.RoomId = rm.RoomId
                WHERE r.BookingStatus = 'Checked In'
                ORDER BY rm.RoomNumber;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var total = reader.GetDecimal(11);
                var paid = reader.GetDecimal(22);
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
                    ActualCheckInTime = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15)),
                    ActualCheckOutTime = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
                    GuestName = reader.GetString(17),
                    GuestPhone = reader.GetString(18),
                    GuestEmail = reader.GetString(19),
                    RoomNumber = reader.GetString(20),
                    RoomType = reader.GetString(21),
                    PaidAmount = paid,
                    PaymentStatus = paymentStatus
                });
            }

            return list;
        }

        private static async Task<string> GenerateBookingReferenceAsync(SqliteConnection conn, SqliteTransaction tx)
        {
            var year = DateTime.Now.Year;
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT COUNT(*) FROM Reservations;";
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync()) + 1001;
            return $"HE-{year}-{count}";
        }

        private static void ValidateReservation(Reservation res)
        {
            if (res.GuestId <= 0)
                throw new ArgumentException("Please select a registered guest.");

            if (res.RoomId <= 0)
                throw new ArgumentException("Please select an available room.");

            if (res.CheckInDate.Date >= res.CheckOutDate.Date)
                throw new ArgumentException("Check-out date must be strictly after the check-in date.");

            if (res.NumberOfGuests <= 0)
                throw new ArgumentException("Number of guests must be at least 1.");

            res.NumberOfNights = (int)(res.CheckOutDate.Date - res.CheckInDate.Date).TotalDays;
            if (res.NumberOfNights <= 0)
                throw new ArgumentException("Reservation duration must be at least 1 night.");

            var roomCost = res.NumberOfNights * res.PricePerNight;
            res.TotalAmount = Math.Max(0, roomCost - res.Discount + res.AdditionalCharges);
        }
    }
}
