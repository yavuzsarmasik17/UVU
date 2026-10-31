
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UVU.Domain.Entities;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Controllers;

[ApiController]
[Route("api/hikayeler")]
[Authorize]
public class HikayelerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    private const long MaxFotografBoyutu = 10 * 1024 * 1024;

    public HikayelerController(
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private string? KullaniciId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    // =========================================
    // 1. HİKÂYE PAYLAŞ
    // POST /api/hikayeler
    // =========================================

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Paylas(
     IFormFile? fotograf,
     CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KullaniciId))
            return Unauthorized();

        if (fotograf == null || fotograf.Length == 0)
            return BadRequest(new
            {
                mesaj = "Lütfen bir fotoğraf seçin."
            });

        if (fotograf.Length > MaxFotografBoyutu)
            return BadRequest(new
            {
                mesaj = "Fotoğraf en fazla 10 MB olabilir."
            });

        // İlk sürümde fotoğraf destekliyoruz.
        // Video desteğini ayrı medya işleme
        // aşamasında ekleyeceğiz.
        var uzanti = Path.GetExtension(
            fotograf.FileName).ToLowerInvariant();

        var izinliUzantilar = new[]
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        if (!izinliUzantilar.Contains(uzanti))
            return BadRequest(new
            {
                mesaj = "JPG, PNG veya WEBP fotoğraf yükleyin."
            });

        var izinliMimeTipleri = new Dictionary<string, string[]>
        {
            ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
            ["image/png"] = new[] { ".png" },
            ["image/webp"] = new[] { ".webp" }
        };

        if (!izinliMimeTipleri.TryGetValue(
                fotograf.ContentType.ToLowerInvariant(),
                out var uzantilar)
            || !uzantilar.Contains(uzanti))
        {
            return BadRequest(new
            {
                mesaj = "Fotoğraf dosyasının türü geçersiz."
            });
        }

        // Dosya başlığını kontrol et.
        // Sadece dosya uzantısına güvenmiyoruz.
        var gecerliFotograf = await FotografDogrula(
            fotograf, uzanti, cancellationToken);

        if (!gecerliFotograf)
            return BadRequest(new
            {
                mesaj = "Fotoğraf dosyası geçersiz."
            });

        var webRoot = _env.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(
                _env.ContentRootPath, "wwwroot");
        }

        var klasor = Path.Combine(
            webRoot, "uploads", "stories");

        Directory.CreateDirectory(klasor);

        var dosyaAdi =
            $"{Guid.NewGuid():N}{uzanti}";

        var tamYol = Path.Combine(
            klasor, dosyaAdi);

        try
        {
            await using (var stream =
                new FileStream(
                    tamYol,
                    FileMode.CreateNew,
                    FileAccess.Write))
            {
                await fotograf.CopyToAsync(
                    stream, cancellationToken);
            }

            var simdi = DateTime.UtcNow;

            var hikaye = new Hikaye
            {
                KullaniciId = KullaniciId!,
                MedyaYolu =
                    $"/uploads/stories/{dosyaAdi}",
                MedyaTipi = "Fotograf",
                OlusturmaTarihi = simdi,
                BitisTarihi = simdi.AddHours(24),
                AktifMi = true
            };

            _db.Hikayeler.Add(hikaye);

            await _db.SaveChangesAsync(
                cancellationToken);

            return Ok(new
            {
                mesaj = "Hikâye paylaşıldı.",
                hikaye = new
                {
                    hikaye.Id,
                    hikaye.KullaniciId,
                    hikaye.MedyaYolu,
                    hikaye.MedyaTipi,
                    hikaye.OlusturmaTarihi,
                    hikaye.BitisTarihi
                }
            });
        }
        catch
        {
            if (System.IO.File.Exists(tamYol))
                System.IO.File.Delete(tamYol);

            throw;
        }
    }

    // =========================================
    // 2. AKTİF HİKÂYELER
    // GET /api/hikayeler
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Listele(
        CancellationToken cancellationToken)
    {
        var simdi = DateTime.UtcNow;

        var hikayeler = await _db.Hikayeler
            .AsNoTracking()
            .Where(x =>
                x.AktifMi &&
                x.BitisTarihi > simdi)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Select(x => new
            {
                x.Id,
                x.KullaniciId,
                x.MedyaYolu,
                x.MedyaTipi,
                x.EfektKodu,
                x.OlusturmaTarihi,
                x.BitisTarihi,
                GoruntulendiMi = _db.HikayeGoruntulemeleri
                    .Any(g =>
                        g.HikayeId == x.Id &&
                        g.KullaniciId == KullaniciId)
            })
            .ToListAsync(cancellationToken);

        return Ok(hikayeler);
    }

    // =========================================
    // 3. KENDİ HİKÂYELERİM
    // GET /api/hikayeler/benim
    // =========================================

    [HttpGet("benim")]
    public async Task<IActionResult> Benim(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KullaniciId))
            return Unauthorized();

        var simdi = DateTime.UtcNow;

        var hikayeler = await _db.Hikayeler
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == KullaniciId &&
                x.AktifMi &&
                x.BitisTarihi > simdi)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Select(x => new
            {
                x.Id,
                x.MedyaYolu,
                x.MedyaTipi,
                x.OlusturmaTarihi,
                x.BitisTarihi,
                GoruntulemeSayisi =
                    x.Goruntulemeler.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(hikayeler);
    }

    // =========================================
    // 4. HİKÂYE GÖRÜNTÜLE
    // POST /api/hikayeler/{id}/goruntule
    // =========================================

    [HttpPost("{id:int}/goruntule")]
    public async Task<IActionResult> Goruntule(
        int id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KullaniciId))
            return Unauthorized();

        var simdi = DateTime.UtcNow;

        var hikaye = await _db.Hikayeler
            .FirstOrDefaultAsync(
                x => x.Id == id &&
                     x.AktifMi &&
                     x.BitisTarihi > simdi,
                cancellationToken);

        if (hikaye == null)
            return NotFound(new
            {
                mesaj = "Hikâye bulunamadı veya süresi doldu."
            });

        // Kendi hikâyemizi görüntüleme
        // sayısına dahil etmiyoruz.
        if (hikaye.KullaniciId == KullaniciId)
            return Ok(new
            {
                mesaj = "Kendi hikâyeniz."
            });

        var mevcutKayit = await _db
            .HikayeGoruntulemeleri
            .AnyAsync(
                x => x.HikayeId == id &&
                     x.KullaniciId == KullaniciId,
                cancellationToken);

        if (!mevcutKayit)
        {
            _db.HikayeGoruntulemeleri.Add(
                new HikayeGoruntuleme
                {
                    HikayeId = id,
                    KullaniciId = KullaniciId!,
                    GoruntulemeTarihi = simdi
                });

            try
            {
                await _db.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Eşzamanlı isteklerde benzersiz indeks
                // aynı görüntülemeyi ikinci kez engeller.
                // Diğer veritabanı hatalarını gizlemiyoruz.
                var zatenKayitli = await _db
                    .HikayeGoruntulemeleri
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.HikayeId == id &&
                             x.KullaniciId == KullaniciId,
                        cancellationToken);

                if (!zatenKayitli)
                    throw;

                _db.ChangeTracker.Clear();
            }
        }

        return Ok(new
        {
            mesaj = "Görüntüleme kaydedildi."
        });
    }

    // =========================================
    // 5. HİKÂYE SİL
    // DELETE /api/hikayeler/{id}
    // =========================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(
        int id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(KullaniciId))
            return Unauthorized();

        var hikaye = await _db.Hikayeler
            .FirstOrDefaultAsync(
                x => x.Id == id &&
                     x.KullaniciId == KullaniciId,
                cancellationToken);

        if (hikaye == null)
            return NotFound(new
            {
                mesaj = "Hikâye bulunamadı."
            });

        hikaye.AktifMi = false;

        await _db.SaveChangesAsync(
            cancellationToken);

        return Ok(new
        {
            mesaj = "Hikâye silindi."
        });
    }

    // =========================================
    // FOTOĞRAF DOSYA İMZASI KONTROLÜ
    // =========================================

    private static async Task<bool> FotografDogrula(
        IFormFile dosya,
        string uzanti,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[12];

        await using var stream = dosya.OpenReadStream();

        var okunan = await stream.ReadAsync(
            buffer.AsMemory(0, buffer.Length),
            cancellationToken);

        if (uzanti is ".jpg" or ".jpeg")
        {
            return okunan >= 3 &&
                buffer[0] == 0xFF &&
                buffer[1] == 0xD8 &&
                buffer[2] == 0xFF;
        }

        if (uzanti == ".png")
        {
            byte[] imza =
            {
                137, 80, 78, 71, 13, 10, 26, 10
            };

            return okunan >= 8 &&
                buffer.Take(8).SequenceEqual(imza);
        }

        if (uzanti == ".webp")
        {
            return okunan >= 12 &&
                System.Text.Encoding.ASCII.GetString(
                    buffer, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(
                    buffer, 8, 4) == "WEBP";
        }

        return false;
    }
}
