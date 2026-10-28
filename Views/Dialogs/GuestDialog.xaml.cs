using System;
using System.Text.RegularExpressions;
using System.Windows;
using HotelEase.Models;

namespace HotelEase.Views.Dialogs
{
    public partial class GuestDialog : Window
    {
        public Guest Guest { get; }
        private readonly bool _isEdit;

        public GuestDialog(Guest guest, bool isEdit = false)
        {
            InitializeComponent();
            Guest = guest;
            _isEdit = isEdit;

            TxtTitle.Text = _isEdit ? "Edit Guest Information" : "Register New Guest";

            TxtFullName.Text = guest.FullName;
            TxtPhone.Text = guest.Phone;
            TxtEmail.Text = guest.Email;
            TxtAddress.Text = guest.Address;
            TxtIdReference.Text = guest.IdReference;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var name = TxtFullName.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter the guest's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFullName.Focus();
                return;
            }

            var phone = TxtPhone.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Please enter a contact phone number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPhone.Focus();
                return;
            }

            var email = TxtEmail.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter an email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid email address format (e.g., guest@example.com).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            Guest.FullName = name;
            Guest.Phone = phone;
            Guest.Email = email;
            Guest.Address = TxtAddress.Text?.Trim() ?? string.Empty;
            Guest.IdReference = TxtIdReference.Text?.Trim() ?? string.Empty;

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
