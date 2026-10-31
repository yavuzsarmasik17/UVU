
namespace UVU.Domain.Entities;

public class Hikaye
{
    public int Id { get; set; }

    public string KullaniciId { get; set; } = string.Empty;

    public string MedyaYolu { get; set; } = string.Empty;

    // Fotograf veya Video
    public string MedyaTipi { get; set; } = "Fotograf";

    public string? EfektKodu { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
        = DateTime.UtcNow;

    public DateTime BitisTarihi { get; set; }
        = DateTime.UtcNow.AddHours(24);

    public bool AktifMi { get; set; } = true;

    public ICollection<HikayeKatman> Katmanlar { get; set; }
        = new List<HikayeKatman>();

    public ICollection<HikayeGoruntuleme> Goruntulemeler
    { get; set; } = new List<HikayeGoruntuleme>();

    public HikayeMuzik? Muzik { get; set; }
}
