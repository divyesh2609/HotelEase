using System;

namespace HotelEase.Models
{
    public class DashboardMetrics
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int ReservedRooms { get; set; }
        public int MaintenanceRooms { get; set; }

        public int TodayCheckIns { get; set; }
        public int TodayCheckOuts { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }

        private double _occupancyPercentage;
        public double OccupancyPercentage
        {
            get => TotalRooms > 0 
                ? Math.Round(((double)OccupiedRooms / TotalRooms) * 100.0, 1) 
                : _occupancyPercentage;
            set => _occupancyPercentage = value;
        }
    }

    public class RoomTypeDistribution
    {
        public string RoomType { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int OccupiedCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
