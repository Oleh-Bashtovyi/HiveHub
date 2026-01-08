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
    string RoomCode
) : IRequest<Result<JoinRoomResponseDto>>;

public class JoinRoomHandler(
    ISpyGameRepository spyGameRepository,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<JoinRoomHandler> logger,
    IIdGenerator idGenerator)
    : IRequestHandler<JoinRoomCommand, Result<JoinRoomResponseDto>>
{
    private readonly ISpyGameRepository _spyGameRepository = spyGameRepository;
    private readonly IConnectionMappingService _mappingService = mappingService;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<JoinRoomHandler> _logger = logger;
    private readonly IIdGenerator _idGenerator = idGenerator;

    public async Task<Result<JoinRoomResponseDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _spyGameRepository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

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
            var playerName = $"Player {room.Players.Count + 1}";

            var newPlayer = new SpyPlayer(request.ConnectionId, publicId)
            {
                Name = playerName,
                IsHost = isHost,
                AvatarId = "default",
                IsReady = false
            };

            if (!room.Players.TryAdd(request.ConnectionId, newPlayer))
            {
                return Results.ActionFailed("Помилка додавання гравця.");
            }

            var myDto = new PlayerDto(
                newPlayer.IdInRoom,
                newPlayer.Name,
                newPlayer.IsHost,
                newPlayer.IsReady,
                newPlayer.AvatarId);

            var allPlayersDto = room.Players.Values
                .Select(p => new PlayerDto(p.IdInRoom, p.Name, p.IsHost, p.IsReady, p.AvatarId))
                .ToList();

            var settingsDto = new RoomGameSettingsDto(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategory(c.Name, c.Words)).ToList()
            );

            var responseDto = new JoinRoomResponseDto(
                    Me: myDto,
                    RoomCode: room.RoomCode,
                    Players: allPlayersDto,
                    Settings: settingsDto
                );

            return Result.Ok(responseDto);
        });

        if (result.IsFailed)
        {
            return result;
        }

        var response = result.Value;

        if (response == null)
        {
            return Results.ActionFailed("Unknown error");
        }

        _logger.LogInformation("User with connection id: {ConnectionId} and id: {UserId} joined to room with code: {RoomCode}",
            request.ConnectionId,
            response.Me.Id,
            response.RoomCode);

        _mappingService.Map(request.ConnectionId, request.RoomCode);

        await _publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, request.RoomCode);

        var eventDto = new PlayerJoinedEventDto(request.RoomCode, response.Me);

        await _publisher.PublishPlayerJoinedAsync(eventDto);

        return Result.Ok(response);
    }
}
