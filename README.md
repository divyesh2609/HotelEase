# HotelEase — Hotel Room Booking and Management System

A modern Windows desktop application built with **C#**, **WPF (XAML)**, **.NET 8.0**, and **SQLite**.

Designed and structured as a fifth-semester B.Tech Computer Science and Engineering mini-project for the subject **Windows Application Development using WPF and .NET**, adhering to curriculum concepts, design patterns, and viva voce presentation requirements.

---

## 1. Project Overview

### 1.1 Problem Statement
Traditional hospitality businesses and mid-sized hotels frequently struggle with manual reservation registers, accidental double-bookings, fragmented billing calculations, and disconnected front-desk workflows. Commercial property management systems are often expensive, bloated, web-dependent, or require internet connectivity.

### 1.2 Solution & Objectives
**HotelEase** delivers an offline-first, Windows desktop solution featuring:
- Real-time room occupancy and inventory tracking.
- Parameterized SQLite persistent storage with ACID transaction guarantees.
- Comprehensive booking overlap detection engine across `[check-in, check-out)` intervals.
- Express front-desk workflow for seamless arrivals check-in and checkout folio settlement.
- Offline payment simulation supporting Cash, Card, and UPI with printable official folios.
- Business reporting with dynamic filtering and CSV data export.

---

## 2. Key Modules & Features

```
┌────────────────────────────────────────────────────────────────────────┐
│                        HOTELEASE DASHBOARD                             │
├───────────────┬────────────────────────────────────────────────────────┤
│ 1. Dashboard  │ 6 KPI summary cards, occupancy visual progress bar,   │
│               │ quick action shortcuts, and live recent booking feed.  │
├───────────────┼────────────────────────────────────────────────────────┤
│ 2. Rooms      │ Filter by Standard, Deluxe, Suite, Family; status      │
│               │ indicators; price & capacity validation; safe delete.  │
├───────────────┼────────────────────────────────────────────────────────┤
│ 3. Guests     │ Guest registry, search by name/phone/email/ID, full    │
│               │ stay history modal, and contact validation.            │
├───────────────┼────────────────────────────────────────────────────────┤
│ 4. Bookings   │ Interval-based availability checking, auto night &     │
│               │ amount calculations, discounts, and cancellation flow. │
├───────────────┼────────────────────────────────────────────────────────┤
│ 5. Front Desk │ Dedicated reception desk for express check-in and      │
│               │ checkout with incidental expense calculation.          │
├───────────────┼────────────────────────────────────────────────────────┤
│ 6. Billing    │ Cash, Card, and UPI payment simulation, invoice        │
│               │ numbering, outstanding balance, and WPF folio print.   │
├───────────────┼────────────────────────────────────────────────────────┤
│ 7. Reports    │ Filter by date ranges, room tier distribution chart,   │
│               │ financial aggregates, and export to CSV.               │
├───────────────┼────────────────────────────────────────────────────────┤
│ 8. Settings   │ Persistent SQLite path, backup creation (*.db),        │
│               │ property metadata, and demo dataset restore.           │
└───────────────┴────────────────────────────────────────────────────────┘
```

---

## 3. Technology Stack & Prerequisites

- **Language:** C# 12
- **Desktop Framework:** Windows Presentation Foundation (WPF)
- **Target Framework:** `.NET 8.0-windows` (LTS)
- **Data Access:** ADO.NET with `Microsoft.Data.Sqlite.Core` (v8.0.8) and `SQLitePCLRaw.bundle_e_sqlite3` (v2.1.6)
- **Database Engine:** SQLite (Local persistent single-file database)
- **Development Environment:** Visual Studio 2022 / VS Code with C# Dev Kit / .NET CLI

---

## 4. Project Folder Structure

