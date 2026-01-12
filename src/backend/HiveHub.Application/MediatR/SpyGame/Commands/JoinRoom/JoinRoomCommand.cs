using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.JoinRoom;

public record JoinRoomCommand(
    string ConnectionId,
    string RoomCode
) : IRequest<Result<JoinRoomResponseDto>>;

public class JoinRoomHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<JoinRoomHandler> logger,
    IIdGenerator idGenerator)
    : IRequestHandler<JoinRoomCommand, Result<JoinRoomResponseDto>>
{
    private readonly ISpyGameRepository _repository = repository;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<JoinRoomHandler> _logger = logger;
    private readonly IIdGenerator _idGenerator = idGenerator;

    public async Task<Result<JoinRoomResponseDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.JoinRoom.CanNotJoinMidGame);
            }

            if (room.Players.Count >= ProjectConstants.SpyGameMaxPlayersCount)
            {
                return Results.ActionFailed(ProjectMessages.JoinRoom.SpyGameExceedingMaxPlayersCount);
            }

            if (room.Players.Any(x => x.ConnectionId == request.ConnectionId))
            {
                return Results.ActionFailed(ProjectMessages.JoinRoom.YouAreAlreadyInRoom);
            }

            var publicId = _idGenerator.GenerateId(length: 16);
            bool isHost = room.Players.Count == 0;
            var playerName = $"Player {room.Players.Count + 1}";

            var newPlayer = new SpyPlayer(request.ConnectionId, publicId)
            {
                Name = playerName,
                IsHost = isHost,
                AvatarId = "default",
                IsReady = false,
                IsConnected = true,
            };

            room.Players.Add(newPlayer);

            var myDto = new PlayerDto(
                newPlayer.IdInRoom,
                newPlayer.Name,
                newPlayer.IsHost,
                newPlayer.IsReady,
                newPlayer.AvatarId,
                newPlayer.IsConnected,
                newPlayer.PlayerState.IsSpy,
                newPlayer.PlayerState.VotedToStopTimer);

            var allPlayersDto = room.Players
                .Select(p => new PlayerDto(
                    Id: p.IdInRoom, 
                    Name: p.Name, 
                    IsHost: p.IsHost, 
                    IsReady: p.IsReady, 
                    AvatarId: p.AvatarId,
                    IsConnected: p.IsConnected,
                    IsSpy: null,
                    IsVotedToStopTimer: null))
                .ToList();

            var settingsDto = new RoomGameSettingsDto(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
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

        await _publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, request.RoomCode);

        var eventDto = new PlayerJoinedEventDto(request.RoomCode, response.Me);

        await _publisher.PublishPlayerJoinedAsync(eventDto);

        return Result.Ok(response);
    }
}
