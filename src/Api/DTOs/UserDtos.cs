using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record UserListDto(
    int Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string Role,
    int RoleId,
    string? VpsInfo,
    string Timezone,
    DateTime CreatedAt,
    bool IsActive
);

public record CreateUserRequest(
    [Required][MaxLength(100)] string Username,
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MinLength(6)] string Password,
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [MaxLength(50)] string? Phone,
    [Required] int RoleId,
    string? VpsInfo,
    string? Timezone
);

public class UpdateUserRequest
{
    [MaxLength(100)]
    public string? Username { get; set; }
    [MaxLength(100)]
    public string? FirstName { get; set; }
    [MaxLength(100)]
    public string? LastName { get; set; }
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }
    [MaxLength(50)]
    public string? Phone { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public string? VpsInfo { get; set; }
    public string? Timezone { get; set; }
}
