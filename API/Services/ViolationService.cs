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

        public async Task<List<BookingDto>> GetBookingsRelatedToViolation(int violationId)
        {
            var violation = await _context.Violations
                .Include(v => v.ReportedProperty)
                .Include(v => v.ReportedHost)
                .FirstOrDefaultAsync(v => v.Id == violationId);

            if (violation == null)
                return new List<BookingDto>();

            var query = _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Include(b => b.Payments)
                .AsQueryable();

            // Filter based on property or host
            if (violation.ReportedPropertyId.HasValue)
            {
                query = query.Where(b => b.PropertyId == violation.ReportedPropertyId.Value);
            }
            else if (violation.ReportedHostId.HasValue)
            {
                query = query.Where(b => b.Property.HostId == violation.ReportedHostId.Value);
            }
            else
            {
                // No property or host reported, so no related bookings
                return new List<BookingDto>();
            }

            // Only include confirmed or completed bookings
            query = query.Where(b => b.Status == "Confirmed" || b.Status == "Completed");

            // Add a time filter - only include bookings from the last 90 days
            var cutOffDate = DateTime.UtcNow.AddDays(-90);
            query = query.Where(b => b.StartDate >= cutOffDate);

            var bookings = await query.ToListAsync();

            return bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                PropertyId = b.PropertyId,
                PropertyTitle = b.Property?.Title ?? "Unknown Property",
                GuestId = b.GuestId,
                GuestName = $"{b.Guest?.FirstName} {b.Guest?.LastName}".Trim(),
                Status = b.Status,
                TotalPrice = b.TotalAmount,
                PaymentId = b.Payments?.FirstOrDefault()?.Id,
                PaymentAmount = b.Payments?.FirstOrDefault()?.Amount,
                // A booking can be refunded if it has a successful payment that hasn't been fully refunded
                CanBeRefunded = b.Payments != null && 
                               b.Payments.FirstOrDefault()?.Status == "succeeded" &&
                               (b.Payments?.FirstOrDefault() ?.RefundedAmount < b.Payments?.FirstOrDefault()?.Amount)
            }).ToList();
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