using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace HotelEase.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync()
        {
            DatabaseHelper.EnsureDirectoryExists();

            using var connection = DatabaseHelper.CreateConnection();

            // Create tables with constraints and foreign keys
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Rooms (
                    RoomId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomNumber TEXT NOT NULL UNIQUE,
                    RoomType TEXT NOT NULL,
                    Floor INTEGER NOT NULL,
                    Capacity INTEGER NOT NULL,
                    PricePerNight REAL NOT NULL,
                    Description TEXT,
                    Status TEXT NOT NULL DEFAULT 'Available',
                    IsActive INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Guests (
                    GuestId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Address TEXT,
                    IdReference TEXT,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Reservations (
                    ReservationId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookingReference TEXT NOT NULL UNIQUE,
                    GuestId INTEGER NOT NULL,
                    RoomId INTEGER NOT NULL,
                    CheckInDate TEXT NOT NULL,
                    CheckOutDate TEXT NOT NULL,
                    NumberOfGuests INTEGER NOT NULL,
                    NumberOfNights INTEGER NOT NULL,
                    PricePerNight REAL NOT NULL,
                    Discount REAL NOT NULL DEFAULT 0.0,
                    AdditionalCharges REAL NOT NULL DEFAULT 0.0,
                    TotalAmount REAL NOT NULL,
                    BookingStatus TEXT NOT NULL DEFAULT 'Confirmed',
                    SpecialRequests TEXT,
                    CreatedAt TEXT NOT NULL,
                    ActualCheckInTime TEXT,
                    ActualCheckOutTime TEXT,
                    FOREIGN KEY(GuestId) REFERENCES Guests(GuestId) ON DELETE RESTRICT,
                    FOREIGN KEY(RoomId) REFERENCES Rooms(RoomId) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT NOT NULL UNIQUE,
                    ReservationId INTEGER NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentMethod TEXT NOT NULL,
                    PaymentStatus TEXT NOT NULL,
                    Notes TEXT,
                    FOREIGN KEY(ReservationId) REFERENCES Reservations(ReservationId) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS idx_rooms_status ON Rooms(Status);
                CREATE INDEX IF NOT EXISTS idx_reservations_dates ON Reservations(CheckInDate, CheckOutDate);
                CREATE INDEX IF NOT EXISTS idx_reservations_status ON Reservations(BookingStatus);
            ";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createTablesSql;
                await cmd.ExecuteNonQueryAsync();
            }

            // Check if demo data is needed
            long roomCount = 0;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Rooms;";
                var result = await cmd.ExecuteScalarAsync();
                roomCount = result != null ? Convert.ToInt64(result) : 0;
            }

            if (roomCount == 0)
            {
                await SeedDemoDataAsync(connection);
            }
        }

        private static async Task SeedDemoDataAsync(SqliteConnection connection)
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Seed Rooms across 4 floors
                var insertRoomSql = @"
                    INSERT INTO Rooms (RoomNumber, RoomType, Floor, Capacity, PricePerNight, Description, Status, IsActive)
                    VALUES (@num, @type, @floor, @cap, @price, @desc, @status, 1);";

                var rooms = new[]
                {
                    // Floor 1 - Standard
                    ("101", "Standard", 1, 2, 75.00, "Cozy ground-floor room with queen bed, Wi-Fi, and workspace.", "Reserved"),
                    ("102", "Standard", 1, 2, 75.00, "Comfortable standard room overlooking the serene courtyard garden.", "Available"),
                    ("103", "Standard", 1, 1, 65.00, "Single executive room designed for solo travelers and business guests.", "Available"),
                    ("104", "Standard", 1, 2, 75.00, "Standard double room with contemporary decor and smart TV.", "Available"),

                    // Floor 2 - Deluxe
                    ("201", "Deluxe", 2, 2, 120.00, "Spacious deluxe room with private balcony and skyline views.", "Occupied"),
                    ("202", "Deluxe", 2, 3, 135.00, "Deluxe king room with extra sofa bed and espresso machine.", "Available"),
                    ("203", "Deluxe", 2, 2, 120.00, "Deluxe room featuring a deep soaking tub and king-size bed.", "Available"),
                    ("204", "Deluxe", 2, 2, 120.00, "Deluxe room with panoramic views of the city skyline.", "Available"),

                    // Floor 3 - Suite
                    ("301", "Suite", 3, 4, 220.00, "Executive suite featuring a separate living room, dining area, and kitchenette.", "Occupied"),
                    ("302", "Suite", 3, 4, 250.00, "Presidential luxury suite with private jacuzzi and personal lounge.", "Available"),
                    ("303", "Suite", 3, 3, 210.00, "Junior suite with master bedroom, workspace, and bathrobes.", "Available"),
                    ("304", "Suite", 3, 4, 220.00, "Corner executive suite with wraparound terrace windows.", "Available"),

                    // Floor 4 - Family
                    ("401", "Family", 4, 5, 180.00, "Family suite with two queen beds, pull-out sofa, and play space.", "Available"),
                    ("402", "Family", 4, 4, 170.00, "Connected family suite with dual bathrooms and entertainment center.", "Available"),
                    ("403", "Family", 4, 6, 195.00, "Grand family quarters with full dining setup and kitchenette.", "Maintenance"),
                    ("404", "Family", 4, 4, 170.00, "Comfortable family lodging with smart streaming and scenic balcony.", "Available")
                };

                foreach (var r in rooms)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertRoomSql;
                    cmd.Parameters.AddWithValue("@num", r.Item1);
                    cmd.Parameters.AddWithValue("@type", r.Item2);
                    cmd.Parameters.AddWithValue("@floor", r.Item3);
                    cmd.Parameters.AddWithValue("@cap", r.Item4);
                    cmd.Parameters.AddWithValue("@price", r.Item5);
                    cmd.Parameters.AddWithValue("@desc", r.Item6);
                    cmd.Parameters.AddWithValue("@status", r.Item7);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Seed Guests
                var insertGuestSql = @"
                    INSERT INTO Guests (FullName, Phone, Email, Address, IdReference, CreatedAt)
                    VALUES (@name, @phone, @email, @addr, @idRef, @created);";

                var guests = new[]
                {
                    ("Alexander Vance", "+1 (555) 234-5678", "a.vance@example.com", "742 Evergreen Terrace, Springfield, OR", "DL-OR-992384"),
                    ("Dr. Elena Rostova", "+1 (555) 345-6789", "elena.rostova@example.com", "100 Pine Crest Way, Seattle, WA", "PASS-USA-823901"),
                    ("Vikram Malhotra", "+1 (555) 456-7890", "vikram.m@example.com", "45 Austin Boulevard, Austin, TX", "DL-TX-448291"),
                    ("Sarah Jenkins", "+1 (555) 567-8901", "sarah.jenkins@example.com", "12 Ocean Drive, Miami, FL", "DL-FL-772910"),
                    ("David Chen", "+1 (555) 678-9012", "david.chen@example.com", "88 Market Street, San Francisco, CA", "PASS-USA-419082"),
                    ("Priya Nair", "+1 (555) 789-0123", "priya.nair@example.com", "230 Michigan Ave, Chicago, IL", "DL-IL-551920")
                };

                foreach (var g in guests)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertGuestSql;
                    cmd.Parameters.AddWithValue("@name", g.Item1);
                    cmd.Parameters.AddWithValue("@phone", g.Item2);
                    cmd.Parameters.AddWithValue("@email", g.Item3);
                    cmd.Parameters.AddWithValue("@addr", g.Item4);
                    cmd.Parameters.AddWithValue("@idRef", g.Item5);
                    cmd.Parameters.AddWithValue("@created", DateTime.Today.AddDays(-15).ToString("yyyy-MM-dd HH:mm:ss"));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Seed Reservations
                var today = DateTime.Today;

                var insertResSql = @"
                    INSERT INTO Reservations (
                        BookingReference, GuestId, RoomId, CheckInDate, CheckOutDate,
                        NumberOfGuests, NumberOfNights, PricePerNight, Discount, AdditionalCharges,
                        TotalAmount, BookingStatus, SpecialRequests, CreatedAt, ActualCheckInTime, ActualCheckOutTime
                    ) VALUES (
                        @ref, @gid, @rid, @in, @out,
                        @guests, @nights, @price, @disc, @add,
                        @total, @status, @requests, @created, @actIn, @actOut
                    );";

                // Res 1: Occupied Stay (Alexander Vance in Room 201 Deluxe)
                // Checked in yesterday, checking out in 2 days
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1001");
                    cmd.Parameters.AddWithValue("@gid", 1);
                    cmd.Parameters.AddWithValue("@rid", 5); // Room 201
                    cmd.Parameters.AddWithValue("@in", today.AddDays(-1).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(2).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 2);
                    cmd.Parameters.AddWithValue("@nights", 3);
                    cmd.Parameters.AddWithValue("@price", 120.00);
                    cmd.Parameters.AddWithValue("@disc", 20.00);
                    cmd.Parameters.AddWithValue("@add", 15.00); // Room service
                    cmd.Parameters.AddWithValue("@total", 355.00); // 3*120 - 20 + 15
                    cmd.Parameters.AddWithValue("@status", "Checked In");
                    cmd.Parameters.AddWithValue("@requests", "Late check-in requested. Extra towels.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", today.AddDays(-1).ToString("yyyy-MM-dd 14:30:00"));
                    cmd.Parameters.AddWithValue("@actOut", DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 2: Occupied Stay (Dr. Elena Rostova in Room 301 Suite)
                // Checked in today morning, checking out in 3 days
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1002");
                    cmd.Parameters.AddWithValue("@gid", 2);
                    cmd.Parameters.AddWithValue("@rid", 9); // Room 301
                    cmd.Parameters.AddWithValue("@in", today.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(3).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 2);
                    cmd.Parameters.AddWithValue("@nights", 3);
                    cmd.Parameters.AddWithValue("@price", 220.00);
                    cmd.Parameters.AddWithValue("@disc", 0.00);
                    cmd.Parameters.AddWithValue("@add", 0.00);
                    cmd.Parameters.AddWithValue("@total", 660.00);
                    cmd.Parameters.AddWithValue("@status", "Checked In");
                    cmd.Parameters.AddWithValue("@requests", "High floor preferred. Business desk setup.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", today.ToString("yyyy-MM-dd 10:15:00"));
                    cmd.Parameters.AddWithValue("@actOut", DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 3: Confirmed Arrival Today (Vikram Malhotra in Room 101 Standard)
                // CheckIn = Today, CheckOut = Today + 2
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1003");
                    cmd.Parameters.AddWithValue("@gid", 3);
                    cmd.Parameters.AddWithValue("@rid", 1); // Room 101
                    cmd.Parameters.AddWithValue("@in", today.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(2).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 1);
                    cmd.Parameters.AddWithValue("@nights", 2);
                    cmd.Parameters.AddWithValue("@price", 75.00);
                    cmd.Parameters.AddWithValue("@disc", 10.00);
                    cmd.Parameters.AddWithValue("@add", 0.00);
                    cmd.Parameters.AddWithValue("@total", 140.00); // 2*75 - 10
                    cmd.Parameters.AddWithValue("@status", "Confirmed");
                    cmd.Parameters.AddWithValue("@requests", "Quiet room near the garden.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", DBNull.Value);
                    cmd.Parameters.AddWithValue("@actOut", DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 4: Upcoming Confirmed (Sarah Jenkins in Room 102 Standard)
                // CheckIn = Today + 2, CheckOut = Today + 5
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1004");
                    cmd.Parameters.AddWithValue("@gid", 4);
                    cmd.Parameters.AddWithValue("@rid", 2); // Room 102
                    cmd.Parameters.AddWithValue("@in", today.AddDays(2).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(5).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 2);
                    cmd.Parameters.AddWithValue("@nights", 3);
                    cmd.Parameters.AddWithValue("@price", 75.00);
                    cmd.Parameters.AddWithValue("@disc", 0.00);
                    cmd.Parameters.AddWithValue("@add", 0.00);
                    cmd.Parameters.AddWithValue("@total", 225.00);
                    cmd.Parameters.AddWithValue("@status", "Confirmed");
                    cmd.Parameters.AddWithValue("@requests", "Airport shuttle required at 3 PM.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", DBNull.Value);
                    cmd.Parameters.AddWithValue("@actOut", DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 5: Checked Out Today (David Chen in Room 202 Deluxe)
                // CheckIn = Today - 3, CheckOut = Today
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1005");
                    cmd.Parameters.AddWithValue("@gid", 5);
                    cmd.Parameters.AddWithValue("@rid", 6); // Room 202
                    cmd.Parameters.AddWithValue("@in", today.AddDays(-3).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 2);
                    cmd.Parameters.AddWithValue("@nights", 3);
                    cmd.Parameters.AddWithValue("@price", 135.00);
                    cmd.Parameters.AddWithValue("@disc", 15.00);
                    cmd.Parameters.AddWithValue("@add", 25.00); // Laundry
                    cmd.Parameters.AddWithValue("@total", 415.00); // 3*135 - 15 + 25
                    cmd.Parameters.AddWithValue("@status", "Checked Out");
                    cmd.Parameters.AddWithValue("@requests", "Early morning coffee service.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", today.AddDays(-3).ToString("yyyy-MM-dd 15:00:00"));
                    cmd.Parameters.AddWithValue("@actOut", today.ToString("yyyy-MM-dd 10:00:00"));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 6: Historical Past Completed Stay (Priya Nair in Room 401 Family)
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1006");
                    cmd.Parameters.AddWithValue("@gid", 6);
                    cmd.Parameters.AddWithValue("@rid", 13); // Room 401
                    cmd.Parameters.AddWithValue("@in", today.AddDays(-10).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(-6).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 4);
                    cmd.Parameters.AddWithValue("@nights", 4);
                    cmd.Parameters.AddWithValue("@price", 180.00);
                    cmd.Parameters.AddWithValue("@disc", 50.00);
                    cmd.Parameters.AddWithValue("@add", 30.00);
                    cmd.Parameters.AddWithValue("@total", 700.00);
                    cmd.Parameters.AddWithValue("@status", "Checked Out");
                    cmd.Parameters.AddWithValue("@requests", "Baby crib needed.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-14).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", today.AddDays(-10).ToString("yyyy-MM-dd 13:00:00"));
                    cmd.Parameters.AddWithValue("@actOut", today.AddDays(-6).ToString("yyyy-MM-dd 11:30:00"));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Res 7: Cancelled Stay
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertResSql;
                    cmd.Parameters.AddWithValue("@ref", "HE-2026-1007");
                    cmd.Parameters.AddWithValue("@gid", 3);
                    cmd.Parameters.AddWithValue("@rid", 4); // Room 104
                    cmd.Parameters.AddWithValue("@in", today.AddDays(-2).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@out", today.AddDays(1).ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@guests", 2);
                    cmd.Parameters.AddWithValue("@nights", 3);
                    cmd.Parameters.AddWithValue("@price", 75.00);
                    cmd.Parameters.AddWithValue("@disc", 0.00);
                    cmd.Parameters.AddWithValue("@add", 0.00);
                    cmd.Parameters.AddWithValue("@total", 225.00);
                    cmd.Parameters.AddWithValue("@status", "Cancelled");
                    cmd.Parameters.AddWithValue("@requests", "Emergency trip cancellation by guest.");
                    cmd.Parameters.AddWithValue("@created", today.AddDays(-8).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@actIn", DBNull.Value);
                    cmd.Parameters.AddWithValue("@actOut", DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Seed Payments
                var insertPaymentSql = @"
                    INSERT INTO Payments (InvoiceNumber, ReservationId, PaymentDate, Amount, PaymentMethod, PaymentStatus, Notes)
                    VALUES (@inv, @resId, @date, @amount, @method, @status, @notes);";

                // Payment for Res 1 (Alexander Vance - Partially Paid deposit)
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertPaymentSql;
                    cmd.Parameters.AddWithValue("@inv", "INV-2026-0001");
                    cmd.Parameters.AddWithValue("@resId", 1);
                    cmd.Parameters.AddWithValue("@date", today.AddDays(-1).ToString("yyyy-MM-dd 14:35:00"));
                    cmd.Parameters.AddWithValue("@amount", 200.00);
                    cmd.Parameters.AddWithValue("@method", "Card");
                    cmd.Parameters.AddWithValue("@status", "Partially Paid");
                    cmd.Parameters.AddWithValue("@notes", "Advance deposit paid upon check-in.");
                    await cmd.ExecuteNonQueryAsync();
                }

                // Payment for Res 2 (Dr. Elena Rostova - Full Paid via UPI)
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertPaymentSql;
                    cmd.Parameters.AddWithValue("@inv", "INV-2026-0002");
                    cmd.Parameters.AddWithValue("@resId", 2);
                    cmd.Parameters.AddWithValue("@date", today.ToString("yyyy-MM-dd 10:20:00"));
                    cmd.Parameters.AddWithValue("@amount", 660.00);
                    cmd.Parameters.AddWithValue("@method", "UPI");
                    cmd.Parameters.AddWithValue("@status", "Paid");
                    cmd.Parameters.AddWithValue("@notes", "Paid in full via corporate UPI transfer.");
                    await cmd.ExecuteNonQueryAsync();
                }

                // Payment for Res 5 (David Chen - Paid in Full upon checkout)
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertPaymentSql;
                    cmd.Parameters.AddWithValue("@inv", "INV-2026-0003");
                    cmd.Parameters.AddWithValue("@resId", 5);
                    cmd.Parameters.AddWithValue("@date", today.ToString("yyyy-MM-dd 10:05:00"));
                    cmd.Parameters.AddWithValue("@amount", 415.00);
                    cmd.Parameters.AddWithValue("@method", "Card");
                    cmd.Parameters.AddWithValue("@status", "Paid");
                    cmd.Parameters.AddWithValue("@notes", "Final bill settled with card at checkout.");
                    await cmd.ExecuteNonQueryAsync();
                }

                // Payment for Res 6 (Priya Nair - Paid in Full Cash)
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = insertPaymentSql;
                    cmd.Parameters.AddWithValue("@inv", "INV-2026-0004");
                    cmd.Parameters.AddWithValue("@resId", 6);
                    cmd.Parameters.AddWithValue("@date", today.AddDays(-6).ToString("yyyy-MM-dd 11:35:00"));
                    cmd.Parameters.AddWithValue("@amount", 700.00);
                    cmd.Parameters.AddWithValue("@method", "Cash");
                    cmd.Parameters.AddWithValue("@status", "Paid");
                    cmd.Parameters.AddWithValue("@notes", "Cash settlement on departure.");
                    await cmd.ExecuteNonQueryAsync();
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
