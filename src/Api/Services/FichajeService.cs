using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class FichajeService
{
    private readonly AppDbContext _db;

    public FichajeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TimeEntryDto>> GetByInstructorAsync(int instructorId, DateTime from, DateTime to)
    {
        return await _db.TimeEntries
            .Include(t => t.Instructor)
            .Where(t => t.InstructorId == instructorId && t.Date >= from.Date && t.Date <= to.Date)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.StartHour)
            .Select(t => new TimeEntryDto(
                t.Id,
                t.InstructorId,
                t.Instructor != null ? t.Instructor.Username : "",
                t.Date,
                t.StartHour,
                t.EndHour,
                t.Description,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<(TimeEntryDto? Entry, string? Error)> CreateAsync(int instructorId, CreateTimeEntryRequest request)
    {
        if (request.StartHour >= request.EndHour)
            return (null, "La hora de inicio debe ser menor a la hora de fin");

        // Check for overlapping entries
        var hasOverlap = await _db.TimeEntries
            .AnyAsync(t => t.InstructorId == instructorId
                && t.Date.Date == request.Date.Date
                && t.StartHour < request.EndHour
                && t.EndHour > request.StartHour);

        if (hasOverlap)
            return (null, "Ya existe un fichaje que se superpone con ese horario");

        var entry = new TimeEntry
        {
            InstructorId = instructorId,
            Date = request.Date.Date,
            StartHour = request.StartHour,
            EndHour = request.EndHour,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        var instructor = await _db.Users.FindAsync(instructorId);

        return (new TimeEntryDto(
            entry.Id,
            entry.InstructorId,
            instructor?.Username ?? "",
            entry.Date,
            entry.StartHour,
            entry.EndHour,
            entry.Description,
            entry.CreatedAt,
            entry.UpdatedAt
        ), null);
    }

    public async Task<(TimeEntryDto? Entry, string? Error)> UpdateAsync(int id, int instructorId, UpdateTimeEntryRequest request)
    {
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry is null)
            return (null, "Fichaje no encontrado");

        if (entry.InstructorId != instructorId)
            return (null, "No tenes permiso para editar este fichaje");

        if (request.StartHour >= request.EndHour)
            return (null, "La hora de inicio debe ser menor a la hora de fin");

        // Check for overlapping entries (excluding current)
        var hasOverlap = await _db.TimeEntries
            .AnyAsync(t => t.InstructorId == instructorId
                && t.Id != id
                && t.Date.Date == entry.Date.Date
                && t.StartHour < request.EndHour
                && t.EndHour > request.StartHour);

        if (hasOverlap)
            return (null, "Ya existe un fichaje que se superpone con ese horario");

        entry.StartHour = request.StartHour;
        entry.EndHour = request.EndHour;
        entry.Description = request.Description;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var instructor = await _db.Users.FindAsync(instructorId);

        return (new TimeEntryDto(
            entry.Id,
            entry.InstructorId,
            instructor?.Username ?? "",
            entry.Date,
            entry.StartHour,
            entry.EndHour,
            entry.Description,
            entry.CreatedAt,
            entry.UpdatedAt
        ), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int instructorId)
    {
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry is null)
            return (false, "Fichaje no encontrado");

        if (entry.InstructorId != instructorId)
            return (false, "No tenes permiso para eliminar este fichaje");

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
