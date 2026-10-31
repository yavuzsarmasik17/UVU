using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UVU.Infrastructure.Persistence;

namespace UVU.API.Hubs;

[Authorize]
public sealed class SohbetHub : Hub
{
    private readonly ApplicationDbContext _db;

    public SohbetHub(ApplicationDbContext db)
    {
        _db = db;
    }

    // Aynı isim controller tarafından da kullanılabilir.
    public static string SohbetGrubu(int sohbetId) => $"sohbet:{sohbetId}";

    public async Task SohbeteKatil(int sohbetId)
    {
        if (sohbetId <= 0)
            throw new HubException("Geçersiz sohbet.");

        var kullaniciId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(kullaniciId))
            throw new HubException("Oturum bulunamadı.");

        var yetkili = await _db.SohbetKatilimcilari.AsNoTracking()
            .AnyAsync(x => x.SohbetId == sohbetId && x.KullaniciId == kullaniciId,
                Context.ConnectionAborted);

        if (!yetkili)
            throw new HubException("Bu sohbete erişim yetkiniz yok.");

        await Groups.AddToGroupAsync(Context.ConnectionId,
            SohbetGrubu(sohbetId), Context.ConnectionAborted);
    }

    public Task SohbettenAyril(int sohbetId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId,
            SohbetGrubu(sohbetId), Context.ConnectionAborted);
    }
}
