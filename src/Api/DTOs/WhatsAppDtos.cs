namespace Api.DTOs;

public record WhatsAppStatusDto(bool Linked, string? Info, bool IsLinking);

public record SendWhatsAppRequest(string Phone, string Message);

public record SendBulkWhatsAppRequest(List<BulkRecipient> Recipients, string Message);

public record BulkRecipient(string Phone, string Name);

public record BulkSendResult(string Phone, string Name, bool Success, string Message);
