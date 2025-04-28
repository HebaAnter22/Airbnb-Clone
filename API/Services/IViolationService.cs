using API.DTOs;
using API.Models;

namespace API.Services
{
    public interface IViolationService
    {
        Task<ViolationResponseDto> ReportViolation(int userId, CreateViolationDto dto);
        Task<List<ViolationResponseDto>> GetAllViolations();
        Task<List<ViolationResponseDto>> GetViolationsByStatus(string status);
        Task<List<ViolationResponseDto>> GetViolationsByUser(int userId);
        Task<ViolationResponseDto> GetViolationById(int id);
        Task<ViolationResponseDto> UpdateViolationStatus(int id, UpdateViolationStatusDto dto);
        Task<bool> BlockHost(int hostId);
        Task<List<ViolationResponseDto>> GetViolationsByHostId(int hostId);
        Task<List<BookingDto>> GetBookingsRelatedToViolation(int violationId);
    }
} 