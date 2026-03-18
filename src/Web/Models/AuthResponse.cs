namespace Web.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Timezone { get; set; } = "America/Argentina/Buenos_Aires";
    public DateTime ExpiresAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}
