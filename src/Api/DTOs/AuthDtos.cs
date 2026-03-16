using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record RegisterWithInvitationRequest(
    [Required] string Token,
    [Required][MinLength(6)] string Password,
    [Required][MaxLength(100)] string FirstName,
    [Required][MaxLength(100)] string LastName,
    [MaxLength(50)] string? Phone
);

public record AuthResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt,
    List<string> Permissions
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    string? VpsInfo,
    DateTime CreatedAt,
    bool IsActive
);
