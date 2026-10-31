
namespace UVU.Domain.Entities;

public class HikayeKatman
{
    public int Id { get; set; }

    public int HikayeId { get; set; }

    // Yazi, Konum, Emoji, Cikartma
    public string Tip { get; set; } = "Yazi";

    public string Icerik { get; set; } = string.Empty;

    // 0 ile 1 arasinda goreceli koordinatlar
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 0.5;

    public double Olcek { get; set; } = 1.0;

    public double DonmeAcisi { get; set; }

    public string? Renk { get; set; }

    public string? YaziTipi { get; set; }

    public int Sira { get; set; }

    public Hikaye Hikaye { get; set; } = null!;
}
