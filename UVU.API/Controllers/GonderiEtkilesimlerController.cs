using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UVU.Domain.Entities;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Controllers;

[ApiController]
[Authorize]
[Route("api/gonderiler/{gonderiId:int}")]
public class GonderiEtkilesimlerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public GonderiEtkilesimlerController(ApplicationDbContext db) => _db = db;

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<bool> CanSee(int id, string userId, CancellationToken ct)
    {
        return await (from g in _db.Gonderiler.AsNoTracking()
                      join u in _db.Users.AsNoTracking() on g.KullaniciId equals u.Id
                      where g.Id == id && g.AktifMi && (!u.GizliHesapMi || g.KullaniciId == userId)
                      select g.Id).AnyAsync(ct);
    }

    [HttpGet("etkilesimler")]
    public async Task<IActionResult> Durum(int gonderiId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        return Ok(new
        {
            begendiMi = await _db.GonderiBegenileri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct),
            kaydettiMi = await _db.GonderiKaydetmeleri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct),
            begeniSayisi = await _db.GonderiBegenileri.CountAsync(x => x.GonderiId == gonderiId, ct),
            yorumSayisi = await _db.GonderiYorumlari.CountAsync(x => x.GonderiId == gonderiId, ct)
        });
    }

    [HttpPut("begeni")]
    public async Task<IActionResult> Begen(int gonderiId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        if (!await _db.GonderiBegenileri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct))
        {
            _db.GonderiBegenileri.Add(new GonderiBegeni { GonderiId = gonderiId, KullaniciId = userId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                if (!await _db.GonderiBegenileri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct)) throw;
            }
        }
        return await Durum(gonderiId, ct);
    }

    [HttpDelete("begeni")]
    public async Task<IActionResult> BegeniyiKaldir(int gonderiId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        await _db.GonderiBegenileri.Where(x => x.GonderiId == gonderiId && x.KullaniciId == userId).ExecuteDeleteAsync(ct);
        return await Durum(gonderiId, ct);
    }

    [HttpPut("kaydet")]
    public async Task<IActionResult> Kaydet(int gonderiId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        if (!await _db.GonderiKaydetmeleri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct))
        {
            _db.GonderiKaydetmeleri.Add(new GonderiKaydetme { GonderiId = gonderiId, KullaniciId = userId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                if (!await _db.GonderiKaydetmeleri.AnyAsync(x => x.GonderiId == gonderiId && x.KullaniciId == userId, ct)) throw;
            }
        }
        return await Durum(gonderiId, ct);
    }

    [HttpDelete("kaydet")]
    public async Task<IActionResult> KaydiKaldir(int gonderiId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        await _db.GonderiKaydetmeleri.Where(x => x.GonderiId == gonderiId && x.KullaniciId == userId).ExecuteDeleteAsync(ct);
        return await Durum(gonderiId, ct);
    }

    public sealed class YorumIstegi
    {
        public string? Icerik { get; set; }
    }

    [HttpPost("yorumlar")]
    public async Task<IActionResult> YorumEkle(int gonderiId, [FromBody] YorumIstegi request, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        var icerik = request.Icerik?.Trim();
        if (string.IsNullOrEmpty(icerik) || icerik.Length > 1000)
            return BadRequest(new { message = "Yorum 1-1000 karakter olmalıdır." });
        var yorum = new GonderiYorum { GonderiId = gonderiId, KullaniciId = userId, Icerik = icerik };
        _db.GonderiYorumlari.Add(yorum);
        await _db.SaveChangesAsync(ct);
        return Ok(new { yorum.Id, yorum.Icerik, yorum.OlusturmaTarihi });
    }

    [HttpGet("yorumlar")]
    public async Task<IActionResult> YorumlariListele(int gonderiId, [FromQuery] int sayfa = 1, CancellationToken ct = default)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        if (!await CanSee(gonderiId, userId, ct)) return NotFound();
        sayfa = Math.Clamp(sayfa, 1, 10000);
        var yorumlar = await (from y in _db.GonderiYorumlari.AsNoTracking()
                              join u in _db.Users.AsNoTracking() on y.KullaniciId equals u.Id
                              where y.GonderiId == gonderiId
                              orderby y.OlusturmaTarihi descending, y.Id descending
                              select new { y.Id, y.Icerik, y.OlusturmaTarihi, y.KullaniciId, u.UserName, u.ProfilFotografi })
                             .Skip((sayfa - 1) * 30).Take(30).ToListAsync(ct);
        return Ok(yorumlar);
    }

    [HttpDelete("yorumlar/{yorumId:int}")]
    public async Task<IActionResult> YorumSil(int gonderiId, int yorumId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();
        var yorum = await _db.GonderiYorumlari.FirstOrDefaultAsync(x => x.Id == yorumId && x.GonderiId == gonderiId, ct);
        if (yorum == null) return NotFound();
        var gonderiSahibi = await _db.Gonderiler.AsNoTracking().Where(x => x.Id == gonderiId).Select(x => x.KullaniciId).FirstOrDefaultAsync(ct);
        if (yorum.KullaniciId != userId && gonderiSahibi != userId) return Forbid();
        _db.GonderiYorumlari.Remove(yorum);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
