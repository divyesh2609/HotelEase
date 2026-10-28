using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelEase.Services
{
    /// <summary>
    /// Service for printing WPF visual elements using PrintDialog.
    /// Demonstrates syllabus concepts of WPF Document and Visual printing.
    /// </summary>
    public static class PrintService
    {
        public static bool PrintVisual(Visual visual, string jobTitle)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(visual, jobTitle);
                return true;
            }
            return false;
        }
    }
}
