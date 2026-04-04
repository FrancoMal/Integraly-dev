using Api.Data;
using Api.DTOs;
using Api.Models;
using ClosedXML.Excel;
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
            result.Add(new WebinarDateDto { Id = d.Id, Name = d.Name, Date = d.Date, MeetingLink = d.MeetingLink, RegistrationCount = count, CreatedAt = d.CreatedAt });
        }

        return Ok(result);
    }

    // POST /api/webinar/dates
    [HttpPost("dates")]
    public async Task<IActionResult> CreateDate([FromBody] CreateWebinarDateRequest request)
    {
        var date = new WebinarDate
        {
            Name = request.Name?.Trim() ?? "",
            Date = request.Date,
            MeetingLink = request.MeetingLink
        };

        _db.WebinarDates.Add(date);
        await _db.SaveChangesAsync();

        return Created($"/api/webinar/dates/{date.Id}",
            new WebinarDateDto { Id = date.Id, Name = date.Name, Date = date.Date, MeetingLink = date.MeetingLink, RegistrationCount = 0, CreatedAt = date.CreatedAt });
    }

    // PUT /api/webinar/dates/{id}
    [HttpPut("dates/{id}")]
    public async Task<IActionResult> UpdateDate(int id, [FromBody] CreateWebinarDateRequest request)
    {
        var date = await _db.WebinarDates.FindAsync(id);
        if (date is null) return NotFound();

        date.Name = request.Name?.Trim() ?? "";
        date.Date = request.Date;
        date.MeetingLink = request.MeetingLink;
        await _db.SaveChangesAsync();

        var count = await _db.WebinarRegistrations.CountAsync(r => r.WebinarDateId == id);
        return Ok(new WebinarDateDto { Id = date.Id, Name = date.Name, Date = date.Date, MeetingLink = date.MeetingLink, RegistrationCount = count, CreatedAt = date.CreatedAt });
    }

    // GET /api/webinar/dates/{id}/registrations
    [HttpGet("dates/{id}/registrations")]
    public async Task<IActionResult> GetDateRegistrations(int id)
    {
        var registrations = await _db.WebinarRegistrations
            .Where(r => r.WebinarDateId == id)
            .Join(_db.WebinarContacts, r => r.ContactId, c => c.Id, (r, c) => new
            {
                c.FullName,
                c.Email,
                c.Phone,
                c.Company,
                r.RegisteredAt
            })
            .OrderByDescending(x => x.RegisteredAt)
            .ToListAsync();

        return Ok(registrations);
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
                Id = c.Id, Email = c.Email, FullName = c.FullName,
                Phone = c.Phone, Company = c.Company, Tag = c.Tag, UUID = c.UUID, WebinarDateId = c.WebinarDateId,
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
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim(),
            UUID = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _db.WebinarContacts.Add(contact);
        await _db.SaveChangesAsync();

        return Created($"/api/webinar/contacts/{contact.Id}",
            new WebinarContactDto
            {
                Id = contact.Id, Email = contact.Email, FullName = contact.FullName,
                Phone = contact.Phone, Company = contact.Company, Tag = contact.Tag, UUID = contact.UUID,
                HasRegistration = false, CreatedAt = contact.CreatedAt
            });
    }

    // PUT /api/webinar/contacts/{id}
    [HttpPut("contacts/{id}")]
    public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdateWebinarContactRequest request)
    {
        var contact = await _db.WebinarContacts.FindAsync(id);
        if (contact is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Email y nombre son obligatorios" });

        // Check email uniqueness if changed
        if (contact.Email != request.Email.Trim())
        {
            var emailExists = await _db.WebinarContacts.AnyAsync(c => c.Email == request.Email.Trim() && c.Id != id);
            if (emailExists)
                return BadRequest(new { message = "Ya existe un contacto con ese email" });
        }

        contact.FullName = request.FullName.Trim();
        contact.Email = request.Email.Trim();
        contact.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        contact.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();
        contact.Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim();

        await _db.SaveChangesAsync();

        var hasReg = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == contact.Id);
        string? dateDisplay = null;
        if (contact.WebinarDateId != null)
        {
            var wd = await _db.WebinarDates.FindAsync(contact.WebinarDateId);
            if (wd != null) dateDisplay = wd.Date.ToString("dd/MM/yyyy HH:mm");
        }

        return Ok(new WebinarContactDto
        {
            Id = contact.Id, Email = contact.Email, FullName = contact.FullName,
            Phone = contact.Phone, Company = contact.Company, Tag = contact.Tag, UUID = contact.UUID,
            WebinarDateId = contact.WebinarDateId, WebinarDateDisplay = dateDisplay,
            HasRegistration = hasReg, CreatedAt = contact.CreatedAt
        });
    }

    // POST /api/webinar/contacts/assign
    [HttpPost("contacts/assign")]
    public async Task<IActionResult> AssignContacts([FromBody] AssignWebinarRequest request)
    {
        if (request.ContactIds is null || request.ContactIds.Count == 0)
            return BadRequest(new { message = "No se seleccionaron contactos" });

        var dateExists = await _db.WebinarDates.AnyAsync(d => d.Id == request.WebinarDateId);
        if (!dateExists)
            return BadRequest(new { message = "Fecha de webinar no valida" });

        var assigned = 0;
        var skipped = 0;

        foreach (var contactId in request.ContactIds)
        {
            var contact = await _db.WebinarContacts.FindAsync(contactId);
            if (contact is null) { skipped++; continue; }

            var alreadyRegistered = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == contactId);
            if (alreadyRegistered) { skipped++; continue; }

            contact.WebinarDateId = request.WebinarDateId;
            assigned++;
        }

        await _db.SaveChangesAsync();

        return Ok(new { assigned, skipped, message = $"{assigned} contactos asignados al webinar, {skipped} omitidos" });
    }

    // GET /api/webinar/contacts/export
    [HttpGet("contacts/export")]
    public async Task<IActionResult> ExportContacts()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var contacts = await _db.WebinarContacts
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Contactos");

        // Headers
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Nombre y Apellido";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Telefono";
        ws.Cell(1, 5).Value = "Empresa";
        ws.Cell(1, 6).Value = "Tag";
        ws.Cell(1, 7).Value = "Link";
        ws.Cell(1, 8).Value = "Inscripto";
        ws.Cell(1, 9).Value = "Fecha inscripcion";

        var headerRange = ws.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var c in contacts)
        {
            var hasReg = await _db.WebinarRegistrations.AnyAsync(r => r.ContactId == c.Id);
            string? dateDisplay = null;
            if (c.WebinarDateId != null)
            {
                var wd = await _db.WebinarDates.FindAsync(c.WebinarDateId);
                if (wd != null) dateDisplay = wd.Date.ToString("dd/MM/yyyy HH:mm");
            }

            ws.Cell(row, 1).Value = c.Id;
            ws.Cell(row, 2).Value = c.FullName;
            ws.Cell(row, 3).Value = c.Email;
            ws.Cell(row, 4).Value = c.Phone ?? "";
            ws.Cell(row, 5).Value = c.Company ?? "";
            ws.Cell(row, 6).Value = c.Tag ?? "";
            ws.Cell(row, 7).Value = $"{baseUrl}/webinar/{c.UUID}";
            ws.Cell(row, 8).Value = hasReg ? "Si" : "No";
            ws.Cell(row, 9).Value = dateDisplay ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "contactos_webinar.xlsx");
    }

    // POST /api/webinar/contacts/import
    [HttpPost("contacts/import")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> ImportContacts(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se recibio un archivo" });

        var imported = 0;
        var updated = 0;
        var skipped = 0;

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

            // Skip header row (row 1)
            for (int r = 2; r <= lastRow; r++)
            {
                var idStr = ws.Cell(r, 1).GetString().Trim();
                var fullName = ws.Cell(r, 2).GetString().Trim();
                var email = ws.Cell(r, 3).GetString().Trim();
                var phone = ws.Cell(r, 4).GetString().Trim();
                var company = ws.Cell(r, 5).GetString().Trim();
                var tag = ws.Cell(r, 6).GetString().Trim();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
                {
                    skipped++;
                    continue;
                }

                // Si viene un ID, buscar contacto existente para actualizar
                if (int.TryParse(idStr, out var id) && id > 0)
                {
                    var existing = await _db.WebinarContacts.FindAsync(id);
                    if (existing != null)
                    {
                        existing.FullName = fullName;
                        existing.Email = email;
                        existing.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
                        existing.Company = string.IsNullOrWhiteSpace(company) ? null : company;
                        existing.Tag = string.IsNullOrWhiteSpace(tag) ? null : tag;
                        updated++;
                        continue;
                    }
                }

                // Sin ID o ID no encontrado: verificar duplicado por email
                var existsByEmail = await _db.WebinarContacts.AnyAsync(c => c.Email == email);
                if (existsByEmail)
                {
                    skipped++;
                    continue;
                }

                _db.WebinarContacts.Add(new WebinarContact
                {
                    FullName = fullName,
                    Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    Company = string.IsNullOrWhiteSpace(company) ? null : company,
                    Tag = string.IsNullOrWhiteSpace(tag) ? null : tag,
                    UUID = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow
                });
                imported++;
            }

            await _db.SaveChangesAsync();
        }
        catch
        {
            return BadRequest(new { message = "Error al leer el archivo. Asegurate de que sea un Excel valido (.xlsx)" });
        }

        return Ok(new { imported, updated, skipped, message = $"{imported} importados, {updated} actualizados, {skipped} omitidos" });
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
                Name = d.Name,
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
            .Select(d => new WebinarDateOptionDto { Id = d.Id, Name = d.Name, Date = d.Date })
            .ToListAsync();

        return Ok(new WebinarFormDataDto
        {
            FullName = contact.FullName,
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
