namespace Web.Models;

public class TokenPackDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public int RemainingTokens { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTokenPackRequest
{
    public int UserId { get; set; }
    public int TotalTokens { get; set; }
    public string Description { get; set; } = string.Empty;
}
