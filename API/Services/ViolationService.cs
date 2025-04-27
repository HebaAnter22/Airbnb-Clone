using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ViolationService : IViolationService
    {
        private readonly AppDbContext _context;

        public ViolationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ViolationResponseDto> ReportViolation(int userId, CreateViolationDto dto)
        {
            var violation = new Violation
            {
                ReportedById = userId,
                ReportedPropertyId = dto.ReportedPropertyId,
                ReportedHostId = dto.ReportedHostId,
                ViolationType = dto.ViolationType,
                Description = dto.Description,
                Status = ViolationStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Violations.Add(violation);
            await _context.SaveChangesAsync();

            // Reload the violation with all the navigation properties
            violation = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .FirstOrDefaultAsync(v => v.Id == violation.Id);

            return MapToDto(violation);
        }

        public async Task<List<ViolationResponseDto>> GetAllViolations()
        {
            var violations = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return violations.Select(MapToDto).ToList();
        }

        public async Task<List<ViolationResponseDto>> GetViolationsByStatus(string status)
        {
            var violations = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .Where(v => v.Status == status)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return violations.Select(MapToDto).ToList();
        }

        public async Task<List<ViolationResponseDto>> GetViolationsByUser(int userId)
        {
            var violations = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .Where(v => v.ReportedById == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return violations.Select(MapToDto).ToList();
        }

        public async Task<ViolationResponseDto> GetViolationById(int id)
        {
            var violation = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (violation == null)
                return null;

            return MapToDto(violation);
        }

        public async Task<ViolationResponseDto> UpdateViolationStatus(int id, UpdateViolationStatusDto dto)
        {
            var violation = await _context.Violations.FindAsync(id);
            if (violation == null)
                return null;

            violation.Status = dto.Status;
            violation.AdminNotes = dto.AdminNotes;
            violation.UpdatedAt = DateTime.UtcNow;
            
            if (dto.Status == ViolationStatus.Resolved.ToString())
            {
                violation.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Reload the violation with all the navigation properties
            violation = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .FirstOrDefaultAsync(v => v.Id == violation.Id);

            return MapToDto(violation);
        }

        public async Task<bool> BlockHost(int hostId)
        {
            var host = await _context.HostProfules.FindAsync(hostId);
            if (host == null)
                return false;

            var user = await _context.Users.FindAsync(host.HostId);
            if (user == null)
                return false;

            user.AccountStatus = Account_Status.Blocked.ToString();
            user.UpdatedAt = DateTime.UtcNow;

            // Mark all the host's properties as suspended
            var hostProperties = await _context.Properties.Where(p => p.HostId == hostId).ToListAsync();
            foreach (var property in hostProperties)
            {
                property.Status = PropertyStatus.Suspended.ToString();
                property.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ViolationResponseDto>> GetViolationsByHostId(int hostId)
        {
            var violations = await _context.Violations
                .Include(v => v.ReportedBy)
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .Where(v => v.ReportedHostId == hostId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return violations.Select(MapToDto).ToList();
        }

        private ViolationResponseDto MapToDto(Violation violation)
        {
            return new ViolationResponseDto
            {
                Id = violation.Id,
                ReportedById = violation.ReportedById,
                ReporterName = violation.ReportedBy != null 
                    ? $"{violation.ReportedBy.FirstName} {violation.ReportedBy.LastName}" 
                    : "Unknown",
                ReportedPropertyId = violation.ReportedPropertyId,
                ReportedPropertyTitle = violation.ReportedProperty?.Title,
                ReportedHostId = violation.ReportedHostId,
                ReportedHostName = violation.ReportedHost?.User != null 
                    ? $"{violation.ReportedHost.User.FirstName} {violation.ReportedHost.User.LastName}" 
                    : "Unknown",
                ViolationType = violation.ViolationType,
                Description = violation.Description,
                Status = violation.Status,
                CreatedAt = violation.CreatedAt,
                ResolvedAt = violation.ResolvedAt
            };
        }
    }
} 