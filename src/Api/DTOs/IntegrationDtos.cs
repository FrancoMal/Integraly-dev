namespace Api.DTOs;

public record IntegrationDto(
    int Id,
    string Provider,
    string? AppId,
    bool HasSecret,
    string? RedirectUrl,
    string? Settings,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SaveIntegrationRequest(
    string Provider,
    string? AppId,
    string? AppSecret,
    string? RedirectUrl,
    string? Settings,
    bool IsActive
);
