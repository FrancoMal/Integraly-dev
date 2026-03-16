using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class AvailabilityService
{
    private readonly AppDbContext _db;

    public AvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AvailabilityDto>> GetByInstructorIdAsync(int instructorId)
    {
        return await _db.Availabilities
            .Where(a => a.InstructorId == instructorId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartHour)
            .Select(a => new AvailabilityDto(
                a.Id,
                a.InstructorId,
                a.DayOfWeek,
                a.StartHour,
                a.IsActive
            ))
            .ToListAsync();
    }

    public async Task<AvailabilityDto> SetAvailabilityAsync(int instructorId, int dayOfWeek, int startHour, bool isActive)
    {
        var existing = await _db.Availabilities
            .FirstOrDefaultAsync(a => a.InstructorId == instructorId
                && a.DayOfWeek == dayOfWeek
                && a.StartHour == startHour);

        if (existing is not null)
        {
            existing.IsActive = isActive;
            await _db.SaveChangesAsync();
            return new AvailabilityDto(existing.Id, existing.InstructorId, existing.DayOfWeek, existing.StartHour, existing.IsActive);
        }

        var availability = new Availability
        {
            InstructorId = instructorId,
            DayOfWeek = dayOfWeek,
            StartHour = startHour,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Availabilities.Add(availability);
        await _db.SaveChangesAsync();

        return new AvailabilityDto(availability.Id, availability.InstructorId, availability.DayOfWeek, availability.StartHour, availability.IsActive);
    }

    public async Task<List<AvailabilityDto>> BulkSetAsync(int instructorId, List<SetAvailabilityRequest> slots)
    {
        var results = new List<AvailabilityDto>();

        foreach (var slot in slots)
        {
            var result = await SetAvailabilityAsync(instructorId, slot.DayOfWeek, slot.StartHour, slot.IsActive);
            results.Add(result);
        }

        return results;
    }
}
