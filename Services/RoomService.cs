using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Data;
using HotelEase.Models;
using Microsoft.Data.Sqlite;

namespace HotelEase.Services
{
    public class RoomService : IRoomService
    {
        public async Task<List<Room>> GetAllRoomsAsync(bool includeInactive = false)
        {
            var list = new List<Room>();
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = includeInactive
                ? "SELECT RoomId, RoomNumber, RoomType, Floor, Capacity, PricePerNight, Description, Status, IsActive FROM Rooms ORDER BY RoomNumber;"
                : "SELECT RoomId, RoomNumber, RoomType, Floor, Capacity, PricePerNight, Description, Status, IsActive FROM Rooms WHERE IsActive = 1 ORDER BY RoomNumber;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Room
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

            return list;
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT RoomId, RoomNumber, RoomType, Floor, Capacity, PricePerNight, Description, Status, IsActive FROM Rooms WHERE RoomId = @id;";
            cmd.Parameters.AddWithValue("@id", roomId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Room
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
                };
            }
            return null;
        }

        public async Task<bool> AddRoomAsync(Room room)
        {
            using var conn = DatabaseHelper.CreateConnection();

            // Check duplicate room number
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM Rooms WHERE RoomNumber = @num;";
                checkCmd.Parameters.AddWithValue("@num", room.RoomNumber.Trim());
                var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    throw new InvalidOperationException($"Room number '{room.RoomNumber}' already exists.");
                }
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Rooms (RoomNumber, RoomType, Floor, Capacity, PricePerNight, Description, Status, IsActive)
                VALUES (@num, @type, @floor, @cap, @price, @desc, @status, 1);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@num", room.RoomNumber.Trim());
            cmd.Parameters.AddWithValue("@type", room.RoomType);
            cmd.Parameters.AddWithValue("@floor", room.Floor);
            cmd.Parameters.AddWithValue("@cap", room.Capacity);
            cmd.Parameters.AddWithValue("@price", (double)room.PricePerNight);
            cmd.Parameters.AddWithValue("@desc", (object?)room.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", room.Status);

            var id = await cmd.ExecuteScalarAsync();
            if (id != null)
            {
                room.RoomId = Convert.ToInt32(id);
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateRoomAsync(Room room)
        {
            using var conn = DatabaseHelper.CreateConnection();

            // Check duplicate room number excluding this RoomId
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM Rooms WHERE RoomNumber = @num AND RoomId != @id;";
                checkCmd.Parameters.AddWithValue("@num", room.RoomNumber.Trim());
                checkCmd.Parameters.AddWithValue("@id", room.RoomId);
                var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    throw new InvalidOperationException($"Another room with number '{room.RoomNumber}' already exists.");
                }
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Rooms 
                SET RoomNumber = @num, RoomType = @type, Floor = @floor, Capacity = @cap, 
                    PricePerNight = @price, Description = @desc, Status = @status, IsActive = @active
                WHERE RoomId = @id;";

            cmd.Parameters.AddWithValue("@num", room.RoomNumber.Trim());
            cmd.Parameters.AddWithValue("@type", room.RoomType);
            cmd.Parameters.AddWithValue("@floor", room.Floor);
            cmd.Parameters.AddWithValue("@cap", room.Capacity);
            cmd.Parameters.AddWithValue("@price", (double)room.PricePerNight);
            cmd.Parameters.AddWithValue("@desc", (object?)room.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", room.Status);
            cmd.Parameters.AddWithValue("@active", room.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", room.RoomId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> CanDeleteRoomAsync(int roomId)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Reservations WHERE RoomId = @id;";
            cmd.Parameters.AddWithValue("@id", roomId);
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count == 0;
        }

        public async Task<bool> DeleteRoomAsync(int roomId)
        {
            if (!await CanDeleteRoomAsync(roomId))
            {
                throw new InvalidOperationException("This room has historical or active reservations attached and cannot be permanently deleted. You can mark it as 'Maintenance' or deactivate it instead.");
            }

            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Rooms WHERE RoomId = @id;";
            cmd.Parameters.AddWithValue("@id", roomId);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> SetRoomStatusAsync(int roomId, string status)
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Rooms SET Status = @status WHERE RoomId = @id;";
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", roomId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task RecalculateRoomStatusesAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.CreateConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Fetch rooms that are not in explicit Maintenance
                var rooms = new List<int>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = "SELECT RoomId FROM Rooms WHERE Status != 'Maintenance' AND IsActive = 1;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        rooms.Add(reader.GetInt32(0));
                    }
                }

                foreach (var rid in rooms)
                {
                    string newStatus = "Available";

                    // Check if checked in right now
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "SELECT COUNT(*) FROM Reservations WHERE RoomId = @rid AND BookingStatus = 'Checked In';";
                        cmd.Parameters.AddWithValue("@rid", rid);
                        var checkedInCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        if (checkedInCount > 0)
                        {
                            newStatus = "Occupied";
                        }
                    }

                    // If not occupied, check if reserved for today
                    if (newStatus == "Available")
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            SELECT COUNT(*) FROM Reservations 
                            WHERE RoomId = @rid 
                              AND BookingStatus = 'Confirmed' 
                              AND CheckInDate <= @today 
                              AND CheckOutDate > @today;";
                        cmd.Parameters.AddWithValue("@rid", rid);
                        cmd.Parameters.AddWithValue("@today", today);
                        var reservedCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        if (reservedCount > 0)
                        {
                            newStatus = "Reserved";
                        }
                    }

                    // Update room status
                    using (var updateCmd = conn.CreateCommand())
                    {
                        updateCmd.Transaction = transaction;
                        updateCmd.CommandText = "UPDATE Rooms SET Status = @status WHERE RoomId = @rid;";
                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@rid", rid);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
