using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UVU.Domain.Entities;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Controllers;

[ApiController]
[Authorize]
[Route("api/takip")]
public class TakipController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TakipController(ApplicationDbContext db) => _db = db;
    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPut("{kullaniciId}")]
    public async Task<IActionResult> TakipEt(string kullaniciId, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        if (me == kullaniciId) return BadRequest(new { message = "Kendinizi takip edemezsiniz." });
        var target = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == kullaniciId, ct);
        if (target == null) return NotFound();
        if (target.GizliHesapMi) return BadRequest(new { message = "Gizli hesaplar için takip isteği sistemi henüz etkin değil." });
        if (!await _db.Takipler.AnyAsync(x => x.TakipEdenId == me && x.TakipEdilenId == kullaniciId, ct))
        {
            _db.Takipler.Add(new Takip { TakipEdenId = me, TakipEdilenId = kullaniciId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                if (!await _db.Takipler.AnyAsync(x => x.TakipEdenId == me && x.TakipEdilenId == kullaniciId, ct)) throw;
            }
        }
        return Ok(new { takipEdiliyor = true });
    }

    [HttpDelete("{kullaniciId}")]
    public async Task<IActionResult> TakibiBirak(string kullaniciId, CancellationToken ct)
    {
        var me = Me;
        if (me == null) return Unauthorized();
        await _db.Takipler.Where(x => x.TakipEdenId == me && x.TakipEdilenId == kullaniciId).ExecuteDeleteAsync(ct);
        return Ok(new { takipEdiliyor = false });
    }

    [HttpGet("{kullaniciId}/takipciler")]
    public async Task<IActionResult> Takipciler(string kullaniciId, CancellationToken ct)
    {
        if (Me == null) return Unauthorized();
        if (!await _db.Users.AnyAsync(x => x.Id == kullaniciId, ct)) return NotFound();
        var result = await (from t in _db.Takipler.AsNoTracking()
                            join u in _db.Users.AsNoTracking() on t.TakipEdenId equals u.Id
                            where t.TakipEdilenId == kullaniciId
                            orderby t.OlusturmaTarihi descending
                            select new { u.Id, u.UserName, u.Ad, u.Soyad, u.ProfilFotografi })
                            .Take(100).ToListAsync(ct);
        return Ok(result);
    }

    [HttpGet("{kullaniciId}/takip-edilenler")]
    public async Task<IActionResult> TakipEdilenler(string kullaniciId, CancellationToken ct)
    {
        if (Me == null) return Unauthorized();
        if (!await _db.Users.AnyAsync(x => x.Id == kullaniciId, ct)) return NotFound();
        var result = await (from t in _db.Takipler.AsNoTracking()
                            join u in _db.Users.AsNoTracking() on t.TakipEdilenId equals u.Id
                            where t.TakipEdenId == kullaniciId
                            orderby t.OlusturmaTarihi descending
                            select new { u.Id, u.UserName, u.Ad, u.Soyad, u.ProfilFotografi })
                            .Take(100).ToListAsync(ct);
        return Ok(result);
    }
}
