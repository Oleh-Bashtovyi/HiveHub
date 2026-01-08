using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.Commands.ChangeAvatar;
using HiveHub.Application.MediatR.SpyGame.Commands.ChangeHost;
using HiveHub.Application.MediatR.SpyGame.Commands.CreateRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.HandleDisconnect;
using HiveHub.Application.MediatR.SpyGame.Commands.JoinRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.KickPlayer;
using HiveHub.Application.MediatR.SpyGame.Commands.LeaveRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.Reconnect;
using HiveHub.Application.MediatR.SpyGame.Commands.RenamePlayer;
using HiveHub.Application.MediatR.SpyGame.Commands.ReturnToLobby;
using HiveHub.Application.MediatR.SpyGame.Commands.RevealSpies;
using HiveHub.Application.MediatR.SpyGame.Commands.SendMessage;
using HiveHub.Application.MediatR.SpyGame.Commands.StartGame;
using HiveHub.Application.MediatR.SpyGame.Commands.ToggleReady;
using HiveHub.Application.MediatR.SpyGame.Commands.UpdateSettings;
using HiveHub.Application.MediatR.SpyGame.Commands.VoteStopTimer;
using MediatR;

namespace HiveHub.API.Hubs;

public class SpyGameHub : BaseGameHub<ISpyGameClient>
{
    private readonly ILogger<SpyGameHub> _logger;

    public SpyGameHub(IMediator mediator, ILogger<SpyGameHub> logger) : base(mediator)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}. Reason: {Message}",
            Context.ConnectionId, exception?.Message ?? "Normal closure");

        await _mediator.Send(new HandleDisconnectCommand(Context.ConnectionId));

        await base.OnDisconnectedAsync(exception);
    }

    // --- Connection Management ---
    public Task<object> CreateRoom()
        => HandleCommand(new CreateRoomCommand(Context.ConnectionId));

    public Task<object> JoinRoom(string roomCode)
        => HandleCommand(new JoinRoomCommand(Context.ConnectionId, roomCode));

    public Task<object> Reconnect(string roomCode, string lastPlayerId)
        => HandleCommand(new ReconnectCommand(roomCode, lastPlayerId, Context.ConnectionId));

    public Task<object> LeaveRoom(string roomCode)
        => HandleCommand(new LeaveRoomCommand(roomCode, Context.ConnectionId));

    // --- Profile ---
    public Task<object> ChangeName(string roomCode, string newName)
        => HandleCommand(new RenamePlayerCommand(roomCode, Context.ConnectionId, newName));

    public Task<object> ChangeAvatar(string roomCode, string avatarId)
        => HandleCommand(new ChangeAvatarCommand(roomCode, Context.ConnectionId, avatarId));

    public Task<object> ToggleReady(string roomCode)
        => HandleCommand(new ToggleReadyCommand(roomCode, Context.ConnectionId));

    // --- Host Actions ---
    public Task<object> ChangeHost(string roomCode, string newHostPlayerId)
        => HandleCommand(new ChangeHostCommand(roomCode, Context.ConnectionId, newHostPlayerId));

    public Task<object> KickPlayer(string roomCode, string targetPlayerId)
        => HandleCommand(new KickPlayerCommand(roomCode, Context.ConnectionId, targetPlayerId));

    public Task<object> UpdateSettings(string roomCode, RoomGameSettingsDto settings)
        => HandleCommand(new UpdateGameSettingsCommand(roomCode, Context.ConnectionId, settings));

    public Task<object> ReturnToLobby(string roomCode)
        => HandleCommand(new ReturnToLobbyCommand(roomCode, Context.ConnectionId));

    // --- Gameplay ---
    public Task<object> StartGame(string roomCode)
        => HandleCommand(new StartGameCommand(roomCode, Context.ConnectionId));

    public Task<object> SendMessage(string roomCode, string message)
        => HandleCommand(new SendMessageCommand(roomCode, Context.ConnectionId, message));

    public Task<object> VoteStopTimer(string roomCode)
        => HandleCommand(new VoteStopTimerCommand(roomCode, Context.ConnectionId));

    public Task<object> RevealSpies(string roomCode)
        => HandleCommand(new RevealSpiesCommand(roomCode, Context.ConnectionId));
}