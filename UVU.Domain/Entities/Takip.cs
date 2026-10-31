namespace UVU.Domain.Entities;

public class Takip
{
    public int Id { get; set; }
    public string TakipEdenId { get; set; } = "";
    public string TakipEdilenId { get; set; } = "";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
}
