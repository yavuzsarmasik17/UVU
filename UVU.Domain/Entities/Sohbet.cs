namespace UVU.Domain.Entities;

public class Sohbet
{
    public int Id { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public ICollection<SohbetKatilimci> Katilimcilar { get; set; } = new List<SohbetKatilimci>();
    public ICollection<Mesaj> Mesajlar { get; set; } = new List<Mesaj>();
}

public class SohbetKatilimci
{
    public int Id { get; set; }
    public int SohbetId { get; set; }
    public Sohbet Sohbet { get; set; } = null!;
    public string KullaniciId { get; set; } = "";
    public DateTime? SonOkumaTarihi { get; set; }
}

public class Mesaj
{
    public long Id { get; set; }
    public int SohbetId { get; set; }
    public Sohbet Sohbet { get; set; } = null!;
    public string GonderenId { get; set; } = "";
    public string Tip { get; set; } = "metin";
    public string? Icerik { get; set; }
    public string? MedyaYolu { get; set; }
    public int? GonderiId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? SilinmeTarihi { get; set; }
}
