using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    private readonly NavigationManager _navigation;

    public ApiClient(HttpClient http, AuthService authService, NavigationManager navigation)
    {
        _http = http;
        _authService = authService;
        _navigation = navigation;
    }

    // --- Dashboard ---
    public async Task<AdminDashboardDto?> GetAdminDashboardAsync()
    {
        return await GetAsync<AdminDashboardDto>("/api/dashboard");
    }

    public async Task<InstructorDashboardDto?> GetInstructorDashboardAsync()
    {
        return await GetAsync<InstructorDashboardDto>("/api/dashboard");
    }

    public async Task<UserDashboardDto?> GetUserDashboardAsync()
    {
        return await GetAsync<UserDashboardDto>("/api/dashboard");
    }

    public async Task<UserDto?> GetMeAsync()
    {
        return await GetAsync<UserDto>("/api/auth/me");
    }

    // --- Users ---
    public async Task<List<UserManageDto>?> GetUsersAsync()
    {
        return await GetAsync<List<UserManageDto>>("/api/users");
    }

    public async Task<UserManageDto?> CreateUserAsync(CreateUserRequest request)
    {
        return await PostAsync<UserManageDto>("/api/users", request);
    }

    public async Task<UserManageDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        return await PutAsync<UserManageDto>($"/api/users/{id}", request);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        return await DeleteAsync($"/api/users/{id}");
    }

    public async Task<List<UserManageDto>?> GetInstructorsAsync()
    {
        return await GetAsync<List<UserManageDto>>("/api/users/instructors");
    }

    // --- Roles ---
    public async Task<List<RoleDto>?> GetRolesAsync()
    {
        return await GetAsync<List<RoleDto>>("/api/roles");
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request)
    {
        return await PostAsync<RoleDto>("/api/roles", request);
    }

    public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        return await PutAsync<RoleDto>($"/api/roles/{id}", request);
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        return await DeleteAsync($"/api/roles/{id}");
    }

    public async Task<List<MenuTreeDto>?> GetMenuTreeAsync()
    {
        return await GetAsync<List<MenuTreeDto>>("/api/roles/menu-tree");
    }

    // --- Profile ---
    public async Task<ProfileDto?> GetProfileAsync()
    {
        return await GetAsync<ProfileDto>("/api/auth/profile");
    }

    public async Task<ProfileDto?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        return await PutAsync<ProfileDto>("/api/auth/profile", request);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("/api/auth/password", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("login", forceLoad: true);
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    // --- Invitations ---
    public async Task<List<InvitationDto>?> GetInvitationsAsync()
    {
        return await GetAsync<List<InvitationDto>>("/api/invitations");
    }

    public async Task<InvitationDto?> CreateInvitationAsync(CreateInvitationRequest request)
    {
        return await PostAsync<InvitationDto>("/api/invitations", request);
    }

    public async Task<bool> DeleteInvitationAsync(int id)
    {
        return await DeleteAsync($"/api/invitations/{id}");
    }

    public async Task<InvitationDto?> GetInvitationByTokenAsync(string token)
    {
        return await GetAsync<InvitationDto>($"/api/invitations/token/{token}");
    }

    // --- Token Packs ---
    public async Task<List<TokenPackDto>?> GetAllTokenPacksAsync()
    {
        return await GetAsync<List<TokenPackDto>>("/api/tokenpacks");
    }

    public async Task<List<TokenPackDto>?> GetMyTokenPacksAsync()
    {
        return await GetAsync<List<TokenPackDto>>("/api/tokenpacks/my");
    }

    public async Task<TokenPackDto?> CreateTokenPackAsync(CreateTokenPackRequest request)
    {
        return await PostAsync<TokenPackDto>("/api/tokenpacks", request);
    }

    // --- Availability ---
    public async Task<List<AvailabilityDto>?> GetInstructorAvailabilityAsync(int instructorId)
    {
        return await GetAsync<List<AvailabilityDto>>($"/api/availability/instructor/{instructorId}");
    }

    public async Task<List<AvailabilityDto>?> GetMyAvailabilityAsync()
    {
        return await GetAsync<List<AvailabilityDto>>("/api/availability/my");
    }

    public async Task<List<AvailabilityDto>?> SetAvailabilityAsync(List<SetAvailabilityRequest> slots)
    {
        return await PutAsync<List<AvailabilityDto>>("/api/availability", slots);
    }

    // --- Week Availability ---
    public async Task<List<WeekAvailabilityDto>?> GetMyWeekAvailabilityAsync(DateTime from, DateTime to)
    {
        return await GetAsync<List<WeekAvailabilityDto>>($"/api/availability/my-week?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
    }

    public async Task<List<WeekAvailabilityDto>?> GetWeekAvailabilityAsync(int instructorId, DateTime from, DateTime to)
    {
        return await GetAsync<List<WeekAvailabilityDto>>($"/api/availability/week/{instructorId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
    }

    public async Task<List<WeekAvailabilityDto>?> SetWeekAvailabilityAsync(DateTime weekStart, List<object> slots)
    {
        return await PutAsync<List<WeekAvailabilityDto>>("/api/availability/week", new { weekStart, slots });
    }

    // --- Bookings ---
    public async Task<List<BookingDto>?> GetAllBookingsAsync()
    {
        return await GetAsync<List<BookingDto>>("/api/bookings");
    }

    public async Task<List<BookingDto>?> GetMyBookingsAsync()
    {
        return await GetAsync<List<BookingDto>>("/api/bookings/my");
    }

    public async Task<BookingDto?> CreateBookingAsync(CreateBookingRequest request)
    {
        return await PostAsync<BookingDto>("/api/bookings", request);
    }

    public async Task<bool> CancelBookingAsync(int id)
    {
        return await DeleteAsync($"/api/bookings/{id}");
    }

    public async Task<List<BookingDto>?> GetWeekBookingsAsync(DateTime from, DateTime to)
    {
        return await GetAsync<List<BookingDto>>($"/api/bookings/week?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
    }

    public async Task<List<AvailableSlotDto>?> GetAvailableSlotsAsync(int instructorId, DateTime date)
    {
        return await GetAsync<List<AvailableSlotDto>>($"/api/bookings/available-slots?instructorId={instructorId}&date={date:yyyy-MM-dd}");
    }

    public async Task<TokenPackDto?> UpdateTokenPackAsync(int id, object request)
    {
        return await PutAsync<TokenPackDto>($"/api/tokenpacks/{id}", request);
    }

    // --- Fichajes (Time Entries) ---
    public async Task<List<TimeEntryDto>?> GetMyTimeEntriesAsync(DateTime from, DateTime to)
    {
        return await GetAsync<List<TimeEntryDto>>($"/api/fichajes/my?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
    }

    public async Task<List<TimeEntryDto>?> GetTimeEntriesAsync(int instructorId, DateTime from, DateTime to)
    {
        return await GetAsync<List<TimeEntryDto>>($"/api/fichajes?instructorId={instructorId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
    }

    public async Task<List<InstructorListItemDto>?> GetFichajeInstructorsAsync()
    {
        return await GetAsync<List<InstructorListItemDto>>("/api/fichajes/instructors");
    }

    public async Task<TimeEntryDto?> CreateTimeEntryAsync(CreateTimeEntryRequest request)
    {
        return await PostAsync<TimeEntryDto>("/api/fichajes", request);
    }

    public async Task<TimeEntryDto?> UpdateTimeEntryAsync(int id, UpdateTimeEntryRequest request)
    {
        return await PutAsync<TimeEntryDto>($"/api/fichajes/{id}", request);
    }

    public async Task<bool> DeleteTimeEntryAsync(int id)
    {
        return await DeleteAsync($"/api/fichajes/{id}");
    }

    // --- Audit Logs ---
    public async Task<AuditLogListResponse?> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? entityType = null, int page = 1)
    {
        var url = $"/api/audit-logs?page={page}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-ddTHH:mm:ss}";
        if (to.HasValue) url += $"&to={to.Value:yyyy-MM-ddTHH:mm:ss}";
        if (!string.IsNullOrEmpty(entityType)) url += $"&entityType={entityType}";
        return await GetAsync<AuditLogListResponse>(url);
    }

    // --- Settings ---
    public async Task<Dictionary<string, string>?> GetSettingsAsync()
    {
        return await GetAsync<Dictionary<string, string>>("/api/settings");
    }

    public async Task<bool> UpdateSettingAsync(string key, string value)
    {
        var result = await PutAsync<object>($"/api/settings/{key}", new { Value = value });
        return result != null;
    }

    public async Task<bool> SendTestEmailAsync(string to)
    {
        try
        {
            await PostAsync<object>("/api/settings/test-email", new { to });
            return true;
        }
        catch { return false; }
    }

    // --- Register (public, no auth) ---
    public async Task<bool> RegisterWithInvitationAsync(object request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register-invitation", request);
        return response.IsSuccessStatusCode;
    }

    // --- HTTP helpers ---
    private bool IsOnLoginPage()
    {
        try { return _navigation.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    private async Task HandleUnauthorizedAsync()
    {
        await _authService.LogoutAsync();
        if (!IsOnLoginPage())
            _navigation.NavigateTo("login", forceLoad: true);
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PostAsync<T>(string url, object data)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, data);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PutAsync<T>(string url, object data)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync(url, data);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<bool> DeleteAsync(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
