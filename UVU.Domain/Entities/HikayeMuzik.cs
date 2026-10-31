
namespace UVU.Domain.Entities;

public class HikayeMuzik
{
    public int Id { get; set; }

    public int HikayeId { get; set; }

    public string MuzikId { get; set; } = string.Empty;

    public string SarkiAdi { get; set; } = string.Empty;

    public string Sanatci { get; set; } = string.Empty;

    public double BaslangicSaniyesi { get; set; }

    public double SureSaniyesi { get; set; } = 15;

    public Hikaye Hikaye { get; set; } = null!;
}
