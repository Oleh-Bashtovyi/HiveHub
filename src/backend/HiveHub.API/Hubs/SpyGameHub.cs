using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.Commands.ChangeAvatar;
using HiveHub.Application.MediatR.SpyGame.Commands.ChangeHost;
using HiveHub.Application.MediatR.SpyGame.Commands.CreateRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.JoinRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.KickPlayer;
using HiveHub.Application.MediatR.SpyGame.Commands.LeaveRoom;
using HiveHub.Application.MediatR.SpyGame.Commands.RenamePlayer;
using HiveHub.Application.MediatR.SpyGame.Commands.ReturnToLobby;
using HiveHub.Application.MediatR.SpyGame.Commands.RevealSpies;
using HiveHub.Application.MediatR.SpyGame.Commands.SendMessage;
using HiveHub.Application.MediatR.SpyGame.Commands.StartGame;
using HiveHub.Application.MediatR.SpyGame.Commands.ToggleReady;
using HiveHub.Application.MediatR.SpyGame.Commands.UpdateSettings;
using HiveHub.Application.MediatR.SpyGame.Commands.VoteStopTimer;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace HiveHub.API.Hubs;

public class SpyGameHub : Hub<ISpyGameClient>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SpyGameHub> _logger;

    public SpyGameHub(IMediator mediator, ILogger<SpyGameHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object> CreateRoom()
    {
        var command = new CreateRoomCommand(Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true, data = result.Value };
    }

    // Приєднатися до кімнати
    public async Task<object> JoinRoom(string roomCode)
    {
        var command = new JoinRoomCommand(Context.ConnectionId, roomCode);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true, data = result.Value };
    }

    // Вийти з кімнати
    public async Task<object> LeaveRoom(string roomCode)
    {
        var command = new LeaveRoomCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Змінити ім'я
    public async Task<object> ChangeName(string roomCode, string newName)
    {
        var command = new RenamePlayerCommand(roomCode, Context.ConnectionId, newName);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Змінити аватар
    public async Task<object> ChangeAvatar(string roomCode, string avatarId)
    {
        var command = new ChangeAvatarCommand(roomCode, Context.ConnectionId, avatarId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Змінити статус готовності
    public async Task<object> ToggleReady(string roomCode)
    {
        var command = new ToggleReadyCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Передати права хоста
    public async Task<object> ChangeHost(string roomCode, string newHostPlayerId)
    {
        var command = new ChangeHostCommand(roomCode, Context.ConnectionId, newHostPlayerId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Вигнати гравця
    public async Task<object> KickPlayer(string roomCode, string targetPlayerId)
    {
        var command = new KickPlayerCommand(roomCode, Context.ConnectionId, targetPlayerId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Оновити налаштування гри
    public async Task<object> UpdateSettings(string roomCode, RoomGameSettingsDto settings)
    {
        var command = new UpdateGameSettingsCommand(roomCode, Context.ConnectionId, settings);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Почати гру
    public async Task<object> StartGame(string roomCode)
    {
        var command = new StartGameCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Відправити повідомлення в чат
    public async Task<object> SendMessage(string roomCode, string message)
    {
        var command = new SendMessageCommand(roomCode, Context.ConnectionId, message);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Проголосувати за зупинку таймера
    public async Task<object> VoteStopTimer(string roomCode)
    {
        var command = new VoteStopTimerCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Показати шпигунів
    public async Task<object> RevealSpies(string roomCode)
    {
        var command = new RevealSpiesCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }

    // Повернутися в лобі
    public async Task<object> ReturnToLobby(string roomCode)
    {
        var command = new ReturnToLobbyCommand(roomCode, Context.ConnectionId);
        var result = await _mediator.Send(command);

        if (result.IsFailed)
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message };

        return new { success = true };
    }
}