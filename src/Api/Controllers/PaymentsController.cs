using System.Security.Claims;
using System.Text.Json;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MercadoPagoService _mercadoPagoService;
    private readonly PayPalService _payPalService;
    private readonly EmailService _emailService;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext db,
        MercadoPagoService mercadoPagoService,
        PayPalService payPalService,
        EmailService emailService,
        AuditLogService auditLogService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _db = db;
        _mercadoPagoService = mercadoPagoService;
        _payPalService = payPalService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get active payment plans (public for authenticated users)
    /// </summary>
    [HttpGet("plans")]
    [Authorize]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _db.PaymentPlans
            .Where(p => p.Active)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Classes,
                p.Price,
                p.PriceUSD,
                p.Currency,
                isActive = p.Active,
                isPopular = p.Classes == 5
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Detect country by client IP (returns "AR" for Argentina, etc.)
    /// </summary>
    [HttpGet("country")]
    [AllowAnonymous]
    public async Task<IActionResult> DetectCountry([FromQuery] string? u = null)
    {
        // URL param override: ?u=MX
        if (!string.IsNullOrEmpty(u))
        {
            return Ok(new { country = u.ToUpper(), provider = u.ToUpper() == "AR" ? "mercadopago" : "paypal" });
        }

        // Try to detect by IP
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        // Check forwarded headers (behind nginx/proxy)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        if (string.IsNullOrEmpty(ip) || ip == "::1" || ip == "127.0.0.1")
        {
            // Localhost = default Argentina
            return Ok(new { country = "AR", provider = "mercadopago" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://ip-api.com/json/{ip}?fields=countryCode");
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var countryCode = doc.RootElement.TryGetProperty("countryCode", out var cc)
                    ? cc.GetString() ?? "AR" : "AR";
                var provider = countryCode == "AR" ? "mercadopago" : "paypal";
                return Ok(new { country = countryCode, provider });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not detect country by IP {Ip}", ip);
        }

        // Default to Argentina
        return Ok(new { country = "AR", provider = "mercadopago" });
    }

    /// <summary>
    /// Get all plans (admin - includes inactive)
    /// </summary>
    [HttpGet("plans/all")]
    [Authorize]
    public async Task<IActionResult> GetAllPlans()
    {
        if (!IsAdmin()) return Forbid();

        var plans = await _db.PaymentPlans
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Classes,
                p.Price,
                p.PriceUSD,
                p.Currency,
                p.Active,
                p.DisplayOrder
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Create a payment plan (admin only)
    /// </summary>
    [HttpPost("plans")]
    [Authorize]
    public async Task<IActionResult> CreatePlan([FromBody] PlanRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var plan = new PaymentPlan
        {
            Name = request.Name,
            Description = request.Description,
            Classes = request.Classes,
            Price = request.Price,
            PriceUSD = request.PriceUSD,
            Currency = request.Currency ?? "ARS",
            Active = request.Active,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.PaymentPlans.Add(plan);
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("PaymentPlan", plan.Id.ToString(), "create",
            $"{plan.Name}: ${plan.Price} ARS / ${plan.PriceUSD} USD", GetUsername());

        return Ok(plan);
    }

    /// <summary>
    /// Update a payment plan (admin only)
    /// </summary>
    [HttpPut("plans/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] PlanRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var plan = await _db.PaymentPlans.FindAsync(id);
        if (plan is null) return NotFound(new { message = "Plan no encontrado" });

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Classes = request.Classes;
        plan.Price = request.Price;
        plan.PriceUSD = request.PriceUSD;
        plan.Currency = request.Currency ?? plan.Currency;
        plan.Active = request.Active;
        plan.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("PaymentPlan", plan.Id.ToString(), "update",
            $"{plan.Name}: ${plan.Price} ARS / ${plan.PriceUSD} USD", GetUsername());

        return Ok(plan);
    }

    /// <summary>
    /// Delete a payment plan (admin only)
    /// </summary>
    [HttpDelete("plans/{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePlan(int id)
    {
        if (!IsAdmin()) return Forbid();

        var plan = await _db.PaymentPlans.FindAsync(id);
        if (plan is null) return NotFound(new { message = "Plan no encontrado" });

        // Check if any payments reference this plan
        var hasPayments = await _db.Payments.AnyAsync(p => p.PaymentPlanId == id);
        if (hasPayments)
        {
            // Soft delete - just deactivate
            plan.Active = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Plan desactivado (tiene pagos asociados)" });
        }

        _db.PaymentPlans.Remove(plan);
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("PaymentPlan", id.ToString(), "delete",
            $"{plan.Name}", GetUsername());

        return Ok(new { message = "Plan eliminado" });
    }

    /// <summary>
    /// Create a payment and get checkout URL (MercadoPago or PayPal)
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var provider = (request.Provider ?? "mercadopago").ToLower();
        if (provider != "mercadopago" && provider != "paypal" && provider != "transferencia")
            return BadRequest(new { message = "Proveedor de pago no soportado." });

        var plan = await _db.PaymentPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.Active);
        if (plan is null) return BadRequest(new { message = "Plan no encontrado o inactivo" });

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        // USD for PayPal, ARS + IVA 21% for MercadoPago, ARS + IVA 21% - 5% discount for transferencia
        decimal amount;
        string currency;
        if (provider == "paypal")
        {
            amount = plan.PriceUSD;
            currency = "USD";
        }
        else if (provider == "transferencia")
        {
            amount = Math.Round(plan.Price * 1.21m * 0.95m, 2);
            currency = plan.Currency;
        }
        else
        {
            amount = Math.Round(plan.Price * 1.21m, 2);
            currency = plan.Currency;
        }

        // Create payment record
        var payment = new Payment
        {
            UserId = userId.Value,
            PaymentPlanId = plan.Id,
            Amount = amount,
            Currency = currency,
            Status = "pending",
            PaymentProvider = provider,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        if (provider == "transferencia")
        {
            // Transferencia: queda pendiente, el usuario debe subir comprobante
            await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "create",
                $"Provider: transferencia, Plan: {plan.Name} - ${amount} ARS (con 5% dto)", GetUsername());

            return Ok(new
            {
                checkoutUrl = (string?)null,
                paymentId = payment.Id,
                transfer = true,
                amount = payment.Amount,
                cvu = _configuration["Transfer:CVU"] ?? Environment.GetEnvironmentVariable("TRANSFER_CVU") ?? "",
                alias = _configuration["Transfer:Alias"] ?? Environment.GetEnvironmentVariable("TRANSFER_ALIAS") ?? "",
                titular = _configuration["Transfer:Titular"] ?? Environment.GetEnvironmentVariable("TRANSFER_TITULAR") ?? ""
            });
        }

        string? checkoutUrl = null;

        if (provider == "mercadopago")
        {
            checkoutUrl = await _mercadoPagoService.CreatePreference(payment, plan, user.Email);
        }
        else if (provider == "paypal")
        {
            var (approvalUrl, orderId) = await _payPalService.CreateOrder(payment, plan, user.Email);
            if (orderId is not null)
            {
                payment.PayPalOrderId = orderId;
                await _db.SaveChangesAsync();
            }
            checkoutUrl = approvalUrl;
        }

        if (checkoutUrl is null)
        {
            payment.Status = "error";
            await _db.SaveChangesAsync();
            return StatusCode(500, new { message = "Error al crear el link de pago" });
        }

        await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "create",
            $"Provider: {provider}, Plan: {plan.Name} - ${plan.Price}", GetUsername());

        return Ok(new { checkoutUrl, paymentId = payment.Id });
    }

    /// <summary>
    /// PayPal capture - called when user returns from PayPal after approval
    /// </summary>
    [HttpGet("paypal/capture")]
    [AllowAnonymous]
    public async Task<IActionResult> PayPalCapture([FromQuery] int paymentId, [FromQuery] string token)
    {
        _logger.LogInformation("PayPal capture callback: paymentId={PaymentId}, token={Token}", paymentId, token);

        var payment = await _db.Payments
            .Include(p => p.PaymentPlan)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.PaymentProvider == "paypal");

        if (payment is null)
        {
            _logger.LogWarning("PayPal capture: payment {PaymentId} not found", paymentId);
            return Redirect("/panel/pago-fallido");
        }

        // The token from PayPal query param is the order ID
        var orderId = payment.PayPalOrderId ?? token;

        var (status, captureId) = await _payPalService.CaptureOrder(orderId);

        if (status == "COMPLETED" && payment.Status != "approved")
        {
            payment.Status = "approved";
            payment.PayPalCaptureId = captureId;
            payment.ApprovedAt = DateTime.UtcNow;

            var plan = payment.PaymentPlan;
            if (plan is not null)
            {
                var tokenPack = new TokenPack
                {
                    UserId = payment.UserId,
                    TotalTokens = plan.Classes,
                    RemainingTokens = plan.Classes,
                    CreatedBy = payment.UserId,
                    Description = $"Compra: {plan.Name} (Pago #{payment.Id})",
                    CreatedAt = DateTime.UtcNow
                };

                _db.TokenPacks.Add(tokenPack);
                await _db.SaveChangesAsync();

                payment.TokenPackId = tokenPack.Id;
            }

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "approved",
                $"PayPal Order: {orderId}, Capture: {captureId}, Plan: {plan?.Name}", "paypal-capture");

            if (payment.User is not null && plan is not null)
            {
                await SendPaymentConfirmationEmail(payment.User, plan, payment);
            }

            _logger.LogInformation("PayPal payment {PaymentId} approved. TokenPack created for user {UserId}",
                payment.Id, payment.UserId);

            return Redirect("/panel/pago-exitoso");
        }
        else if (status == "COMPLETED" && payment.Status == "approved")
        {
            // Already processed, just redirect
            return Redirect("/panel/pago-exitoso");
        }
        else
        {
            payment.Status = "rejected";
            await _db.SaveChangesAsync();
            _logger.LogWarning("PayPal capture failed for payment {PaymentId}: status={Status}", paymentId, status);
            return Redirect("/panel/pago-fallido");
        }
    }

    /// <summary>
    /// PayPal webhook (optional - capture already handles the main flow)
    /// </summary>
    [HttpPost("webhook/paypal")]
    [AllowAnonymous]
    public async Task<IActionResult> PayPalWebhook()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        _logger.LogInformation("PayPal webhook received: {Body}", body);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var eventType = doc.RootElement.TryGetProperty("event_type", out var et) ? et.GetString() : null;

            if (eventType == "CHECKOUT.ORDER.APPROVED" || eventType == "PAYMENT.CAPTURE.COMPLETED")
            {
                var resource = doc.RootElement.GetProperty("resource");
                var orderId = resource.TryGetProperty("id", out var oid) ? oid.GetString() : null;

                if (eventType == "PAYMENT.CAPTURE.COMPLETED")
                {
                    // For capture events, the supplementary_data has the order ID
                    orderId = resource.TryGetProperty("supplementary_data", out var sd)
                        && sd.TryGetProperty("related_ids", out var ri)
                        && ri.TryGetProperty("order_id", out var orderIdProp)
                        ? orderIdProp.GetString() : orderId;
                }

                if (orderId is not null)
                {
                    var payment = await _db.Payments
                        .Include(p => p.PaymentPlan)
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.PayPalOrderId == orderId);

                    if (payment is not null && payment.Status != "approved")
                    {
                        // Capture if not already captured
                        var (status, captureId) = await _payPalService.CaptureOrder(orderId);
                        if (status == "COMPLETED")
                        {
                            payment.Status = "approved";
                            payment.PayPalCaptureId = captureId;
                            payment.ApprovedAt = DateTime.UtcNow;

                            var plan = payment.PaymentPlan;
                            if (plan is not null)
                            {
                                var tokenPack = new TokenPack
                                {
                                    UserId = payment.UserId,
                                    TotalTokens = plan.Classes,
                                    RemainingTokens = plan.Classes,
                                    CreatedBy = payment.UserId,
                                    Description = $"Compra: {plan.Name} (Pago #{payment.Id})",
                                    CreatedAt = DateTime.UtcNow
                                };

                                _db.TokenPacks.Add(tokenPack);
                                await _db.SaveChangesAsync();
                                payment.TokenPackId = tokenPack.Id;
                            }

                            await _db.SaveChangesAsync();

                            await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "approved",
                                $"PayPal Webhook: {orderId}, Plan: {plan?.Name}", "paypal-webhook");

                            if (payment.User is not null && plan is not null)
                            {
                                await SendPaymentConfirmationEmail(payment.User, plan, payment);
                            }
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return Ok();
        }
    }

    /// <summary>
    /// MercadoPago webhook - NO authentication required
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        _logger.LogInformation("MercadoPago webhook received: {Body}", body);

        try
        {
            // Parse query params (MercadoPago sends topic and id as query params too)
            var topic = Request.Query["topic"].FirstOrDefault()
                ?? Request.Query["type"].FirstOrDefault();
            var resourceId = Request.Query["id"].FirstOrDefault();

            // Also try to parse from body
            if (!string.IsNullOrEmpty(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("type", out var typeProp))
                    topic ??= typeProp.GetString();
                if (doc.RootElement.TryGetProperty("data", out var dataProp)
                    && dataProp.TryGetProperty("id", out var idProp))
                    resourceId ??= idProp.ToString();
            }

            _logger.LogInformation("Webhook topic={Topic}, resourceId={ResourceId}", topic, resourceId);

            if (topic == "payment" && !string.IsNullOrEmpty(resourceId))
            {
                await ProcessPaymentNotification(resourceId);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Ok(); // Always return 200 to MercadoPago to avoid retries
        }
    }

    /// <summary>
    /// Get transfer bank details
    /// </summary>
    [HttpGet("transfer-info")]
    [Authorize]
    public IActionResult GetTransferInfo()
    {
        return Ok(new
        {
            cvu = _configuration["Transfer:CVU"] ?? Environment.GetEnvironmentVariable("TRANSFER_CVU") ?? "",
            alias = _configuration["Transfer:Alias"] ?? Environment.GetEnvironmentVariable("TRANSFER_ALIAS") ?? "",
            titular = _configuration["Transfer:Titular"] ?? Environment.GetEnvironmentVariable("TRANSFER_TITULAR") ?? ""
        });
    }

    /// <summary>
    /// Upload transfer receipt for a pending transfer payment
    /// </summary>
    [HttpPost("{id}/receipt")]
    [Authorize]
    public async Task<IActionResult> UploadReceipt(int id, IFormFile file)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var payment = await _db.Payments.FirstOrDefaultAsync(
            p => p.Id == id && p.UserId == userId.Value && p.PaymentProvider == "transferencia");
        if (payment is null) return NotFound(new { message = "Pago no encontrado" });

        if (payment.Status == "approved")
            return BadRequest(new { message = "Este pago ya fue aprobado" });

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Debe adjuntar un archivo" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 5 MB" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "receipts");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"receipt_{payment.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        payment.TransferReceiptUrl = $"/api/payments/{payment.Id}/receipt-file/{fileName}";
        payment.Status = "pending_review";
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "receipt_uploaded",
            $"File: {fileName}", GetUsername());

        return Ok(new { message = "Comprobante subido correctamente", receiptUrl = payment.TransferReceiptUrl });
    }

    /// <summary>
    /// Serve a receipt file
    /// </summary>
    [HttpGet("{id}/receipt-file/{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetReceiptFile(int id, string fileName)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();

        var payment = await _db.Payments.FindAsync(id);
        if (payment is null) return NotFound();

        // Only the owner or admin can see the receipt
        if (!isAdmin && payment.UserId != userId) return Forbid();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "receipts", fileName);
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var contentType = Path.GetExtension(fileName).ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType);
    }

    /// <summary>
    /// Get current user's payment history
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyPayments()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var payments = await _db.Payments
            .Include(p => p.PaymentPlan)
            .Where(p => p.UserId == userId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                planName = p.PaymentPlan != null ? p.PaymentPlan.Name : "",
                classes = p.PaymentPlan != null ? p.PaymentPlan.Classes : 0,
                planId = p.PaymentPlanId,
                p.Amount,
                p.Currency,
                p.Status,
                p.PaymentProvider,
                p.TransferReceiptUrl,
                p.CreatedAt,
                p.ApprovedAt
            })
            .ToListAsync();

        return Ok(payments);
    }

    /// <summary>
    /// Approve a transfer payment (admin only) - creates token pack
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApprovePayment(int id)
    {
        if (!IsAdmin()) return Forbid();

        var payment = await _db.Payments
            .Include(p => p.PaymentPlan)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null) return NotFound(new { message = "Pago no encontrado" });

        if (payment.Status == "approved")
            return BadRequest(new { message = "Este pago ya fue aprobado" });

        payment.Status = "approved";
        payment.ApprovedAt = DateTime.UtcNow;

        var plan = payment.PaymentPlan;
        if (plan is not null)
        {
            var tokenPack = new TokenPack
            {
                UserId = payment.UserId,
                TotalTokens = plan.Classes,
                RemainingTokens = plan.Classes,
                CreatedBy = GetUserId() ?? payment.UserId,
                Description = $"Compra: {plan.Name} (Pago #{payment.Id})",
                CreatedAt = DateTime.UtcNow
            };

            _db.TokenPacks.Add(tokenPack);
            await _db.SaveChangesAsync();
            payment.TokenPackId = tokenPack.Id;
        }

        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "approved",
            $"Admin approved transfer payment. Plan: {plan?.Name}", GetUsername());

        if (payment.User is not null && plan is not null)
        {
            await SendPaymentConfirmationEmail(payment.User, plan, payment);
        }

        return Ok(new { message = "Pago aprobado y clases acreditadas" });
    }

    /// <summary>
    /// Delete a pending payment (user can only delete their own pending payments)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId.Value);
        if (payment is null) return NotFound(new { message = "Pago no encontrado" });

        if (payment.Status == "approved")
            return BadRequest(new { message = "No se puede eliminar un pago aprobado" });

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync("Payment", id.ToString(), "delete",
            $"User deleted pending payment", GetUsername());

        return Ok(new { message = "Pago eliminado" });
    }

    /// <summary>
    /// Get all payments (admin only)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var payments = await _db.Payments
            .Include(p => p.User)
            .Include(p => p.PaymentPlan)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                userId = p.UserId,
                userName = p.User != null ? p.User.Username : "",
                userEmail = p.User != null ? p.User.Email : "",
                planName = p.PaymentPlan != null ? p.PaymentPlan.Name : "",
                classes = p.PaymentPlan != null ? p.PaymentPlan.Classes : 0,
                p.Amount,
                p.Currency,
                p.Status,
                p.PaymentProvider,
                p.MercadoPagoPaymentId,
                p.PayPalOrderId,
                p.TokenPackId,
                p.CreatedAt,
                p.ApprovedAt
            })
            .ToListAsync();

        return Ok(payments);
    }

    private async Task ProcessPaymentNotification(string mercadoPagoPaymentId)
    {
        // Fetch payment info from MercadoPago
        using var paymentInfo = await _mercadoPagoService.GetPaymentInfo(mercadoPagoPaymentId);
        if (paymentInfo is null)
        {
            _logger.LogWarning("Could not fetch payment info for MP payment {MpPaymentId}", mercadoPagoPaymentId);
            return;
        }

        var root = paymentInfo.RootElement;
        var status = root.GetProperty("status").GetString();
        var externalReference = root.TryGetProperty("external_reference", out var extRef)
            ? extRef.GetString() : null;

        _logger.LogInformation("MP Payment {MpPaymentId}: status={Status}, external_reference={ExtRef}",
            mercadoPagoPaymentId, status, externalReference);

        if (string.IsNullOrEmpty(externalReference) || !int.TryParse(externalReference, out var paymentId))
        {
            _logger.LogWarning("Invalid external_reference: {ExtRef}", externalReference);
            return;
        }

        var payment = await _db.Payments
            .Include(p => p.PaymentPlan)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found in database", paymentId);
            return;
        }

        // Store MercadoPago payment ID
        payment.MercadoPagoPaymentId = mercadoPagoPaymentId;

        if (status == "approved" && payment.Status != "approved")
        {
            payment.Status = "approved";
            payment.ApprovedAt = DateTime.UtcNow;

            // Create TokenPack for the user
            var plan = payment.PaymentPlan;
            if (plan is not null)
            {
                var tokenPack = new TokenPack
                {
                    UserId = payment.UserId,
                    TotalTokens = plan.Classes,
                    RemainingTokens = plan.Classes,
                    CreatedBy = payment.UserId, // self-purchased
                    Description = $"Compra: {plan.Name} (Pago #{payment.Id})",
                    CreatedAt = DateTime.UtcNow
                };

                _db.TokenPacks.Add(tokenPack);
                await _db.SaveChangesAsync();

                payment.TokenPackId = tokenPack.Id;
            }

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync("Payment", payment.Id.ToString(), "approved",
                $"MP Payment: {mercadoPagoPaymentId}, Plan: {plan?.Name}", "webhook");

            // Send confirmation email
            if (payment.User is not null && plan is not null)
            {
                await SendPaymentConfirmationEmail(payment.User, plan, payment);
            }

            _logger.LogInformation("Payment {PaymentId} approved. TokenPack created for user {UserId}",
                payment.Id, payment.UserId);
        }
        else if (status == "rejected" || status == "cancelled")
        {
            payment.Status = status;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} status updated to {Status}",
                payment.Id, status);
        }
        else
        {
            payment.Status = status ?? payment.Status;
            await _db.SaveChangesAsync();
        }
    }

    private async Task SendPaymentConfirmationEmail(User user, PaymentPlan plan, Payment payment)
    {
        var providerName = payment.PaymentProvider == "paypal" ? "PayPal" : "MercadoPago";
        var currencySymbol = payment.Currency == "USD" ? "US$" : "$";
        var amountFormatted = payment.Currency == "USD"
            ? $"{currencySymbol} {payment.Amount:N2}"
            : $"{currencySymbol} {payment.Amount:N0}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #10b981, #059669); padding: 30px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; }}
        .header .check {{ font-size: 48px; margin-bottom: 10px; }}
        .body {{ padding: 30px; }}
        .body h2 {{ color: #1a1a2e; margin-top: 0; }}
        .body p {{ color: #555; line-height: 1.6; }}
        .detail-box {{ background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .steps {{ background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .steps h3 {{ color: #0369a1; margin-top: 0; font-size: 16px; }}
        .steps ol {{ color: #555; padding-left: 20px; margin: 0; }}
        .steps li {{ padding: 6px 0; line-height: 1.5; }}
        .steps a {{ color: #0369a1; text-decoration: none; font-weight: 600; }}
        .footer {{ background: #f9fafb; padding: 20px; text-align: center; color: #999; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""check"">&#10003;</div>
            <h1>Pago confirmado</h1>
        </div>
        <div class=""body"">
            <h2>Hola {user.FirstName ?? user.Username},</h2>
            <p>Tu pago fue procesado exitosamente. Ya tenes tus clases disponibles para reservar.</p>

            <div class=""detail-box"">
                <table width=""100%"" cellpadding=""8"" cellspacing=""0"">
                    <tr>
                        <td style=""color:#666;font-weight:500;"">Plan</td>
                        <td style=""color:#1a1a2e;font-weight:600;text-align:right;"">{plan.Name}</td>
                    </tr>
                    <tr>
                        <td style=""color:#666;font-weight:500;"">Clases disponibles</td>
                        <td style=""color:#1a1a2e;font-weight:600;text-align:right;"">{plan.Classes}</td>
                    </tr>
                    <tr>
                        <td style=""color:#666;font-weight:500;"">Monto abonado</td>
                        <td style=""color:#1a1a2e;font-weight:600;text-align:right;"">{amountFormatted} {payment.Currency}</td>
                    </tr>
                    <tr>
                        <td style=""color:#666;font-weight:500;"">Medio de pago</td>
                        <td style=""color:#1a1a2e;font-weight:600;text-align:right;"">{providerName}</td>
                    </tr>
                    <tr>
                        <td style=""color:#666;font-weight:500;"">Fecha</td>
                        <td style=""color:#1a1a2e;font-weight:600;text-align:right;"">{payment.ApprovedAt:dd/MM/yyyy HH:mm}</td>
                    </tr>
                </table>
            </div>

            <div class=""steps"">
                <h3>¿Que podes hacer con tus clases?</h3>
                <ol>
                    <li>Ingresa a tu panel en <a href=""https://integraly.dev/panel"">integraly.dev/panel</a></li>
                    <li>Anda a la seccion <strong>""Reservar""</strong> en el menu lateral</li>
                    <li>Elegi el instructor, dia y horario que prefieras</li>
                    <li>Confirma tu reserva y listo, vas a recibir un link de Google Meet para conectarte</li>
                </ol>
            </div>

            <p>Tus clases no tienen vencimiento, asi que podes usarlas cuando quieras.</p>
            <p>Si tenes alguna duda, respondenos a este mail y te ayudamos.</p>
        </div>
        <div class=""footer"">
            <p>Integraly - Clases particulares de tecnologia</p>
        </div>
    </div>
</body>
</html>";

        await _emailService.SendEmailAsync(user.Email, "Tu pago fue confirmado - Integraly", htmlBody, bcc: "ventas@integraly.com");
    }

    private bool IsAdmin()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value == "admin";
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }

    private string GetUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value ?? "unknown";
    }
}

public class CreatePaymentRequest
{
    public int PlanId { get; set; }
    public string? Provider { get; set; }
}

public class PlanRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Classes { get; set; }
    public decimal Price { get; set; }
    public decimal PriceUSD { get; set; }
    public string? Currency { get; set; }
    public bool Active { get; set; } = true;
    public int DisplayOrder { get; set; }
}
