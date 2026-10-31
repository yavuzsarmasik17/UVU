
namespace UVU.Domain.Entities;

public class HikayeGoruntuleme
{
    public int Id { get; set; }

    public int HikayeId { get; set; }

    public string KullaniciId { get; set; } = string.Empty;

    public DateTime GoruntulemeTarihi { get; set; }
        = DateTime.UtcNow;

    public Hikaye Hikaye { get; set; } = null!;
}
