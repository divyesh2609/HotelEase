using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Models;

namespace HotelEase.Services
{
    public interface IReservationService
    {
        Task<List<Reservation>> GetAllReservationsAsync(string? searchQuery = null, string? statusFilter = null);
        Task<Reservation?> GetReservationByIdAsync(int reservationId);
        Task<Reservation?> GetReservationByReferenceAsync(string reference);
        Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, string? roomType = null, int? capacity = null, int? excludeReservationId = null);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
        Task<string> CreateReservationAsync(Reservation reservation);
        Task<bool> UpdateReservationAsync(Reservation reservation);
        Task<bool> CancelReservationAsync(int reservationId, string? reason = null);
        Task<bool> CheckInReservationAsync(int reservationId);
        Task<bool> CheckOutReservationAsync(int reservationId, decimal additionalCharges = 0);
        Task<List<Reservation>> GetTodayArrivalsAsync();
        Task<List<Reservation>> GetTodayDeparturesAsync();
        Task<List<Reservation>> GetActiveStaysAsync();
    }
}
