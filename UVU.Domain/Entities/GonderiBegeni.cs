namespace UVU.Domain.Entities;

public class GonderiBegeni
{
    public int Id { get; set; }
    public int GonderiId { get; set; }
    public string KullaniciId { get; set; } = "";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public Gonderi Gonderi { get; set; } = null!;
}