```
HotelEase/
├── Commands/
│   ├── RelayCommand.cs                    # Reusable ICommand implementation
│   └── AsyncRelayCommand.cs               # Asynchronous ICommand with task execution
├── Converters/
│   ├── StatusToBrushConverter.cs          # Converts status strings to badge background/foreground brushes
│   ├── CurrencyConverter.cs               # Formats numbers to currency strings ($)
│   ├── DateConverter.cs                   # Formats DateTime objects for presentation
│   └── BooleanToVisibilityConverter.cs    # Converts booleans/nulls to WPF Visibility states
├── Data/
│   ├── DatabaseHelper.cs                  # Connection factory and SQLite foreign key enforcement
│   └── DatabaseInitializer.cs            # Table schema creation and realistic demo dataset seeding
├── Models/
│   ├── Room.cs                            # Room inventory entity
│   ├── Guest.cs                           # Guest contact and identification entity
│   ├── Reservation.cs                     # Stay reservation entity with financial calculations
│   ├── Payment.cs                         # Transaction payment entity
│   └── DashboardMetrics.cs                # Aggregated operational and occupancy metrics
├── Services/
│   ├── IRoomService.cs & RoomService.cs   # Room CRUD, safe deletion checks, status recalculation
│   ├── IGuestService.cs & GuestService.cs # Guest registry, contact validation, stay history
│   ├── IReservationService.cs & ...       # Overlap verification, booking lifecycle, check-in/out
│   ├── IPaymentService.cs & ...           # Payment recording, invoice numbering, balance tracking
│   ├── IReportService.cs & ...            # Business metrics, revenue grouping, CSV export
│   └── PrintService.cs                    # Native WPF PrintDialog visual printing service
├── ViewModels/
│   ├── ViewModelBase.cs                   # INotifyPropertyChanged base class
│   ├── MainViewModel.cs                   # Navigation coordinator, live clock, top header
│   ├── DashboardViewModel.cs              # Dashboard summary cards & quick actions
│   ├── RoomsViewModel.cs                  # Room inventory search, filter, and management
│   ├── GuestsViewModel.cs                 # Guest registry and search
│   ├── ReservationsViewModel.cs           # Booking management, status filters, cancellations
│   ├── CheckInCheckOutViewModel.cs        # Front-desk check-in/out tabs and incidental settlement
│   ├── PaymentsViewModel.cs               # Billing transactions and folio view
│   ├── ReportsViewModel.cs                # Date range filters, distribution charts, and CSV export
│   └── SettingsViewModel.cs               # Database location, backup, and settings
├── Views/
│   ├── DashboardView.xaml & .cs
│   ├── RoomsView.xaml & .cs
│   ├── GuestsView.xaml & .cs
│   ├── ReservationsView.xaml & .cs
│   ├── CheckInCheckOutView.xaml & .cs
│   ├── PaymentsView.xaml & .cs
│   ├── ReportsView.xaml & .cs
│   ├── SettingsView.xaml & .cs
│   └── Dialogs/
│       ├── RoomDialog.xaml & .cs          # Room addition and editing modal
│       ├── GuestDialog.xaml & .cs         # Guest registration and editing modal
│       ├── ReservationDialog.xaml & .cs   # Booking wizard with live rate calculations
│       ├── PaymentDialog.xaml & .cs       # Payment entry modal
│       ├── InvoicePrintDialog.xaml & .cs  # Printable guest folio dialog
│       └── GuestHistoryDialog.xaml & .cs  # Guest booking history viewer
├── Resources/
│   ├── Colors.xaml                        # Navy blue, white, and teal color palette
│   ├── Icons.xaml                         # Vector StreamGeometry icons
│   └── Styles.xaml                        # Buttons, TextBoxes, DataGrid, and cards
├── App.xaml & App.xaml.cs                 # Resource merging and global exception handling
├── MainWindow.xaml & MainWindow.xaml.cs   # Main window shell and dialog wiring
└── README.md                              # College documentation and viva guide
```

---

## 5. Database Design & Schema

### 5.1 Storage Location
The SQLite database file is stored in a writable application directory:
- **Path:** `%LOCALAPPDATA%\HotelEase\hoteln.db`
- **Full Path Example:** `C:\Users\<Username>\AppData\Local\HotelEase\hoteln.db`

### 5.2 Backup and Recovery
- To backup: Open **Settings & Database** within HotelEase and click **Backup Database (*.db)** to export a copy.
- Alternatively, copy `hoteln.db` from `%LOCALAPPDATA%\HotelEase\` while the application is closed.

### 5.3 Schema Specification
```sql
PRAGMA foreign_keys = ON;

