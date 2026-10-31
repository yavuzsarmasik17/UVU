using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UVU.Domain.Entities;
using UVU.Infrastructure.Identity;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Controllers;

[ApiController]
[Route("api/gonderiler")]
public class GonderilerController : ControllerBase
{
    private const long MaxPhotoBytes = 10L * 1024 * 1024;
    private const long MaxVideoBytes = 500L * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public GonderilerController(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxVideoBytes + MaxPhotoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoBytes + MaxPhotoBytes + 1024 * 1024)]
    public async Task<IActionResult> Olustur(
        IFormFile? fotograf,
        IFormFile? video,
        [FromForm] string? aciklama,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        aciklama = aciklama?.Trim();
        if (string.IsNullOrWhiteSpace(aciklama) && fotograf == null && video == null)
            return BadRequest(new { message = "Fotoğraf, video veya açıklama ekleyin." });
        if (aciklama?.Length > 2200)
            return BadRequest(new { message = "Açıklama en fazla 2200 karakter olabilir." });
        if (fotograf != null && video != null)
            return BadRequest(new { message = "Bir gönderiye fotoğraf veya video ekleyin; ikisini birden değil." });

        IFormFile? media = video ?? fotograf;
        string? mediaPath = null;
        string? savedFile = null;
        if (media != null)
        {
            bool isVideo = video != null;
            long limit = isVideo ? MaxVideoBytes : MaxPhotoBytes;
            if (media.Length <= 0 || media.Length > limit)
                return BadRequest(new { message = isVideo ? "Video en fazla 500 MB olabilir." : "Fotoğraf en fazla 10 MB olabilir." });

            var extension = Path.GetExtension(media.FileName).ToLowerInvariant();
            var allowed = isVideo
                ? new[] { ".mp4", ".mov", ".webm" }
                : new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(extension))
                return BadRequest(new { message = "Desteklenmeyen dosya biçimi." });

            byte[] header = new byte[16];
            int total = 0;
            await using (var input = media.OpenReadStream())
            {
                while (total < header.Length)
                {
                    int read = await input.ReadAsync(header.AsMemory(total, header.Length - total), cancellationToken);
                    if (read == 0) break;
                    total += read;
                }
            }
            if (total < 12 || !HasValidSignature(header, extension))
                return BadRequest(new { message = "Dosya içeriği veya imzası geçersiz." });

            var webRoot = _environment.WebRootPath ??
                Path.Combine(_environment.ContentRootPath, "wwwroot");
            var subfolder = isVideo ? "videos" : "posts";
            var folder = Path.Combine(webRoot, "uploads", subfolder);
            Directory.CreateDirectory(folder);
            var filename = $"{Guid.NewGuid():N}{extension}";
            savedFile = Path.Combine(folder, filename);
            try
            {
                await using var output = new FileStream(savedFile, FileMode.CreateNew, FileAccess.Write);
                await media.CopyToAsync(output, cancellationToken);
            }
            catch
            {
                DeleteIfExists(savedFile);
                throw;
            }
            mediaPath = $"/uploads/{subfolder}/{filename}";
        }

        var gonderi = new Gonderi
        {
            KullaniciId = user.Id,
            Aciklama = aciklama,
            FotografYolu = fotograf != null ? mediaPath : null,
            VideoYolu = video != null ? mediaPath : null,
            OlusturmaTarihi = DateTime.UtcNow,
            AktifMi = true
        };

        try
        {
            _context.Gonderiler.Add(gonderi);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteIfExists(savedFile);
            throw;
        }

        return Ok(new
        {
            message = "Gönderi paylaşıldı.",
            gonderi.Id,
            gonderi.Aciklama,
            gonderi.FotografYolu,
            gonderi.VideoYolu,
            gonderi.OlusturmaTarihi
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Listele(CancellationToken cancellationToken)
    {
        var gonderiler = await (
            from g in _context.Gonderiler.AsNoTracking()
            join u in _userManager.Users.AsNoTracking()
                on g.KullaniciId equals u.Id
            where g.AktifMi && !u.GizliHesapMi
            orderby g.OlusturmaTarihi descending, g.Id descending
            select new
            {
                g.Id,
                g.Aciklama,
                g.FotografYolu,
                g.VideoYolu,
                g.OlusturmaTarihi,
                KullaniciId = u.Id,
                u.UserName,
                u.ProfilFotografi
            }
        ).Take(50).ToListAsync(cancellationToken);
        return Ok(gonderiler);
    }

    [Authorize]
    [HttpGet("benim")]
    public async Task<IActionResult> BenimGonderilerim(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var gonderiler = await _context.Gonderiler.AsNoTracking()
            .Where(x => x.KullaniciId == userId && x.AktifMi)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Aciklama,
                x.FotografYolu,
                x.VideoYolu,
                x.OlusturmaTarihi
            }).ToListAsync(cancellationToken);
        return Ok(gonderiler);
    }

    private static bool HasValidSignature(byte[] h, string ext)
    {
        bool jpeg = h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
        bool png = h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E &&
                   h[3] == 0x47 && h[4] == 0x0D && h[5] == 0x0A &&
                   h[6] == 0x1A && h[7] == 0x0A;
        bool webp = System.Text.Encoding.ASCII.GetString(h, 0, 4) == "RIFF" &&
                    System.Text.Encoding.ASCII.GetString(h, 8, 4) == "WEBP";
        bool isoBmff = System.Text.Encoding.ASCII.GetString(h, 4, 4) == "ftyp";
        bool webm = h[0] == 0x1A && h[1] == 0x45 && h[2] == 0xDF && h[3] == 0xA3;
        return ext switch
        {
            ".jpg" or ".jpeg" => jpeg,
            ".png" => png,
            ".webp" => webp,
            ".mp4" or ".mov" => isoBmff,
            ".webm" => webm,
            _ => false
        };
    }

    private static void DeleteIfExists(string? path)
    {
        if (path != null && System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }
}
