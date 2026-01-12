using HiveHub.API.Dtos;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
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
    private const string RoomCodeKey = "SpyRoomCode";

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

        if (Context.Items.TryGetValue(RoomCodeKey, out var roomCodeObj) && roomCodeObj is string roomCode)
        {
            await _mediator.Send(new HandleDisconnectCommand(Context.ConnectionId, roomCode));
        }

        await base.OnDisconnectedAsync(exception);
    }

    // --- Connection Management ---
    public async Task<ApiResponse<CreateRoomResponseDto>> CreateRoom()
    {
        var result = await _mediator.Send(new CreateRoomCommand(Context.ConnectionId));

        if (result.IsSuccess)
        {
            Context.Items[RoomCodeKey] = result.Value.RoomCode;
        }

        return result.ToApiResponse();
    }

    public async Task<ApiResponse<JoinRoomResponseDto>> JoinRoom(string roomCode)
    {
        var result = await _mediator.Send(new JoinRoomCommand(Context.ConnectionId, roomCode));

        if (result.IsSuccess)
        {
            Context.Items[RoomCodeKey] = roomCode;
        }

        return result.ToApiResponse();
    }

    public async Task<ApiResponse<RoomStateDto>> Reconnect(string roomCode, string lastPlayerId)
    {
        var result = await _mediator.Send(new ReconnectCommand(roomCode, lastPlayerId, Context.ConnectionId));

        if (result.IsSuccess)
        {
            Context.Items[RoomCodeKey] = roomCode;
        }

        return result.ToApiResponse();
    }

    public async Task<ApiResponse> LeaveRoom(string roomCode)
    {
        var result = await _mediator.Send(new LeaveRoomCommand(roomCode, Context.ConnectionId));

        if (result.IsSuccess)
        {
            Context.Items.Remove(RoomCodeKey);
        }

        return result.ToApiResponse();
    }

    // --- Profile ---
    public Task<ApiResponse> ChangeName(string roomCode, string newName)
        => HandleCommand(new RenamePlayerCommand(roomCode, Context.ConnectionId, newName));

    public Task<ApiResponse> ChangeAvatar(string roomCode, string avatarId)
        => HandleCommand(new ChangeAvatarCommand(roomCode, Context.ConnectionId, avatarId));

    public Task<ApiResponse> ToggleReady(string roomCode)
        => HandleCommand(new ToggleReadyCommand(roomCode, Context.ConnectionId));

    // --- Host Actions ---
    public Task<ApiResponse> ChangeHost(string roomCode, string newHostPlayerId)
        => HandleCommand(new ChangeHostCommand(roomCode, Context.ConnectionId, newHostPlayerId));

    public Task<ApiResponse> KickPlayer(string roomCode, string targetPlayerId)
        => HandleCommand(new KickPlayerCommand(roomCode, Context.ConnectionId, targetPlayerId));

    public Task<ApiResponse> UpdateSettings(string roomCode, RoomGameSettingsDto settings)
        => HandleCommand(new UpdateGameSettingsCommand(roomCode, Context.ConnectionId, settings));

    public Task<ApiResponse> ReturnToLobby(string roomCode)
        => HandleCommand(new ReturnToLobbyCommand(roomCode, Context.ConnectionId));

    // --- Gameplay ---
    public Task<ApiResponse> StartGame(string roomCode)
        => HandleCommand(new StartGameCommand(roomCode, Context.ConnectionId));

    public Task<ApiResponse> SendMessage(string roomCode, string message)
        => HandleCommand(new SendMessageCommand(roomCode, Context.ConnectionId, message));

    public Task<ApiResponse> VoteStopTimer(string roomCode)
        => HandleCommand(new VoteStopTimerCommand(roomCode, Context.ConnectionId));

    public Task<ApiResponse> RevealSpies(string roomCode)
        => HandleCommand(new RevealSpiesCommand(roomCode, Context.ConnectionId));
}