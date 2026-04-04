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

    public async Task<List<StudentSummaryDto>?> GetStudentsSummaryAsync()
    {
        return await GetAsync<List<StudentSummaryDto>>("/api/dashboard/students-summary");
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

    public async Task<AuthResponse?> ImpersonateUserAsync(int userId)
    {
        return await PostAsync<AuthResponse>($"/api/auth/impersonate/{userId}", new { });
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

    public async Task<bool> AdminToggleAvailabilityAsync(int instructorId, DateTime date, int startHour, bool isActive)
    {
        var result = await PutAsync<object>("/api/availability/admin-toggle", new { instructorId, date = date.ToString("yyyy-MM-dd"), startHour, isActive });
        return result != null;
    }

    public async Task<bool> AdminBulkToggleAvailabilityAsync(int instructorId, bool isActive, List<object> slots)
    {
        var result = await PutAsync<object>("/api/availability/admin-bulk-toggle", new { instructorId, isActive, slots });
        return result != null;
    }

    public async Task<bool> CopyPreviousWeekAvailabilityAsync(DateTime targetWeekStart)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync("/api/availability/copy-previous-week",
                new { targetWeekStart = targetWeekStart.ToString("yyyy-MM-dd") });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
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

    public async Task<BookingDto?> AdminCreateBookingAsync(AdminCreateBookingRequest request)
    {
        return await PostAsync<BookingDto>("/api/bookings/admin", request);
    }

    public async Task<bool> AdminCancelBookingAsync(int id)
    {
        return await DeleteAsync($"/api/bookings/{id}/admin-cancel");
    }

    public async Task<bool> AdminCompleteBookingAsync(int id)
    {
        var result = await PutAsync<object>($"/api/bookings/{id}/complete", new { });
        return result != null;
    }

    public async Task<BookingDto?> UpdateBookingNotesAsync(int id, object request)
    {
        return await PutAsync<BookingDto>($"/api/bookings/{id}/notes", request);
    }

    public async Task<TokenPackDto?> UpdateTokenPackAsync(int id, object request)
    {
        return await PutAsync<TokenPackDto>($"/api/tokenpacks/{id}", request);
    }

    public async Task<bool> DeleteTokenPackAsync(int id)
    {
        return await DeleteAsync($"/api/tokenpacks/{id}");
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

    // --- Instructor Tasks ---
    public async Task<List<InstructorTaskDto>?> GetInstructorTasksAsync(int? instructorId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = "/api/instructor-tasks?";
        if (instructorId.HasValue) query += $"instructorId={instructorId}&";
        if (from.HasValue) query += $"from={from:yyyy-MM-dd}&";
        if (to.HasValue) query += $"to={to:yyyy-MM-dd}&";
        return await GetAsync<List<InstructorTaskDto>>(query.TrimEnd('&', '?'));
    }

    public async Task<InstructorTaskDto?> CreateInstructorTaskAsync(CreateInstructorTaskRequest request)
        => await PostAsync<InstructorTaskDto>("/api/instructor-tasks", request);

    public async Task<InstructorTaskDto?> UpdateInstructorTaskAsync(int id, UpdateInstructorTaskRequest request)
        => await PutAsync<InstructorTaskDto>($"/api/instructor-tasks/{id}", request);

    public async Task<bool> CompleteInstructorTaskAsync(int id)
    {
        var result = await PutAsync<object>($"/api/instructor-tasks/{id}/complete", new { });
        return result != null;
    }

    public async Task<bool> DeleteInstructorTaskAsync(int id)
        => await DeleteAsync($"/api/instructor-tasks/{id}");

    // --- Webinar ---
    public async Task<List<WebinarDateDto>?> GetWebinarDatesAsync()
    {
        return await GetAsync<List<WebinarDateDto>>("/api/webinar/dates");
    }

    public async Task<WebinarDateDto?> CreateWebinarDateAsync(CreateWebinarDateRequest request)
    {
        return await PostAsync<WebinarDateDto>("/api/webinar/dates", request);
    }

    public async Task<WebinarDateDto?> UpdateWebinarDateAsync(int id, CreateWebinarDateRequest request)
    {
        return await PutAsync<WebinarDateDto>($"/api/webinar/dates/{id}", request);
    }

    public async Task<bool> DeleteWebinarDateAsync(int id)
    {
        return await DeleteAsync($"/api/webinar/dates/{id}");
    }

    public async Task<List<WebinarRegistrationDto>?> GetDateRegistrationsAsync(int dateId)
    {
        return await GetAsync<List<WebinarRegistrationDto>>($"/api/webinar/dates/{dateId}/registrations");
    }

    public async Task<List<WebinarContactDto>?> GetWebinarContactsAsync()
    {
        return await GetAsync<List<WebinarContactDto>>("/api/webinar/contacts");
    }

    public async Task<WebinarContactDto?> CreateWebinarContactAsync(CreateWebinarContactRequest request)
    {
        return await PostAsync<WebinarContactDto>("/api/webinar/contacts", request);
    }

    public async Task<List<ContactHistoryDto>?> GetWebinarContactHistoryAsync(int id)
    {
        return await GetAsync<List<ContactHistoryDto>>($"/api/webinar/contacts/{id}/history");
    }

    public async Task<WebinarContactDto?> UpdateWebinarContactAsync(int id, UpdateWebinarContactRequest request)
    {
        return await PutAsync<WebinarContactDto>($"/api/webinar/contacts/{id}", request);
    }

    public async Task<bool> DeleteWebinarContactAsync(int id)
    {
        return await DeleteAsync($"/api/webinar/contacts/{id}");
    }

    public async Task<AssignWebinarResult?> AssignContactsToWebinarAsync(AssignWebinarRequest request)
    {
        return await PostAsync<AssignWebinarResult>("/api/webinar/contacts/assign", request);
    }

    public async Task<byte[]?> ExportWebinarContactsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("/api/webinar/contacts/export");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<ImportContactsResult?> ImportWebinarContactsFileAsync(byte[] fileBytes, string fileName)
    {
        await SetAuthHeaderAsync();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);

        var response = await _http.PostAsync("/api/webinar/contacts/import", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleUnauthorizedAsync();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImportContactsResult>();
    }

    public async Task<WebinarStatsDto?> GetWebinarStatsAsync()
    {
        return await GetAsync<WebinarStatsDto>("/api/webinar/stats");
    }

    public async Task<WebinarFormDataDto?> GetWebinarFormAsync(string uuid)
    {
        return await GetAsync<WebinarFormDataDto>($"/api/webinar/form/{uuid}");
    }

    public async Task<bool> SubmitWebinarFormAsync(string uuid, WebinarFormSubmitRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync($"/api/webinar/form/{uuid}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // --- Payments ---
    public async Task<List<PaymentPlanDto>?> GetPaymentPlansAsync()
        => await GetAsync<List<PaymentPlanDto>>("/api/payments/plans");

    public async Task<List<PaymentPlanDto>?> GetAllPaymentPlansAsync()
        => await GetAsync<List<PaymentPlanDto>>("/api/payments/plans/all");

    public async Task<CountryDetectResult?> DetectCountryAsync(string? countryOverride = null)
    {
        var url = "/api/payments/country";
        if (!string.IsNullOrEmpty(countryOverride)) url += $"?u={countryOverride}";
        return await GetAsync<CountryDetectResult>(url);
    }

    public async Task<CreatePaymentResponse?> CreatePaymentAsync(int planId, string provider = "mercadopago")
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/payments/create", new { planId, provider });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"CreatePayment response: {json}");
        return System.Text.Json.JsonSerializer.Deserialize<CreatePaymentResponse>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<PaymentDto>?> GetMyPaymentsAsync()
        => await GetAsync<List<PaymentDto>>("/api/payments/my");

    public async Task<PaymentPlanDto?> CreatePaymentPlanAsync(PlanRequestDto request)
        => await PostAsync<PaymentPlanDto>("/api/payments/plans", request);

    public async Task<PaymentPlanDto?> UpdatePaymentPlanAsync(int id, PlanRequestDto request)
        => await PutAsync<PaymentPlanDto>($"/api/payments/plans/{id}", request);

    public async Task<bool> DeletePaymentPlanAsync(int id)
        => await DeleteAsync($"/api/payments/plans/{id}");

    public async Task<List<AdminPaymentDto>?> GetAllPaymentsAsync()
        => await GetAsync<List<AdminPaymentDto>>("/api/payments");

    public async Task<bool> ApprovePaymentAsync(int id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"/api/payments/{id}/approve", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePaymentAsync(int id)
        => await DeleteAsync($"/api/payments/{id}");

    public async Task<TransferPaymentResponse?> GetTransferInfoAsync()
        => await GetAsync<TransferPaymentResponse>("/api/payments/transfer-info");

    public async Task<TransferPaymentResponse?> CreateTransferPaymentAsync(int planId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/payments/create", new { planId, provider = "transferencia" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<TransferPaymentResponse>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<bool> UploadTransferReceiptAsync(int paymentId, Stream fileStream, string fileName)
    {
        await SetAuthHeaderAsync();
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);
        var response = await _http.PostAsync($"/api/payments/{paymentId}/receipt", content);
        return response.IsSuccessStatusCode;
    }

    // --- Backup ---
    public async Task<List<BackupDto>?> GetBackupsAsync()
    {
        return await GetAsync<List<BackupDto>>("/api/backup");
    }

    public async Task<BackupDto?> CreateBackupAsync()
    {
        return await PostAsync<BackupDto>("/api/backup", new { });
    }

    public async Task<bool> DeleteBackupAsync(int id)
    {
        return await DeleteAsync($"/api/backup/{id}");
    }

    public async Task<BackupScheduleDto?> GetBackupScheduleAsync()
    {
        return await GetAsync<BackupScheduleDto>("/api/backup/schedule");
    }

    public async Task<bool> UpdateBackupScheduleAsync(BackupScheduleDto dto)
    {
        var result = await PutAsync<object>("/api/backup/schedule", dto);
        return result != null;
    }

    public async Task<byte[]?> DownloadBackupAsync(int id)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.GetAsync($"/api/backup/{id}/download");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                await HandleUnauthorizedAsync();
            return null;
        }
        catch { return null; }
    }

    public async Task<BackupDto?> UploadBackupAsync(Stream fileStream, string fileName)
    {
        await SetAuthHeaderAsync();
        try
        {
            // Read stream into memory first (Blazor WASM streams can fail with MultipartFormDataContent)
            using var ms = new System.IO.MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Position = 0;

            using var content = new MultipartFormDataContent();
            var streamContent = new ByteArrayContent(ms.ToArray());
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);
            var response = await _http.PostAsync("/api/backup/upload", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<BackupDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                await HandleUnauthorizedAsync();
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Upload failed: {response.StatusCode} - {errorBody}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload exception: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    public async Task<BackupInfoDto?> GetBackupInfoAsync(int id)
    {
        return await GetAsync<BackupInfoDto>($"/api/backup/{id}/info");
    }

    public async Task<(bool Success, string Message)> RestoreBackupAsync(int id)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsync($"/api/backup/{id}/restore", null);
            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (true, "Base de datos restaurada correctamente");
            }
            // Try to parse error message
            try
            {
                var error = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (false, error?.GetValueOrDefault("error") ?? "Error desconocido");
            }
            catch { return (false, "Error al restaurar la base de datos"); }
        }
        catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
    }

    // --- Integrations ---
    public async Task<List<IntegrationDto>?> GetIntegrationsAsync()
        => await GetAsync<List<IntegrationDto>>("/api/integrations");

    public async Task<IntegrationDto?> GetIntegrationAsync(string provider)
        => await GetAsync<IntegrationDto>($"/api/integrations/{provider}");

    public async Task<IntegrationDto?> SaveIntegrationAsync(SaveIntegrationRequest request)
        => await PostAsync<IntegrationDto>("/api/integrations", request);

    public async Task<bool> DeleteIntegrationAsync(string provider)
        => await DeleteAsync($"/api/integrations/{provider}");

    public async Task<List<AiModelDto>?> GetOpenAiModelsAsync()
        => await GetAsync<List<AiModelDto>>("/api/integrations/openai/models");

    public async Task<List<AiModelDto>?> GetClaudeModelsAsync()
        => await GetAsync<List<AiModelDto>>("/api/integrations/claude/models");

    public async Task<bool> TestEmailIntegrationAsync()
    {
        try
        {
            await PostAsync<object>("/api/integrations/email-smtp/test", new { });
            return true;
        }
        catch { return false; }
    }

    // --- MercadoLibre ---
    public async Task<List<MeliAccountDto>?> GetMeliAccountsAsync()
        => await GetAsync<List<MeliAccountDto>>("/api/meli/accounts");

    public async Task<MeliAuthUrlDto?> GetMeliAuthUrlAsync()
        => await GetAsync<MeliAuthUrlDto>("/api/meli/auth-url");

    public async Task<MeliAccountDto?> MeliCallbackAsync(string code)
        => await PostAsync<MeliAccountDto>("/api/meli/callback", new { code });

    public async Task<bool> DeleteMeliAccountAsync(int id)
        => await DeleteAsync($"/api/meli/accounts/{id}");

    public async Task<bool> SyncMeliItemsAsync()
    {
        try { await PostAsync<object>("/api/meli/items/sync", new { }); return true; }
        catch { return false; }
    }

    public async Task<bool> SyncMeliOrdersAsync()
    {
        try { await PostAsync<object>("/api/meli/orders/sync", new { }); return true; }
        catch { return false; }
    }

    // --- WhatsApp ---
    public async Task<WhatsAppStatusDto?> GetWhatsAppStatusAsync()
        => await GetAsync<WhatsAppStatusDto>("/api/whatsapp/status");

    public async Task<bool> StartWhatsAppLinkAsync()
    {
        try { await PostAsync<object>("/api/whatsapp/link", new { }); return true; }
        catch { return false; }
    }

    public async Task<WhatsAppCheckDto?> CheckWhatsAppLinkedAsync()
        => await GetAsync<WhatsAppCheckDto>("/api/whatsapp/check-linked");

    public async Task<bool> UnlinkWhatsAppAsync()
    {
        try { await PostAsync<object>("/api/whatsapp/unlink", new { }); return true; }
        catch { return false; }
    }

    public async Task<bool> CancelWhatsAppLinkAsync()
    {
        try { await PostAsync<object>("/api/whatsapp/cancel-link", new { }); return true; }
        catch { return false; }
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

    // --- Changelog ---
    public async Task<ChangelogListResponse?> GetChangelogAsync(DateTime? from = null, DateTime? to = null, string? search = null, List<string>? tags = null, int page = 1)
    {
        var url = $"/api/changelog?page={page}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue) url += $"&to={to.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (tags is not null && tags.Count > 0) url += $"&tags={Uri.EscapeDataString(string.Join(",", tags))}";
        return await GetAsync<ChangelogListResponse>(url);
    }

    public async Task<DailyChangeSummaryDetailDto?> GetChangelogDetailAsync(DateTime date)
    {
        return await GetAsync<DailyChangeSummaryDetailDto>($"/api/changelog/{date:yyyy-MM-dd}");
    }

    public async Task<bool> UpdateCommitGroupAuthorAsync(int groupId, string? author)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"/api/changelog/groups/{groupId}/author", new { Author = author });
        return response.IsSuccessStatusCode;
    }
}
