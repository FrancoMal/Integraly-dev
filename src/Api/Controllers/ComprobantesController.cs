using Api.Data;
using Api.DTOs;
using Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprobantesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ComprobantesController> _logger;

    public ComprobantesController(AppDbContext db, ILogger<ComprobantesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoria)
    {
        var query = _db.Comprobantes.AsQueryable();
        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(c => c.Categoria == categoria);

        var items = await query.OrderByDescending(c => c.Fecha).ToListAsync();
        return Ok(items.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Comprobantes.FindAsync(id);
        if (item is null) return NotFound(new { message = "Comprobante no encontrado" });
        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveComprobanteRequest req)
    {
        var entity = new Comprobante
        {
            Categoria = req.Categoria, Fecha = req.Fecha, Tipo = req.Tipo,
            PuntoDeVenta = req.PuntoDeVenta, NumeroDesde = req.NumeroDesde,
            NumeroHasta = req.NumeroHasta, CodAutorizacion = req.CodAutorizacion,
            ContraparteTipoDoc = req.ContraparteTipoDoc, ContraparteNroDoc = req.ContraparteNroDoc,
            ContraparteDenominacion = req.ContraparteDenominacion, TipoCambio = req.TipoCambio,
            Moneda = req.Moneda, NetoGravIva0 = req.NetoGravIva0,
            Iva25 = req.Iva25, NetoGravIva25 = req.NetoGravIva25,
            Iva5 = req.Iva5, NetoGravIva5 = req.NetoGravIva5,
            Iva105 = req.Iva105, NetoGravIva105 = req.NetoGravIva105,
            Iva21 = req.Iva21, NetoGravIva21 = req.NetoGravIva21,
            Iva27 = req.Iva27, NetoGravIva27 = req.NetoGravIva27,
            PercIva = req.PercIva, PercOtrosImp = req.PercOtrosImp,
            PercIIBB = req.PercIIBB, PercImpMuni = req.PercImpMuni,
            ImpInterno = req.ImpInterno, NoGravado = req.NoGravado,
            OtrosTributos = req.OtrosTributos, ImporteTotal = req.ImporteTotal
        };
        _db.Comprobantes.Add(entity);
        await _db.SaveChangesAsync();
        return Created($"/api/comprobantes/{entity.Id}", ToDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveComprobanteRequest req)
    {
        var entity = await _db.Comprobantes.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Comprobante no encontrado" });

        entity.Categoria = req.Categoria; entity.Fecha = req.Fecha; entity.Tipo = req.Tipo;
        entity.PuntoDeVenta = req.PuntoDeVenta; entity.NumeroDesde = req.NumeroDesde;
        entity.NumeroHasta = req.NumeroHasta; entity.CodAutorizacion = req.CodAutorizacion;
        entity.ContraparteTipoDoc = req.ContraparteTipoDoc; entity.ContraparteNroDoc = req.ContraparteNroDoc;
        entity.ContraparteDenominacion = req.ContraparteDenominacion; entity.TipoCambio = req.TipoCambio;
        entity.Moneda = req.Moneda; entity.NetoGravIva0 = req.NetoGravIva0;
        entity.Iva25 = req.Iva25; entity.NetoGravIva25 = req.NetoGravIva25;
        entity.Iva5 = req.Iva5; entity.NetoGravIva5 = req.NetoGravIva5;
        entity.Iva105 = req.Iva105; entity.NetoGravIva105 = req.NetoGravIva105;
        entity.Iva21 = req.Iva21; entity.NetoGravIva21 = req.NetoGravIva21;
        entity.Iva27 = req.Iva27; entity.NetoGravIva27 = req.NetoGravIva27;
        entity.PercIva = req.PercIva; entity.PercOtrosImp = req.PercOtrosImp;
        entity.PercIIBB = req.PercIIBB; entity.PercImpMuni = req.PercImpMuni;
        entity.ImpInterno = req.ImpInterno; entity.NoGravado = req.NoGravado;
        entity.OtrosTributos = req.OtrosTributos; entity.ImporteTotal = req.ImporteTotal;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Comprobantes.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Comprobante no encontrado" });
        _db.Comprobantes.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Comprobante eliminado" });
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var items = await _db.Comprobantes.Where(c => request.Ids.Contains(c.Id)).ToListAsync();
        _db.Comprobantes.RemoveRange(items);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Se eliminaron {items.Count} comprobantes" });
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string categoria, IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();

            // Find header row
            var headerRow = 0;
            for (int row = 1; row <= sheet.LastRowUsed()?.RowNumber(); row++)
            {
                if (sheet.Cell(row, 1).GetString().Trim().StartsWith("Fecha", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = row;
                    break;
                }
            }

            if (headerRow == 0) return BadRequest(new { message = "No se encontro la fila de encabezado" });

            var imported = 0;
            var skipped = 0;

            for (int row = headerRow + 1; row <= sheet.LastRowUsed()?.RowNumber(); row++)
            {
                var fechaStr = sheet.Cell(row, 1).GetString().Trim();
                if (string.IsNullOrEmpty(fechaStr)) continue;

                if (!DateTime.TryParse(fechaStr, out var fecha)) continue;

                var tipo = sheet.Cell(row, 2).GetString().Trim();
                var puntoDeVenta = ParseInt(sheet.Cell(row, 3).GetString());
                var numeroDesde = ParseLong(sheet.Cell(row, 4).GetString());

                // Check duplicate
                var exists = await _db.Comprobantes.AnyAsync(c =>
                    c.Categoria == categoria && c.PuntoDeVenta == puntoDeVenta &&
                    c.NumeroDesde == numeroDesde && c.Tipo == tipo && c.Fecha.Date == fecha.Date);

                if (exists) { skipped++; continue; }

                var comp = new Comprobante
                {
                    Categoria = categoria,
                    Fecha = fecha,
                    Tipo = tipo,
                    PuntoDeVenta = puntoDeVenta,
                    NumeroDesde = numeroDesde,
                    NumeroHasta = ParseLong(sheet.Cell(row, 5).GetString()),
                    CodAutorizacion = ParseNullableLong(sheet.Cell(row, 6).GetString()),
                    ContraparteTipoDoc = sheet.Cell(row, 7).GetString().Trim(),
                    ContraparteNroDoc = ParseNullableLong(sheet.Cell(row, 8).GetString()),
                    ContraparteDenominacion = sheet.Cell(row, 9).GetString().Trim(),
                    TipoCambio = ParseDecimal(sheet.Cell(row, 10).GetString(), 1),
                    Moneda = sheet.Cell(row, 11).GetString().Trim(),
                    ImporteTotal = ParseNullableDecimal(sheet.Cell(row, sheet.LastColumnUsed()?.ColumnNumber() ?? 12).GetString())
                };

                _db.Comprobantes.Add(comp);
                imported++;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = $"Importados: {imported}, Duplicados omitidos: {skipped}", imported, skipped });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al importar", error = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? categoria)
    {
        var query = _db.Comprobantes.AsQueryable();
        if (!string.IsNullOrEmpty(categoria)) query = query.Where(c => c.Categoria == categoria);
        var items = await query.OrderByDescending(c => c.Fecha).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Comprobantes");

        // Header
        var headers = new[] { "Fecha", "Tipo", "Pto Vta", "Nro Desde", "Nro Hasta", "Cod Autorizacion",
            "Tipo Doc", "Nro Doc", "Denominacion", "Tipo Cambio", "Moneda", "Importe Total" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        for (int i = 0; i < items.Count; i++)
        {
            var c = items[i];
            sheet.Cell(i + 2, 1).Value = c.Fecha.ToString("dd/MM/yyyy");
            sheet.Cell(i + 2, 2).Value = c.Tipo;
            sheet.Cell(i + 2, 3).Value = c.PuntoDeVenta;
            sheet.Cell(i + 2, 4).Value = c.NumeroDesde;
            sheet.Cell(i + 2, 5).Value = c.NumeroHasta;
            sheet.Cell(i + 2, 6).Value = c.CodAutorizacion;
            sheet.Cell(i + 2, 7).Value = c.ContraparteTipoDoc;
            sheet.Cell(i + 2, 8).Value = c.ContraparteNroDoc;
            sheet.Cell(i + 2, 9).Value = c.ContraparteDenominacion;
            sheet.Cell(i + 2, 10).Value = c.TipoCambio;
            sheet.Cell(i + 2, 11).Value = c.Moneda;
            sheet.Cell(i + 2, 12).Value = (double?)(c.ImporteTotal);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"comprobantes_{categoria ?? "todos"}_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private static int ParseInt(string? s) => int.TryParse(s?.Trim().Replace(".", ""), out var v) ? v : 0;
    private static long ParseLong(string? s) => long.TryParse(s?.Trim().Replace(".", ""), out var v) ? v : 0;
    private static long? ParseNullableLong(string? s) => long.TryParse(s?.Trim().Replace(".", ""), out var v) ? v : null;
    private static decimal ParseDecimal(string? s, decimal def) => decimal.TryParse(s?.Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
    private static decimal? ParseNullableDecimal(string? s) => decimal.TryParse(s?.Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static ComprobanteDto ToDto(Comprobante c) => new(
        c.Id, c.Categoria, c.Fecha, c.Tipo, c.PuntoDeVenta, c.NumeroDesde, c.NumeroHasta,
        c.CodAutorizacion, c.ContraparteTipoDoc, c.ContraparteNroDoc, c.ContraparteDenominacion,
        c.TipoCambio, c.Moneda, c.NetoGravIva0, c.Iva25, c.NetoGravIva25, c.Iva5, c.NetoGravIva5,
        c.Iva105, c.NetoGravIva105, c.Iva21, c.NetoGravIva21, c.Iva27, c.NetoGravIva27,
        c.PercIva, c.PercOtrosImp, c.PercIIBB, c.PercImpMuni, c.ImpInterno, c.NoGravado,
        c.OtrosTributos, c.ImporteTotal
    );
}
