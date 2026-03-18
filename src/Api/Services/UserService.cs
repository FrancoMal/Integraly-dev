using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserListDto>> GetAllAsync()
    {
        return await _db.Users
            .Include(u => u.RoleNav)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto(
                u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.Phone,
                u.RoleNav != null ? u.RoleNav.Name : "usuario",
                u.RoleId, u.VpsInfo, u.Timezone, u.CreatedAt, u.IsActive
            ))
            .ToListAsync();
    }

    public async Task<UserListDto?> GetByIdAsync(int id)
    {
        return await _db.Users
            .Include(u => u.RoleNav)
            .Where(u => u.Id == id)
            .Select(u => new UserListDto(
                u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.Phone,
                u.RoleNav != null ? u.RoleNav.Name : "usuario",
                u.RoleId, u.VpsInfo, u.Timezone, u.CreatedAt, u.IsActive
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<UserListDto?> CreateAsync(CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return null;
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var role = await _db.Roles.FindAsync(request.RoleId);
        if (role is null) return null;

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            RoleId = request.RoleId,
            VpsInfo = request.VpsInfo,
            Timezone = request.Timezone ?? "America/Argentina/Buenos_Aires",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserListDto(
            user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.Phone,
            role.Name, user.RoleId, user.VpsInfo, user.Timezone, user.CreatedAt, user.IsActive
        );
    }

    public async Task<UserListDto?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.RoleNav).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return null;

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.Phone is not null) user.Phone = request.Phone;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.VpsInfo is not null) user.VpsInfo = request.VpsInfo;
        if (request.Timezone is not null) user.Timezone = request.Timezone;

        if (request.Email is not null && request.Email != user.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                return null;
            user.Email = request.Email;
        }

        if (request.RoleId.HasValue && request.RoleId.Value != user.RoleId)
        {
            var role = await _db.Roles.FindAsync(request.RoleId.Value);
            if (role is null) return null;
            user.RoleId = request.RoleId.Value;
        }

        await _db.SaveChangesAsync();

        var roleName = user.RoleNav?.Name ?? "usuario";
        return new UserListDto(
            user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.Phone,
            roleName, user.RoleId, user.VpsInfo, user.Timezone, user.CreatedAt, user.IsActive
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}
