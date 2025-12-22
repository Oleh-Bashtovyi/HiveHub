using FluentResults;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.CreateRoom;

public record CreateRoomCommand(
    string ConnectionId,
    string PlayerName
) : IRequest<Result<CreateRoomResponseDto>>;

public class CreateRoomHandler(
    SpyGameManager gameManager,
    ISpyGamePublisher publisher,
    ILogger<CreateRoomHandler> logger,
    IIdGenerator idGenerator)
    : IRequestHandler<CreateRoomCommand, Result<CreateRoomResponseDto>>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<CreateRoomHandler> _logger = logger;
    private readonly IIdGenerator _idGenerator = idGenerator;

    public async Task<Result<CreateRoomResponseDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var roomCode = _gameManager.GenerateUniqueRoomCode();

        var room = new SpyRoom(roomCode);

        var hostPublicId = _idGenerator.GenerateId(length: 16);
        var hostPlayer = new SpyPlayer(request.ConnectionId, hostPublicId)
        {
            Name = request.PlayerName,
            IsHost = true
        };

        if (!room.Players.TryAdd(request.ConnectionId, hostPlayer))
        {
            return Results.ActionFailed("Не вдалося додати хоста в кімнату.");
        }

        if (!_gameManager.TryAddRoom(room))
        {
            return Results.ActionFailed("Не вдалося зареєструвати кімнату.");
        }

        var myDto = new PlayerDto(hostPlayer.IdInRoom, hostPlayer.Name, hostPlayer.IsHost);

        var settingsDto = new RoomGameSettings(
            room.GameSettings.TimerMinutes,
            room.GameSettings.SpiesCount,
            room.GameSettings.SpiesKnowEachOther,
            room.GameSettings.ShowCategoryToSpy,
            room.GameSettings.Categories.Select(c => new WordsCategory(c.Name, c.Words)).ToList()
        );

        var response = new CreateRoomResponseDto(
            RoomCode: roomCode,
            Me: myDto,
            Settings: settingsDto
        );

        _logger.LogInformation("Room created with code: {RoomCode} by host: {HostId}", roomCode, hostPublicId);

        await _publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, roomCode);

        return Result.Ok(response);
    }
}