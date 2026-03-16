using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record TokenPackDto(
    int Id,
    int UserId,
    string UserName,
    int TotalTokens,
    int RemainingTokens,
    string CreatedByName,
    string? Description,
    DateTime CreatedAt
);

public record CreateTokenPackRequest(
    [Required] int UserId,
    [Required][Range(1, 10000)] int TotalTokens,
    [MaxLength(500)] string? Description
);
