using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HotelEase.Converters
{
    /// <summary>
    /// Converts room/booking/payment statuses to corresponding styled colors for badges.
    /// Demonstrates IValueConverter from the WPF syllabus.
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public bool ReturnForeground { get; set; } = false;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.Trim() ?? string.Empty;

            if (ReturnForeground)
            {
                return status switch
                {
                    "Available" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),   // Emerald green
                    "Occupied" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),    // Blue
                    "Reserved" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),    // Amber
                    "Maintenance" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")), // Red
                    "Confirmed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7")),   // Sky blue
                    "Checked In" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),  // Emerald
                    "Checked Out" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), // Slate
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),   // Rose
                    "Paid" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),        // Green
                    "Partially Paid" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),// Amber
                    "Pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),     // Red
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
                };
            }
            else
            {
                // Background badge tint
                return status switch
                {
                    "Available" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5")),
                    "Occupied" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                    "Reserved" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")),
                    "Maintenance" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2")),
                    "Confirmed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F9FF")),
                    "Checked In" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5")),
                    "Checked Out" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1F2")),
                    "Paid" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5")),
                    "Partially Paid" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")),
                    "Pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1F2")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                };
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
