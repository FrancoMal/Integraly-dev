namespace Web.Models;

public class IntegrationDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? AppId { get; set; }
    public bool HasSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Settings { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SaveIntegrationRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Settings { get; set; }
    public bool IsActive { get; set; }
}

public class MeliAccountDto
{
    public int Id { get; set; }
    public long MeliUserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool TokenValid { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeliAuthUrlDto
{
    public string Url { get; set; } = string.Empty;
}

public class AiModelDto
{
    public string Id { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class WhatsAppStatusDto
{
    public bool Linked { get; set; }
    public string? Info { get; set; }
    public bool IsLinking { get; set; }
}

public class WhatsAppCheckDto
{
    public bool Linked { get; set; }
}
