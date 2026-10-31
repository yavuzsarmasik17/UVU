using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using UVU.API.Hubs;
using UVU.Domain.Entities;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Controllers;

[ApiController]
[Authorize]
[Route("api/sohbetler")]
public class SohbetlerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<SohbetHub> _hub;
    public SohbetlerController(ApplicationDbContext db, IWebHostEnvironment env, IHubContext<SohbetHub> hub)
    {
        _db = db;
        _env = env;
        _hub = hub;
    }
    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public sealed class BaslatIstegi { public string? KullaniciId { get; set; } }
    public sealed class MesajIstegi { public string? Icerik { get; set; } }

    [HttpPost("baslat")]
    public async Task<IActionResult> Baslat([FromBody] BaslatIstegi request, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        var other = request.KullaniciId?.Trim();
        if (string.IsNullOrEmpty(other) || other == me) return BadRequest(new { message = "Geçersiz kullanıcı." });
        if (!await _db.Users.AnyAsync(x => x.Id == other, ct)) return NotFound();
        // İlk sürüm: iki kullanıcıdan birinin diğerini takip etmesi gerekir.
        var related = await _db.Takipler.AnyAsync(x =>
            (x.TakipEdenId == me && x.TakipEdilenId == other) ||
            (x.TakipEdenId == other && x.TakipEdilenId == me), ct);
        if (!related) return StatusCode(403, new { message = "Sohbet için takip ilişkisi gerekiyor." });
        var existing = await _db.Sohbetler.AsNoTracking()
            .Where(s => s.Katilimcilar.Count == 2 &&
                s.Katilimcilar.Any(k => k.KullaniciId == me) &&
                s.Katilimcilar.Any(k => k.KullaniciId == other))
            .Select(s => (int?)s.Id).FirstOrDefaultAsync(ct);
        if (existing.HasValue) return Ok(new { sohbetId = existing.Value });
        var sohbet = new Sohbet();
        sohbet.Katilimcilar.Add(new SohbetKatilimci { KullaniciId = me });
        sohbet.Katilimcilar.Add(new SohbetKatilimci { KullaniciId = other });
        _db.Sohbetler.Add(sohbet);
        await _db.SaveChangesAsync(ct);
        return Ok(new { sohbetId = sohbet.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Listele(CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        var rows = await _db.Sohbetler.AsNoTracking()
            .Where(s => s.Katilimcilar.Any(k => k.KullaniciId == me))
            .Select(s => new
            {
                sohbetId = s.Id,
                okunmamisMesajSayisi = s.Mesajlar.Count(m => m.GonderenId != me &&
                    m.SilinmeTarihi == null && m.OlusturmaTarihi >
                    (s.Katilimcilar.Where(k => k.KullaniciId == me)
                        .Select(k => k.SonOkumaTarihi).FirstOrDefault() ?? DateTime.MinValue)),
                sonMesaj = s.Mesajlar.OrderByDescending(m => m.Id)
                    .Select(m => m.SilinmeTarihi == null ? m.Icerik : null).FirstOrDefault(),
                sonMesajTarihi = s.Mesajlar.OrderByDescending(m => m.Id)
                    .Select(m => (DateTime?)m.OlusturmaTarihi).FirstOrDefault(),
                digerKullanici = s.Katilimcilar.Where(k => k.KullaniciId != me)
                    .Join(_db.Users, k => k.KullaniciId, u => u.Id,
                        (k, u) => new { u.Id, u.UserName, u.ProfilFotografi }).FirstOrDefault()
            })
            .OrderByDescending(x => x.sonMesajTarihi).Take(100).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{sohbetId:int}/mesajlar")]
    public async Task<IActionResult> Mesajlar(int sohbetId, [FromQuery] long? oncekiId, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        if (!await _db.SohbetKatilimcilari.AnyAsync(x => x.SohbetId == sohbetId && x.KullaniciId == me, ct)) return NotFound();
        var query = _db.Mesajlar.AsNoTracking().Where(x => x.SohbetId == sohbetId);
        if (oncekiId.HasValue) query = query.Where(x => x.Id < oncekiId.Value);
        var rows = await query.OrderByDescending(x => x.Id).Take(50)
            .Select(x => new { x.Id, x.GonderenId, x.Tip, x.Icerik, x.MedyaYolu, x.GonderiId, x.OlusturmaTarihi, silindiMi = x.SilinmeTarihi != null })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("{sohbetId:int}/mesajlar")]
    public async Task<IActionResult> MesajGonder(int sohbetId, [FromBody] MesajIstegi request, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        if (!await _db.SohbetKatilimcilari.AnyAsync(x => x.SohbetId == sohbetId && x.KullaniciId == me, ct)) return NotFound();
        var text = request.Icerik?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 4000)
            return BadRequest(new { message = "Mesaj 1-4000 karakter olmalı." });
        var mesaj = new Mesaj { SohbetId = sohbetId, GonderenId = me, Icerik = text };
        _db.Mesajlar.Add(mesaj);
        await _db.SaveChangesAsync(ct);
        await _hub.Clients.Group(SohbetHub.SohbetGrubu(sohbetId))
            .SendAsync("YeniMesaj", new
            {
                mesaj.Id,
                mesaj.SohbetId,
                mesaj.GonderenId,
                mesaj.Tip,
                mesaj.Icerik,
                mesaj.MedyaYolu,
                mesaj.GonderiId,
                mesaj.OlusturmaTarihi,
                silindiMi = false
            }, CancellationToken.None);
        return Ok(new { mesaj.Id, mesaj.Icerik, mesaj.Tip, mesaj.OlusturmaTarihi });
    }

    // Multipart alanları: dosya (IFormFile), tip (foto veya video).
    [HttpPost("{sohbetId:int}/medya")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(105_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 105_000_000)]
    public async Task<IActionResult> MedyaGonder(
    int sohbetId,
    IFormFile? dosya,
    [FromForm] string? tip,
    CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        if (!await _db.SohbetKatilimcilari.AnyAsync(
            x => x.SohbetId == sohbetId && x.KullaniciId == me, ct)) return NotFound();
        if (dosya == null || dosya.Length == 0)
            return BadRequest(new { message = "Dosya seçilmedi." });

        var isPhoto = tip == "foto";
        var isVideo = tip == "video";
        if (!isPhoto && !isVideo)
            return BadRequest(new { message = "Geçersiz medya türü." });
        var limit = isPhoto ? 10L * 1024 * 1024 : 100L * 1024 * 1024;
        if (dosya.Length > limit)
            return BadRequest(new { message = "Dosya boyutu sınırı aşıldı." });

        var ext = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        var allowed = isPhoto
            ? new[] { ".jpg", ".jpeg", ".png", ".webp" }
            : new[] { ".mp4", ".mov" };
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Desteklenmeyen dosya uzantısı." });

        await using var source = dosya.OpenReadStream();
        var signature = new byte[16];
        var read = await source.ReadAsync(signature.AsMemory(), ct);
        bool valid = read >= 12 && (ext switch
        {
            ".jpg" or ".jpeg" => signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
            ".png" => signature.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => System.Text.Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" &&
                       System.Text.Encoding.ASCII.GetString(signature, 8, 4) == "WEBP",
            ".mp4" or ".mov" => System.Text.Encoding.ASCII.GetString(signature, 4, 4) == "ftyp",
            _ => false
        });
        if (!valid) return BadRequest(new { message = "Dosya biçimi doğrulanamadı." });

        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var directory = Path.Combine(root, "uploads", "chat");
        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(directory, name);
        try
        {
            await using (var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true))
            {
                // Baştaki imza baytları dahil dosyanın tamamını kaydet.
                await target.WriteAsync(signature.AsMemory(0, read), ct);
                await source.CopyToAsync(target, ct);
            }
            var relativePath = $"/uploads/chat/{name}";
            var mesaj = new Mesaj
            {
                SohbetId = sohbetId,
                GonderenId = me,
                Tip = isPhoto ? "foto" : "video",
                MedyaYolu = relativePath
            };
            _db.Mesajlar.Add(mesaj);
            await _db.SaveChangesAsync(ct);
            await _hub.Clients.Group(SohbetHub.SohbetGrubu(sohbetId))
                .SendAsync("YeniMesaj", new
                {
                    mesaj.Id,
                    mesaj.SohbetId,
                    mesaj.GonderenId,
                    mesaj.Tip,
                    mesaj.Icerik,
                    mesaj.MedyaYolu,
                    mesaj.GonderiId,
                    mesaj.OlusturmaTarihi,
                    silindiMi = false
                }, CancellationToken.None);
            return Ok(new { mesaj.Id, mesaj.Tip, mesaj.MedyaYolu, mesaj.OlusturmaTarihi });
        }
        catch
        {
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            throw;
        }
    }

    [HttpPost("{sohbetId:int}/okundu")]
    public async Task<IActionResult> Okundu(int sohbetId, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        var count = await _db.SohbetKatilimcilari.Where(x => x.SohbetId == sohbetId && x.KullaniciId == me)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SonOkumaTarihi, DateTime.UtcNow), ct);
        return count == 0 ? NotFound() : NoContent();
    }
}
