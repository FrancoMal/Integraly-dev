using System.Text.Json;
using Api.Data;
using Api.Models;
using Api.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Api.BackgroundJobs;

public class SyncArcaComprobantesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncArcaComprobantesJob> _logger;

    public SyncArcaComprobantesJob(IServiceProvider serviceProvider, ILogger<SyncArcaComprobantesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var integrationService = scope.ServiceProvider.GetRequiredService<IntegrationService>();
                var integration = await integrationService.GetRawByProviderAsync("arca-web");

                if (integration is not null && integration.IsActive)
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                    var cuit = integration.AppId?.Replace("-", "") ?? "";
                    var clave = integration.AppSecret ?? "";

                    var client = httpFactory.CreateClient();
                    var playwrightUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_URL") ?? "http://playwright:3001";
                    client.Timeout = TimeSpan.FromMinutes(10);

                    var response = await client.PostAsJsonAsync($"{playwrightUrl}/arca/download", new
                    {
                        cuit,
                        clave,
                        cuitEmpresa = cuit
                    }, stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadFromJsonAsync<JsonElement>(stoppingToken);

                        // Process emitidos
                        if (json.TryGetProperty("emitidos", out var emitidos))
                        {
                            var bytes = Convert.FromBase64String(emitidos.GetString()!);
                            await ImportExcel(db, bytes, "Venta", stoppingToken);
                        }

                        // Process recibidos
                        if (json.TryGetProperty("recibidos", out var recibidos))
                        {
                            var bytes = Convert.FromBase64String(recibidos.GetString()!);
                            await ImportExcel(db, bytes, "Compra", stoppingToken);
                        }

                        _logger.LogInformation("SyncArcaComprobantesJob: sync completed");
                    }
                    else
                    {
                        _logger.LogWarning("SyncArcaComprobantesJob: Playwright returned {StatusCode}", response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncArcaComprobantesJob");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private static async Task ImportExcel(AppDbContext db, byte[] bytes, string categoria, CancellationToken ct)
    {
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        var headerRow = 0;
        for (int row = 1; row <= (sheet.LastRowUsed()?.RowNumber() ?? 0); row++)
        {
            if (sheet.Cell(row, 1).GetString().Trim().StartsWith("Fecha", StringComparison.OrdinalIgnoreCase))
            {
                headerRow = row;
                break;
            }
        }

        if (headerRow == 0) return;

        for (int row = headerRow + 1; row <= (sheet.LastRowUsed()?.RowNumber() ?? 0); row++)
        {
            var fechaStr = sheet.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(fechaStr) || !DateTime.TryParse(fechaStr, out var fecha)) continue;

            var tipo = sheet.Cell(row, 2).GetString().Trim();
            var puntoDeVenta = ParseInt(sheet.Cell(row, 3).GetString());
            var numeroDesde = ParseLong(sheet.Cell(row, 4).GetString());

            var exists = await db.Comprobantes.AnyAsync(c =>
                c.Categoria == categoria && c.PuntoDeVenta == puntoDeVenta &&
                c.NumeroDesde == numeroDesde && c.Tipo == tipo && c.Fecha.Date == fecha.Date, ct);

            if (exists) continue;

            db.Comprobantes.Add(new Comprobante
            {
                Categoria = categoria,
                Fecha = fecha,
                Tipo = tipo,
                PuntoDeVenta = puntoDeVenta,
                NumeroDesde = numeroDesde,
                NumeroHasta = ParseLong(sheet.Cell(row, 5).GetString())
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static int ParseInt(string? s) => int.TryParse(s?.Trim().Replace(".", ""), out var v) ? v : 0;
    private static long ParseLong(string? s) => long.TryParse(s?.Trim().Replace(".", ""), out var v) ? v : 0;
}
