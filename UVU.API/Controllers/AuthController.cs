
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UVU.Infrastructure.Identity;

namespace UVU.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _configuration = configuration;
        _environment = environment;
    }

    // ==========================================
    // KULLANICI KAYIT
    // POST /api/auth/register
    // ==========================================

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Zorunlu alanları doldurun."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            Ad = request.Ad?.Trim(),
            Soyad = request.Soyad?.Trim(),
            KayitTarihi = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(
            user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Kayıt oluşturulamadı.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            message = "UVU hesabınız oluşturuldu.",
            userId = user.Id,
            userName = user.UserName
        });
    }

    // ==========================================
    // KULLANICI GİRİŞ
    // POST /api/auth/login
    // ==========================================

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email ?? "");

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Unauthorized(new
            {
                message = "Hesap geçici olarak kilitli."
            });
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(
                user, request.Password ?? "");

        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var expiryMinutes = int.Parse(
            _configuration["Jwt:ExpiryMinutes"] ?? "60");

        var expires = DateTime.UtcNow.AddMinutes(
            expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return Ok(new
        {
            message = "Giriş başarılı.",
            accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAt = expires,
            user = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Ad,
                user.Soyad
            }
        });
    }

    // ==========================================
    // GİRİŞ YAPAN KULLANICI
    // GET /api/auth/me
    // ==========================================

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.Ad,
            user.Soyad,
            user.ProfilFotografi,
            user.Biyografi,
            user.GizliHesapMi
        });
    }

    // ==========================================
    // PROFİL BİLGİLERİNİ GÜNCELLE
    // PUT /api/auth/profile
    // ==========================================

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new
            {
                message = "Kullanıcı adı boş olamaz."
            });
        }

        if (request.Biyografi?.Length > 160)
        {
            return BadRequest(new
            {
                message = "Biyografi en fazla 160 karakter olabilir."
            });
        }

        var newUserName = request.UserName.Trim();

        if (!string.Equals(
            user.UserName,
            newUserName,
            StringComparison.OrdinalIgnoreCase))
        {
            var existingUser =
                await _userManager.FindByNameAsync(
                    newUserName);

            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                return BadRequest(new
                {
                    message = "Bu kullanıcı adı zaten kullanılıyor."
                });
            }
        }

        user.UserName = newUserName;
        user.Ad = request.Ad?.Trim();
        user.Soyad = request.Soyad?.Trim();
        user.Biyografi = request.Biyografi?.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Profil güncellenemedi.",
                errors = result.Errors.Select(
                    e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Profil başarıyla güncellendi.",
            user = new
            {
                user.Id,
                user.UserName,
                user.Ad,
                user.Soyad,
                user.Biyografi,
                user.ProfilFotografi,
                user.GizliHesapMi
            }
        });
    }

    // ==========================================
    // PROFİL FOTOĞRAFI YÜKLE
    // POST /api/auth/profile/photo
    // ==========================================

    [Authorize]
    [HttpPost("profile/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePhoto(
         IFormFile file)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Lütfen fotoğraf seçin."
            });
        }

        const long maxFileSize = 5 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            return BadRequest(new
            {
                message = "Fotoğraf en fazla 5 MB olabilir."
            });
        }

        var extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        if (extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png" &&
            extension != ".webp")
        {
            return BadRequest(new
            {
                message =
                    "Yalnızca JPG, PNG veya WEBP yüklenebilir."
            });
        }

        // Dosya imzasını kontrol et.
        var header = new byte[12];

        await using (var input = file.OpenReadStream())
        {
            var totalRead = 0;

            while (totalRead < header.Length)
            {
                var read = await input.ReadAsync(
                    header.AsMemory(
                        totalRead,
                        header.Length - totalRead));

                if (read == 0)
                    break;

                totalRead += read;
            }

            if (totalRead < header.Length)
            {
                return BadRequest(new
                {
                    message = "Geçersiz fotoğraf dosyası."
                });
            }
        }

        bool isJpeg =
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF;

        bool isPng =
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A;

        bool isWebp =
            Encoding.ASCII.GetString(header, 0, 4)
                == "RIFF" &&
            Encoding.ASCII.GetString(header, 8, 4)
                == "WEBP";

        bool validFormat =
            ((extension == ".jpg" ||
              extension == ".jpeg") && isJpeg) ||
            (extension == ".png" && isPng) ||
            (extension == ".webp" && isWebp);

        if (!validFormat)
        {
            return BadRequest(new
            {
                message = "Dosya biçimi geçersiz."
            });
        }

        // Fotoğrafların saklanacağı klasör.
        var webRoot = _environment.WebRootPath ??
            Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");

        var folder = Path.Combine(
            webRoot,
            "uploads",
            "profiles");

        Directory.CreateDirectory(folder);

        // Güvenli ve benzersiz dosya adı.
        var fileName =
            $"{Guid.NewGuid():N}{extension}";

        var filePath = Path.Combine(
            folder,
            fileName);

        await using (var stream = new FileStream(
            filePath,
            FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var oldPhoto = user.ProfilFotografi;

        user.ProfilFotografi =
            $"/uploads/profiles/{fileName}";

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            System.IO.File.Delete(filePath);

            return BadRequest(new
            {
                message =
                    "Profil fotoğrafı kaydedilemedi.",
                errors = result.Errors.Select(
                    e => e.Description)
            });
        }

        // Eski fotoğrafı kaldır.
        if (!string.IsNullOrWhiteSpace(oldPhoto) &&
            oldPhoto.StartsWith(
                "/uploads/profiles/",
                StringComparison.Ordinal))
        {
            var oldName = Path.GetFileName(oldPhoto);
            var oldPath = Path.Combine(
                folder,
                oldName);

            if (System.IO.File.Exists(oldPath))
            {
                try
                {
                    System.IO.File.Delete(oldPath);
                }
                catch (IOException)
                {
                    // Profil güncellemesi başarılı.
                    // Eski dosya daha sonra temizlenebilir.
                }
                catch (UnauthorizedAccessException)
                {
                    // Dosya silme yetkisi yoksa
                    // başarılı işlemi geri alma.
                }
            }
        }

        return Ok(new
        {
            message = "Profil fotoğrafı güncellendi.",
            profilFotografi = user.ProfilFotografi
        });
    }
}

// ==========================================
// İSTEK MODELLERİ
// ==========================================

public class RegisterRequest
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Ad { get; set; }
    public string? Soyad { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string UserName { get; set; } = "";
    public string? Ad { get; set; }
    public string? Soyad { get; set; }
    public string? Biyografi { get; set; }
}
