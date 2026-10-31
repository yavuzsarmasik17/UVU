namespace UVU.Domain.Entities;

public class Gonderi
{
    public int Id { get; set; }
    public string KullaniciId { get; set; } = "";
    public string? Aciklama { get; set; }
    public string? FotografYolu { get; set; }
    public string? VideoYolu { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public bool AktifMi { get; set; } = true;

    public ICollection<GonderiBegeni> Begeniler { get; set; } = new List<GonderiBegeni>();
    public ICollection<GonderiKaydetme> Kaydetmeler { get; set; } = new List<GonderiKaydetme>();
    public ICollection<GonderiYorum> Yorumlar { get; set; } = new List<GonderiYorum>();
}
