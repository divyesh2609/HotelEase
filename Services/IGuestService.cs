using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Models;

namespace HotelEase.Services
{
    public interface IGuestService
    {
        Task<List<Guest>> GetAllGuestsAsync(string? searchQuery = null);
        Task<Guest?> GetGuestByIdAsync(int guestId);
        Task<bool> AddGuestAsync(Guest guest);
        Task<bool> UpdateGuestAsync(Guest guest);
        Task<List<Reservation>> GetGuestHistoryAsync(int guestId);
        Task<bool> CanDeleteGuestAsync(int guestId);
        Task<bool> DeleteGuestAsync(int guestId);
    }
}
