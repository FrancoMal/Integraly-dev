namespace Web.Models;

public class WebinarDateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public string? MeetingLink { get; set; }
    public string? InviteSubject { get; set; }
    public string? InviteMessage { get; set; }
    public bool SendByEmail { get; set; }
    public bool SendByWhatsapp { get; set; }
    public int RegistrationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWebinarDateRequest
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public string? MeetingLink { get; set; }
    public string? InviteSubject { get; set; }
    public string? InviteMessage { get; set; }
    public bool SendByEmail { get; set; }
    public bool SendByWhatsapp { get; set; }
}

public class WebinarContactDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Tag { get; set; }
    public string UUID { get; set; } = "";
    public int? WebinarDateId { get; set; }
    public string? WebinarDateDisplay { get; set; }
    public bool HasRegistration { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWebinarContactRequest
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Tag { get; set; }
}

public class UpdateWebinarContactRequest
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Tag { get; set; }
}

public class AssignWebinarRequest
{
    public List<int> ContactIds { get; set; } = new();
    public int WebinarDateId { get; set; }
}

public class WebinarRegistrationDto
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class ImportContactsResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public string Message { get; set; } = "";
}

public class AssignWebinarResult
{
    public int Assigned { get; set; }
    public int Skipped { get; set; }
    public string Message { get; set; } = "";
}

public class WebinarFormDataDto
{
    public string FullName { get; set; } = "";
    public List<WebinarDateOptionDto> AvailableDates { get; set; } = new();
    public bool AlreadyRegistered { get; set; }
}

public class WebinarDateOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
}

public class WebinarFormSubmitRequest
{
    public int WebinarDateId { get; set; }
    public bool KnowsChatGPT { get; set; }
    public bool KnowsClaude { get; set; }
    public bool KnowsGrok { get; set; }
    public bool KnowsGemini { get; set; }
    public bool KnowsCopilot { get; set; }
    public bool KnowsPerplexity { get; set; }
    public bool KnowsDeepSeek { get; set; }
    public string VibeCodingKnowledge { get; set; } = "no_idea";
}

public class WebinarStatsDto
{
    public int TotalContacts { get; set; }
    public int TotalRegistrations { get; set; }
    public List<WebinarDateStatsDto> PerDate { get; set; } = new();
}

public class WebinarDateStatsDto
{
    public int DateId { get; set; }
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public int Registrations { get; set; }
    public int KnowsChatGPT { get; set; }
    public int KnowsClaude { get; set; }
    public int KnowsGrok { get; set; }
    public int KnowsGemini { get; set; }
    public int KnowsCopilot { get; set; }
    public int KnowsPerplexity { get; set; }
    public int KnowsDeepSeek { get; set; }
    public Dictionary<string, int> VibeCodingBreakdown { get; set; } = new();
}
