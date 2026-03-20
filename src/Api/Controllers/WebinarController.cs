using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebinarController : ControllerBase
{
    private readonly AppDbContext _db;

    public WebinarController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/webinar/dates
    [HttpGet("dates")]
    public async Task<IActionResult> GetDates()
    {
        var dates = await _db.WebinarDates
            .OrderBy(d => d.Date)
            .ToListAsync();

        var result = new List<WebinarDateDto>();
        foreach (var d in dates)
        {
            var count = await _db.WebinarRegistrations.CountAsync(r => r.WebinarDateId == d.Id);
            result.Add(new WebinarDateDto { Id = d.Id, Date = d.Date, MeetingLink = d.MeetingLink, RegistrationCount = count, CreatedAt = d.CreatedAt });
        }

        return Ok(result);
    }

    // POST /api/webinar/dates
    [HttpPost("dates")]
    public async Task<IActionResult> CreateDate([FromBody] CreateWebinarDateRequest request)
    {
        var date = new WebinarDate
        {
            Date = request.Date,
            MeetingLink = request.MeetingLink
        };

        _db.WebinarDates.Add(date);
        await _db.SaveChangesAsync();

        return Created($"/api/webinar/dates/{date.Id}",
            new WebinarDateDto { Id = date.Id, Date = date.Date, MeetingLink = date.MeetingLink, RegistrationCount = 0, CreatedAt = date.CreatedAt });
    }

    // DELETE /api/webinar/dates/{id}
    [HttpDelete("dates/{id}")]
    public async Task<IActionResult> DeleteDate(int id)
    {
        var date = await _db.WebinarDates.FindAsync(id);
        if (date is null) return NotFound();

        var hasRegistrations = await _db.WebinarRegistrations.AnyAsync(r => r.WebinarDateId == id);
        if (hasRegistrations)
            return BadRequest(new { message = "No se puede eliminar una fecha que tiene inscripciones" });

        _db.WebinarDates.Remove(date);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/webinar/contacts
    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        var contacts = await _db.WebinarContacts
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<WebinarContactDto>();
        foreach (var c in contacts)
        {
            var hasReg = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == c.Id);
            string? dateDisplay = null;
            if (c.WebinarDateId != null)
            {
                var wd = await _db.WebinarDates.FindAsync(c.WebinarDateId);
                if (wd != null) dateDisplay = wd.Date.ToString("dd/MM/yyyy HH:mm");
            }
            result.Add(new WebinarContactDto
            {
                Id = c.Id, Email = c.Email, FirstName = c.FirstName, LastName = c.LastName,
                Phone = c.Phone, UUID = c.UUID, WebinarDateId = c.WebinarDateId,
                WebinarDateDisplay = dateDisplay, HasRegistration = hasReg, CreatedAt = c.CreatedAt
            });
        }

        return Ok(result);
    }

    // POST /api/webinar/contacts
    [HttpPost("contacts")]
    public async Task<IActionResult> CreateContact([FromBody] CreateWebinarContactRequest request)
    {
        var contact = new WebinarContact
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            UUID = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _db.WebinarContacts.Add(contact);
        await _db.SaveChangesAsync();

        return Created($"/api/webinar/contacts/{contact.Id}",
            new WebinarContactDto
            {
                Id = contact.Id, Email = contact.Email, FirstName = contact.FirstName,
                LastName = contact.LastName, Phone = contact.Phone, UUID = contact.UUID,
                HasRegistration = false, CreatedAt = contact.CreatedAt
            });
    }

    // DELETE /api/webinar/contacts/{id}
    [HttpDelete("contacts/{id}")]
    public async Task<IActionResult> DeleteContact(int id)
    {
        var contact = await _db.WebinarContacts.FindAsync(id);
        if (contact is null) return NotFound();

        var registration = await _db.WebinarRegistrations.FirstOrDefaultAsync(r => r.ContactId == id);
        if (registration is not null)
            _db.WebinarRegistrations.Remove(registration);

        _db.WebinarContacts.Remove(contact);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/webinar/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalContacts = await _db.WebinarContacts.CountAsync();
        var totalRegistrations = await _db.WebinarRegistrations.CountAsync();

        var webinarDates = await _db.WebinarDates.OrderBy(d => d.Date).ToListAsync();
        var perDate = new List<WebinarDateStatsDto>();

        foreach (var d in webinarDates)
        {
            var regs = await _db.WebinarRegistrations
                .Where(r => r.WebinarDateId == d.Id)
                .ToListAsync();

            var vibeCodingBreakdown = new Dictionary<string, int>
            {
                { "yes_use_it", regs.Count(r => r.VibeCodingKnowledge == "yes_use_it") },
                { "yes_not_tried", regs.Count(r => r.VibeCodingKnowledge == "yes_not_tried") },
                { "no_idea", regs.Count(r => r.VibeCodingKnowledge == "no_idea") }
            };

            perDate.Add(new WebinarDateStatsDto
            {
                DateId = d.Id,
                Date = d.Date,
                Registrations = regs.Count,
                KnowsChatGPT = regs.Count(r => r.KnowsChatGPT),
                KnowsClaude = regs.Count(r => r.KnowsClaude),
                KnowsGrok = regs.Count(r => r.KnowsGrok),
                KnowsGemini = regs.Count(r => r.KnowsGemini),
                KnowsCopilot = regs.Count(r => r.KnowsCopilot),
                KnowsPerplexity = regs.Count(r => r.KnowsPerplexity),
                KnowsDeepSeek = regs.Count(r => r.KnowsDeepSeek),
                VibeCodingBreakdown = vibeCodingBreakdown
            });
        }

        return Ok(new WebinarStatsDto
        {
            TotalContacts = totalContacts,
            TotalRegistrations = totalRegistrations,
            PerDate = perDate
        });
    }

    // GET /api/webinar/form/{uuid} - Public
    [AllowAnonymous]
    [HttpGet("form/{uuid}")]
    public async Task<IActionResult> GetForm(string uuid)
    {
        var contact = await _db.WebinarContacts.FirstOrDefaultAsync(c => c.UUID == uuid);
        if (contact is null)
            return NotFound(new { message = "Contacto no encontrado" });

        var hasRegistration = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == contact.Id);

        var availableDates = await _db.WebinarDates
            .Where(d => d.Date > DateTime.UtcNow)
            .OrderBy(d => d.Date)
            .Select(d => new WebinarDateOptionDto { Id = d.Id, Date = d.Date })
            .ToListAsync();

        return Ok(new WebinarFormDataDto
        {
            FirstName = contact.FirstName,
            AvailableDates = availableDates,
            AlreadyRegistered = hasRegistration
        });
    }

    // POST /api/webinar/form/{uuid} - Public
    [AllowAnonymous]
    [HttpPost("form/{uuid}")]
    public async Task<IActionResult> SubmitForm(string uuid, [FromBody] WebinarFormSubmitRequest request)
    {
        var contact = await _db.WebinarContacts.FirstOrDefaultAsync(c => c.UUID == uuid);
        if (contact is null)
            return NotFound(new { message = "Contacto no encontrado" });

        var hasRegistration = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == contact.Id);
        if (hasRegistration)
            return BadRequest(new { message = "Ya estas inscripto en un webinar" });

        var dateExists = await _db.WebinarDates.AnyAsync(d => d.Id == request.WebinarDateId);
        if (!dateExists)
            return BadRequest(new { message = "Fecha no valida" });

        var registration = new WebinarRegistration
        {
            ContactId = contact.Id,
            WebinarDateId = request.WebinarDateId,
            KnowsChatGPT = request.KnowsChatGPT,
            KnowsClaude = request.KnowsClaude,
            KnowsGrok = request.KnowsGrok,
            KnowsGemini = request.KnowsGemini,
            KnowsCopilot = request.KnowsCopilot,
            KnowsPerplexity = request.KnowsPerplexity,
            KnowsDeepSeek = request.KnowsDeepSeek,
            VibeCodingKnowledge = request.VibeCodingKnowledge,
            RegisteredAt = DateTime.UtcNow
        };

        _db.WebinarRegistrations.Add(registration);
        contact.WebinarDateId = request.WebinarDateId;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Inscripcion exitosa" });
    }
}
