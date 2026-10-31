using Microsoft.AspNetCore.Identity;

namespace UVU.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? Ad { get; set; }

    public string? Soyad { get; set; }

    public string? ProfilFotografi { get; set; }

    public string? Biyografi { get; set; }

    public bool GizliHesapMi { get; set; } = false;

    public DateTime KayitTarihi { get; set; } =
        DateTime.UtcNow;
}