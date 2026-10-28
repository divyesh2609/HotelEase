using System.Collections.Generic;
using System.Threading.Tasks;
using HotelEase.Models;

namespace HotelEase.Services
{
    public interface IRoomService
    {
        Task<List<Room>> GetAllRoomsAsync(bool includeInactive = false);
        Task<Room?> GetRoomByIdAsync(int roomId);
        Task<bool> AddRoomAsync(Room room);
        Task<bool> UpdateRoomAsync(Room room);
        Task<bool> CanDeleteRoomAsync(int roomId);
        Task<bool> DeleteRoomAsync(int roomId);
        Task<bool> SetRoomStatusAsync(int roomId, string status);
        Task RecalculateRoomStatusesAsync();
    }
}