CREATE TABLE Rooms (
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

CREATE TABLE Guests (
    GuestId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Email TEXT NOT NULL,
    Address TEXT,
    IdReference TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE Reservations (
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

CREATE TABLE Payments (
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
```

---

## 6. How to Build and Run

### 6.1 Building via Command Line (.NET CLI)
Navigate to the project root directory:
```bash
cd C:\Users\harsh\.gemini\antigravity\scratch\HotelEase
```

Build the application:
```bash
dotnet build --no-restore
```

Run the application:
```bash
dotnet run --no-build
```

### 6.2 Opening in Visual Studio 2022
1. Launch Visual Studio 2022.
2. Select **Open a project or solution**.
3. Select `HotelEase.csproj`.
4. Press `F5` to build and run with the debugger.

### 6.3 Opening in Visual Studio Code
1. Open the folder `HotelEase` in VS Code.
2. Ensure the **C# Dev Kit** extension is installed.
3. Open the Terminal (`Ctrl + ` `) and run `dotnet run`.

---

## 7. WPF Syllabus Concepts Mapping

| Syllabus Concept | Implementation in HotelEase | Relevant Files |
| :--- | :--- | :--- |
| **XAML Architecture** | Clear separation between visual design and business logic | `MainWindow.xaml`, `Views/*.xaml` |
| **Layout Panels** | `Grid`, `StackPanel`, `DockPanel`, `Border`, `ScrollViewer` | All View files |
| **ResourceDictionaries** | Merged dictionaries for cohesive theming | `Colors.xaml`, `Icons.xaml`, `Styles.xaml` |
| **Styles & Triggers** | Hover animations, focused border highlights, custom buttons | `Styles.xaml` |
| **MVVM Pattern** | `ViewModelBase` with `INotifyPropertyChanged` and command binding | `ViewModels/*.cs` |
| **Commanding (ICommand)** | `RelayCommand` and `AsyncRelayCommand` | `Commands/*.cs` |
| **Data Binding** | Two-way and one-way bindings, collection binding | `Views/*.xaml` |
| **ObservableCollection<T>** | Live collection notifications to update DataGrids automatically | `ViewModels/*.cs` |
| **WPF DataGrid** | Custom cell templates, badges, auto-generated columns, formatting | All View files |
| **Value Converters** | Status to brush, currency, date, and visibility conversions | `Converters/*.cs` |
| **Vector Graphics** | StreamGeometry paths for crisp resolution-independent icons | `Resources/Icons.xaml` |
| **WPF Printing** | Native Windows `PrintDialog` rendering of visual invoice elements | `PrintService.cs`, `InvoicePrintDialog.xaml` |
| **Data Integrity** | SQLite transactions, parameterized queries, and safe deletions | `Data/*.cs`, `Services/*.cs` |

---

## 8. College Viva Voce Q&A Reference

### Q1: What design pattern is used in this project and why?
**Answer:** The **MVVM (Model-View-ViewModel)** design pattern. It separates the User Interface (XAML Views) from business logic and database access (ViewModels and Services). This allows:
1. Independent UI styling without breaking business logic.
2. Direct unit and integration testing without launching the UI.
3. Clean data binding through `INotifyPropertyChanged` and `ICommand`.

### Q2: How does the reservation overlap checking work?
**Answer:** Hotel stays operate on the half-open interval `[CheckInDate, CheckOutDate)`. Two stays `[A, B)` and `[C, D)` overlap if and only if `A < D AND C < B`.
In HotelEase, the query checks:
```sql
SELECT COUNT(*) FROM Reservations
WHERE RoomId = @rid
  AND BookingStatus IN ('Confirmed', 'Checked In')
  AND @newCheckIn < CheckOutDate
  AND @newCheckOut > CheckInDate;
```
If a guest checks out on October 28th, another guest can check in on October 28th without collision.

### Q3: Why are parameterized queries used instead of string concatenation?
**Answer:** Parameterized queries (`cmd.Parameters.AddWithValue(...)`) separate executable SQL commands from user input values. This:
1. Prevents SQL Injection attacks.
2. Automatically handles escaping of special characters (e.g., quotes in names).
3. Ensures correct type mapping for dates, integers, and decimals.

### Q4: How is safe deletion handled for rooms and guests?
**Answer:** To preserve historical audit logs and accounting integrity, rooms and guests with existing reservation records cannot be deleted. The system validates foreign key dependencies before deletion and prompts the staff to mark the room as "Maintenance" or deactivate it instead.

### Q5: How is printing implemented in WPF?
**Answer:** Using `System.Windows.Controls.PrintDialog`. The `PrintService.PrintVisual(Visual visual, string title)` method captures the formatted WPF `Border` containing the guest invoice layout and directly spools it to any Windows-configured printer or the built-in Microsoft Print to PDF driver.
