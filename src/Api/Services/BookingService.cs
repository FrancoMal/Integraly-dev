using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BookingService
{
    private readonly AppDbContext _db;
    private readonly TokenPackService _tokenPackService;
    private readonly AvailabilityService _availabilityService;

    public BookingService(AppDbContext db, TokenPackService tokenPackService, AvailabilityService availabilityService)
    {
        _db = db;
        _tokenPackService = tokenPackService;
        _availabilityService = availabilityService;
    }

    public async Task<List<BookingDto>> GetByUserIdAsync(int userId)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Instructor)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.ScheduledDate)
            .ThenBy(b => b.StartHour)
            .Select(b => new BookingDto(
                b.Id,
                b.UserId,
                b.User != null ? b.User.Username : "",
                b.InstructorId,
                b.Instructor != null ? b.Instructor.Username : "",
                b.ScheduledDate,
                b.StartHour,
                b.Status,
                b.MeetLink,
                b.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<List<BookingDto>> GetByInstructorIdAsync(int instructorId)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Instructor)
            .Where(b => b.InstructorId == instructorId)
            .OrderByDescending(b => b.ScheduledDate)
            .ThenBy(b => b.StartHour)
            .Select(b => new BookingDto(
                b.Id,
                b.UserId,
                b.User != null ? b.User.Username : "",
                b.InstructorId,
                b.Instructor != null ? b.Instructor.Username : "",
                b.ScheduledDate,
                b.StartHour,
                b.Status,
                b.MeetLink,
                b.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<List<BookingDto>> GetAllAsync()
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Instructor)
            .OrderByDescending(b => b.ScheduledDate)
            .ThenBy(b => b.StartHour)
            .Select(b => new BookingDto(
                b.Id,
                b.UserId,
                b.User != null ? b.User.Username : "",
                b.InstructorId,
                b.Instructor != null ? b.Instructor.Username : "",
                b.ScheduledDate,
                b.StartHour,
                b.Status,
                b.MeetLink,
                b.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<(BookingDto? Booking, string? Error)> CreateAsync(int userId, int instructorId, DateTime scheduledDate, int startHour)
    {
        // Validate instructor has availability for that day/hour (considering week overrides)
        var isAvailable = await _availabilityService.IsAvailableAtAsync(instructorId, scheduledDate, startHour);
        if (!isAvailable)
            return (null, "El instructor no tiene disponibilidad en ese horario");

        // Validate slot not already taken
        var existingBooking = await _db.Bookings
            .AnyAsync(b => b.InstructorId == instructorId
                && b.ScheduledDate.Date == scheduledDate.Date
                && b.StartHour == startHour
                && b.Status == "confirmed");

        if (existingBooking)
            return (null, "Ese horario ya esta reservado");

        // Consume token
        var tokenPackId = await _tokenPackService.ConsumeTokenAsync(userId);
        if (tokenPackId is null)
            return (null, "No tenes tokens disponibles");

        // Generate Google Meet link
        var meetCode = Guid.NewGuid().ToString("N")[..12];
        var meetLink = $"https://meet.google.com/{meetCode[..3]}-{meetCode[3..7]}-{meetCode[7..]}";

        var booking = new Booking
        {
            UserId = userId,
            InstructorId = instructorId,
            TokenPackId = tokenPackId.Value,
            ScheduledDate = scheduledDate.Date,
            StartHour = startHour,
            Status = "confirmed",
            MeetLink = meetLink,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        var instructor = await _db.Users.FindAsync(instructorId);

        var dto = new BookingDto(
            booking.Id,
            booking.UserId,
            user?.Username ?? "",
            booking.InstructorId,
            instructor?.Username ?? "",
            booking.ScheduledDate,
            booking.StartHour,
            booking.Status,
            booking.MeetLink,
            booking.CreatedAt
        );

        return (dto, null);
    }

    public async Task<(bool Success, string? Error)> CancelAsync(int bookingId, int userId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null)
            return (false, "Reserva no encontrada");

        if (booking.UserId != userId)
            return (false, "No tenes permiso para cancelar esta reserva");

        if (booking.Status != "confirmed")
            return (false, "La reserva ya fue cancelada");

        // Check cancellation window (from DB settings)
        var cancellationHoursSetting = await _db.AppSettings.FindAsync("CancellationHours");
        var cancellationHours = int.TryParse(cancellationHoursSetting?.Value, out var ch) ? ch : 24;
        var bookingDateTime = booking.ScheduledDate.Date.AddHours(booking.StartHour);
        var hoursUntilBooking = (bookingDateTime - DateTime.UtcNow).TotalHours;

        if (hoursUntilBooking < cancellationHours)
            return (false, $"No se puede cancelar con menos de {cancellationHours} horas de anticipacion");

        // Refund token
        await _tokenPackService.RefundTokenAsync(booking.TokenPackId);

        booking.Status = "cancelled";
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<BookingDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Instructor)
            .Where(b => b.ScheduledDate >= from.Date && b.ScheduledDate <= to.Date && b.Status == "confirmed")
            .OrderBy(b => b.ScheduledDate)
            .ThenBy(b => b.StartHour)
            .Select(b => new BookingDto(
                b.Id, b.UserId, b.User != null ? b.User.Username : "",
                b.InstructorId, b.Instructor != null ? b.Instructor.Username : "",
                b.ScheduledDate, b.StartHour, b.Status, b.MeetLink, b.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int instructorId, DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        // Get general availability for that day
        var generalSlots = await _db.Availabilities
            .Where(a => a.InstructorId == instructorId && a.DayOfWeek == dayOfWeek && a.IsActive)
            .Select(a => a.StartHour)
            .ToListAsync();

        // Get week overrides for that date
        var weekOverrides = await _db.WeekAvailabilities
            .Where(w => w.InstructorId == instructorId && w.Date == date.Date)
            .ToListAsync();

        // Build effective availability: start with general, apply overrides
        var effectiveHours = new HashSet<int>(generalSlots);

        foreach (var o in weekOverrides)
        {
            if (o.IsActive)
                effectiveHours.Add(o.StartHour);
            else
                effectiveHours.Remove(o.StartHour);
        }

        // Get confirmed bookings for that date
        var bookedHours = await _db.Bookings
            .Where(b => b.InstructorId == instructorId
                && b.ScheduledDate.Date == date.Date
                && b.Status == "confirmed")
            .Select(b => b.StartHour)
            .ToListAsync();

        return effectiveHours.OrderBy(h => h)
            .Select(h => new AvailableSlotDto(h, !bookedHours.Contains(h)))
            .ToList();
    }

    public async Task<(bool Success, string? Error)> AdminCancelAsync(int bookingId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null)
            return (false, "Reserva no encontrada");

        if (booking.Status != "confirmed")
            return (false, "La reserva ya fue cancelada o completada");

        // Refund token
        await _tokenPackService.RefundTokenAsync(booking.TokenPackId);

        booking.Status = "cancelled";
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
