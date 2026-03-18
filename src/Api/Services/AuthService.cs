using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> Login(string usernameOrEmail, string password)
    {
        var user = await _db.Users.Include(u => u.RoleNav)
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse?> RegisterWithInvitation(string token, string password, string firstName, string lastName, string? phone)
    {
        var invitation = await _db.Invitations
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Token == token && i.UsedAt == null && i.ExpiresAt > DateTime.UtcNow);

        if (invitation is null)
            return null;

        // Check if email already registered
        if (await _db.Users.AnyAsync(u => u.Email == invitation.Email))
            return null;

        // Username defaults to email
        var username = invitation.Email.ToLower();

        var user = new User
        {
            Username = username,
            Email = invitation.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            RoleId = invitation.RoleId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);

        // Mark invitation as used
        invitation.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var expirationHours = _config.GetValue<int>("Jwt:ExpirationHours", 24);
        var expiresAt = DateTime.UtcNow.AddHours(expirationHours);

        var roleName = user.RoleNav?.Name ?? "usuario";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        // Get permissions for this role
        List<string> permissions;
        if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            permissions = MenuDefinition.AllMenuKeys;
        }
        else
        {
            permissions = await _db.RolePermissions
                .Where(rp => rp.RoleId == user.RoleId)
                .Select(rp => rp.MenuKey)
                .ToListAsync();
        }

        return new AuthResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Username: user.Username,
            Role: roleName,
            Timezone: user.Timezone,
            ExpiresAt: expiresAt,
            Permissions: permissions
        );
    }
}
