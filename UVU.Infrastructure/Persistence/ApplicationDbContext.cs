using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UVU.Infrastructure.Identity;
using UVU.Domain.Entities;

namespace UVU.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Gonderi> Gonderiler => Set<Gonderi>();
    public DbSet<GonderiBegeni> GonderiBegenileri => Set<GonderiBegeni>();
    public DbSet<GonderiYorum> GonderiYorumlari => Set<GonderiYorum>();
    public DbSet<GonderiKaydetme> GonderiKaydetmeleri => Set<GonderiKaydetme>();

    public DbSet<Hikaye> Hikayeler => Set<Hikaye>();
    public DbSet<HikayeKatman> HikayeKatmanlari => Set<HikayeKatman>();
    public DbSet<HikayeMuzik> HikayeMuzikleri => Set<HikayeMuzik>();
    public DbSet<HikayeGoruntuleme> HikayeGoruntulemeleri => Set<HikayeGoruntuleme>();
    public DbSet<Takip> Takipler => Set<Takip>();
    public DbSet<Sohbet> Sohbetler => Set<Sohbet>();
    public DbSet<SohbetKatilimci> SohbetKatilimcilari => Set<SohbetKatilimci>();
    public DbSet<Mesaj> Mesajlar => Set<Mesaj>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Takip>(e =>
        {
            e.ToTable("Takipler");
            e.HasKey(x => x.Id);
            e.Property(x => x.TakipEdenId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TakipEdilenId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.TakipEdenId, x.TakipEdilenId }).IsUnique();
            e.HasIndex(x => x.TakipEdilenId);
            e.HasCheckConstraint("CK_Takip_Farkli", "[TakipEdenId] <> [TakipEdilenId]");
        });

        builder.Entity<Sohbet>(e =>
        {
            e.ToTable("Sohbetler");
            e.HasKey(x => x.Id);
        });

        builder.Entity<SohbetKatilimci>(e =>
        {
            e.ToTable("SohbetKatilimcilari");
            e.HasKey(x => x.Id);
            e.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.SohbetId, x.KullaniciId }).IsUnique();
            e.HasOne(x => x.Sohbet).WithMany(x => x.Katilimcilar)
                .HasForeignKey(x => x.SohbetId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Mesaj>(e =>
        {
            e.ToTable("Mesajlar");
            e.HasKey(x => x.Id);
            e.Property(x => x.GonderenId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Tip).HasMaxLength(20).IsRequired();
            e.Property(x => x.Icerik).HasMaxLength(4000);
            e.Property(x => x.MedyaYolu).HasMaxLength(1000);
            e.HasIndex(x => new { x.SohbetId, x.Id });
            e.HasOne(x => x.Sohbet).WithMany(x => x.Mesajlar)
                .HasForeignKey(x => x.SohbetId).OnDelete(DeleteBehavior.Cascade);
        });


        builder.Entity<GonderiBegeni>(entity =>
        {
            entity.ToTable("GonderiBegenileri");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.GonderiId, x.KullaniciId }).IsUnique();
            entity.HasOne(x => x.Gonderi).WithMany(x => x.Begeniler)
                .HasForeignKey(x => x.GonderiId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GonderiKaydetme>(entity =>
        {
            entity.ToTable("GonderiKaydetmeleri");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.GonderiId, x.KullaniciId }).IsUnique();
            entity.HasOne(x => x.Gonderi).WithMany(x => x.Kaydetmeler)
                .HasForeignKey(x => x.GonderiId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GonderiYorum>(entity =>
        {
            entity.ToTable("GonderiYorumlari");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Icerik).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.GonderiId, x.OlusturmaTarihi });
            entity.HasOne(x => x.Gonderi).WithMany(x => x.Yorumlar)
                .HasForeignKey(x => x.GonderiId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Hikaye>(entity =>
        {
            entity.ToTable("Hikayeler");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.MedyaYolu).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.MedyaTipi).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EfektKodu).HasMaxLength(100);
            entity.HasIndex(x => new { x.KullaniciId, x.BitisTarihi });
        });

        builder.Entity<HikayeKatman>(entity =>
        {
            entity.ToTable("HikayeKatmanlari");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tip).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Icerik).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Renk).HasMaxLength(30);
            entity.Property(x => x.YaziTipi).HasMaxLength(100);
            entity.HasOne(x => x.Hikaye).WithMany(x => x.Katmanlar)
                .HasForeignKey(x => x.HikayeId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HikayeMuzik>(entity =>
        {
            entity.ToTable("HikayeMuzikleri");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MuzikId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SarkiAdi).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Sanatci).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.HikayeId).IsUnique();
            entity.HasOne(x => x.Hikaye).WithOne(x => x.Muzik)
                .HasForeignKey<HikayeMuzik>(x => x.HikayeId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HikayeGoruntuleme>(entity =>
        {
            entity.ToTable("HikayeGoruntulemeleri");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.HikayeId, x.KullaniciId }).IsUnique();
            entity.HasOne(x => x.Hikaye).WithMany(x => x.Goruntulemeler)
                .HasForeignKey(x => x.HikayeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
