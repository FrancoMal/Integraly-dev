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

        // Build a set of (DayOfWeek, StartHour) from the incoming active slots
        var activeKeys = new HashSet<string>(
            slots.Where(s => s.IsActive).Select(s => $"{s.DayOfWeek}-{s.StartHour}")
        );

        // Get all existing availabilities for this instructor
        var existing = await _db.Availabilities
            .Where(a => a.InstructorId == instructorId)
            .ToListAsync();

        // Deactivate slots that are NOT in the new list
        foreach (var ex in existing)
        {
            var key = $"{ex.DayOfWeek}-{ex.StartHour}";
            if (!activeKeys.Contains(key) && ex.IsActive)
            {
                ex.IsActive = false;
            }
        }

        await _db.SaveChangesAsync();

        // Upsert the sent slots
        foreach (var slot in slots)
        {
            var result = await SetAvailabilityAsync(instructorId, slot.DayOfWeek, slot.StartHour, slot.IsActive);
            results.Add(result);
        }

        return results;
    }

    // Get week-specific overrides for an instructor for a date range
    public async Task<List<WeekAvailabilityDto>> GetWeekAvailabilityAsync(int instructorId, DateTime from, DateTime to)
    {
        return await _db.WeekAvailabilities
            .Where(w => w.InstructorId == instructorId && w.Date >= from.Date && w.Date <= to.Date)
            .OrderBy(w => w.Date).ThenBy(w => w.StartHour)
            .Select(w => new WeekAvailabilityDto(w.Id, w.InstructorId, w.Date, w.StartHour, w.IsActive))
            .ToListAsync();
    }

    // Set week-specific overrides - receives the full list of override slots for a week
    public async Task<List<WeekAvailabilityDto>> SetWeekAvailabilityAsync(int instructorId, DateTime weekStart, List<WeekSlotRequest> slots)
    {
        var weekEnd = weekStart.AddDays(6);

        // Get existing overrides for this week
        var existing = await _db.WeekAvailabilities
            .Where(w => w.InstructorId == instructorId && w.Date >= weekStart.Date && w.Date <= weekEnd.Date)
            .ToListAsync();

        // Remove all existing overrides for this week
        _db.WeekAvailabilities.RemoveRange(existing);

        // Add new overrides
        foreach (var slot in slots)
        {
            _db.WeekAvailabilities.Add(new WeekAvailability
            {
                InstructorId = instructorId,
                Date = slot.Date.Date,
                StartHour = slot.StartHour,
                IsActive = slot.IsActive,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return await GetWeekAvailabilityAsync(instructorId, weekStart, weekEnd);
    }

    // Check if instructor is available at a specific date+hour (considering overrides)
    public async Task<bool> IsAvailableAtAsync(int instructorId, DateTime date, int startHour)
    {
        // First check week-specific override
        var weekOverride = await _db.WeekAvailabilities
            .FirstOrDefaultAsync(w => w.InstructorId == instructorId && w.Date == date.Date && w.StartHour == startHour);

        if (weekOverride is not null)
            return weekOverride.IsActive;

        // Fall back to general availability
        var dayOfWeek = (int)date.DayOfWeek;
        var general = await _db.Availabilities
            .FirstOrDefaultAsync(a => a.InstructorId == instructorId && a.DayOfWeek == dayOfWeek && a.StartHour == startHour);

        return general?.IsActive ?? false;
    }

    // Toggle a single week availability slot for admin
    public async Task<WeekAvailabilityDto> ToggleSingleSlotAsync(int instructorId, DateTime date, int startHour, bool isActive)
    {
        var existing = await _db.WeekAvailabilities
            .FirstOrDefaultAsync(w => w.InstructorId == instructorId && w.Date == date.Date && w.StartHour == startHour);

        if (existing is not null)
        {
            existing.IsActive = isActive;
            await _db.SaveChangesAsync();
            return new WeekAvailabilityDto(existing.Id, existing.InstructorId, existing.Date, existing.StartHour, existing.IsActive);
        }

        var slot = new WeekAvailability
        {
            InstructorId = instructorId,
            Date = date.Date,
            StartHour = startHour,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.WeekAvailabilities.Add(slot);
        await _db.SaveChangesAsync();

        return new WeekAvailabilityDto(slot.Id, slot.InstructorId, slot.Date, slot.StartHour, slot.IsActive);
    }
}
