using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class InvitationService
{
    private readonly AppDbContext _db;

    public InvitationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<InvitationDto>> GetAllAsync()
    {
        return await _db.Invitations
            .Include(i => i.Role)
            .Include(i => i.CreatedByUser)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationDto(
                i.Id,
                i.Email,
                i.Role != null ? i.Role.Name : "",
                i.Token,
                i.CreatedByUser != null ? i.CreatedByUser.Username : "",
                i.CreatedAt,
                i.UsedAt,
                i.ExpiresAt,
                i.ExpiresAt < DateTime.UtcNow || i.UsedAt != null
            ))
            .ToListAsync();
    }

    public async Task<InvitationDto> CreateAsync(string email, int roleId, int createdBy)
    {
        var invitation = new Invitation
        {
            Email = email,
            RoleId = roleId,
            Token = Guid.NewGuid().ToString(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var role = await _db.Roles.FindAsync(roleId);
        var creator = await _db.Users.FindAsync(createdBy);

        return new InvitationDto(
            invitation.Id,
            invitation.Email,
            role?.Name ?? "",
            invitation.Token,
            creator?.Username ?? "",
            invitation.CreatedAt,
            invitation.UsedAt,
            invitation.ExpiresAt,
            false
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var invitation = await _db.Invitations.FindAsync(id);
        if (invitation is null || invitation.UsedAt is not null) return false;

        _db.Invitations.Remove(invitation);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<InvitationDto?> GetByTokenAsync(string token)
    {
        return await _db.Invitations
            .Include(i => i.Role)
            .Include(i => i.CreatedByUser)
            .Where(i => i.Token == token)
            .Select(i => new InvitationDto(
                i.Id,
                i.Email,
                i.Role != null ? i.Role.Name : "",
                i.Token,
                i.CreatedByUser != null ? i.CreatedByUser.Username : "",
                i.CreatedAt,
                i.UsedAt,
                i.ExpiresAt,
                i.ExpiresAt < DateTime.UtcNow || i.UsedAt != null
            ))
            .FirstOrDefaultAsync();
    }
}
