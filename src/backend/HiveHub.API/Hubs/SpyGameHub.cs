using Microsoft.AspNetCore.SignalR;

namespace HiveHub.API.Hubs;

public class SpyGameHub : Hub<ISpyGameClient>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
