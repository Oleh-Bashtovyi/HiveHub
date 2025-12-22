using FluentResults;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Errors;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Domain;
using MediatR;
using HiveHub.Application.Utils;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.JoinRoom;

public record JoinRoomCommand(
    string ConnectionId,
    string RoomCode,
    string PlayerName
) : IRequest<Result<JoinRoomResponseDto>>;

public class JoinRoomHandler(
    SpyGameManager gameManager, 
    ISpyGamePublisher publisher, 
    ILogger<JoinRoomHandler> logger, 
    IIdGenerator idGenerator) 
    : IRequestHandler<JoinRoomCommand, Result<JoinRoomResponseDto>>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<JoinRoomHandler> _logger = logger;
    private readonly IIdGenerator _idGenerator = idGenerator;

    public async Task<Result<JoinRoomResponseDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        JoinRoomResponseDto? response = null;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Гра вже почалась.");
            }

            if (room.Players.Count >= 8)
            {
                return Results.ActionFailed("Кімната заповнена.");
            }

            if (room.Players.ContainsKey(request.ConnectionId))
            {
                return Result.Fail(new ActionFailedError("Ви вже в кімнаті."));
            }

            var publicId = _idGenerator.GenerateId(length: 16);
            bool isHost = room.Players.IsEmpty;

            var newPlayer = new SpyPlayer(request.ConnectionId, publicId)
            {
                Name = request.PlayerName,
                IsHost = isHost
            };

            if (!room.Players.TryAdd(request.ConnectionId, newPlayer))
            {
                return Results.ActionFailed("Помилка додавання гравця.");
            }

            var myDto = new PlayerDto(newPlayer.IdInRoom, newPlayer.Name, newPlayer.IsHost);

            var allPlayersDto = room.Players.Values
                .Select(p => new PlayerDto(p.IdInRoom, p.Name, p.IsHost))
                .ToList();

            var settingsDto = new RoomGameSettings(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategory(c.Name, c.Words)).ToList()
            );

            response = new JoinRoomResponseDto(
                Me: myDto,
                RoomCode: room.RoomCode,
                Players: allPlayersDto,
                Settings: settingsDto
            );

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        if (response == null)
        {
            return Results.ActionFailed("Unknown error");
        }

        _logger.LogInformation("User with connection id: {ConnectionId} and id: {UserId} joined to room with code: {RoomCode}",
            request.ConnectionId,
            response.Me.Id,
            response.RoomCode);

        await _publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, request.RoomCode);

        var eventDto = new PlayerJoinedEventDto(request.RoomCode, response.Me);

        await _publisher.PublishPlayerJoinedAsync(eventDto);

        return Result.Ok(response);
    }
}
