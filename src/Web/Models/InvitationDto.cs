namespace Web.Models;

public class InvitationDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

public class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
}
