using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record InvitationDto(
    int Id,
    string Email,
    string RoleName,
    string Token,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? UsedAt,
    DateTime ExpiresAt,
    bool IsExpired
);

public record CreateInvitationRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required] int RoleId
);
