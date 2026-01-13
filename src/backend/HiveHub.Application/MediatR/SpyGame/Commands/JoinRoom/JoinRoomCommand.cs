using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
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
    public async Task<Result<JoinRoomResponseDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.JoinRoom.CanNotJoinMidGame);
            }

            if (room.Players.Count >= ProjectConstants.SpyGame.MaxPlayersCount)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameJoinRoom.ExceedingMaxPlayersCount);
            }

            if (room.Players.Any(x => x.ConnectionId == request.ConnectionId))
            {
                return Results.ActionFailed(ProjectMessages.JoinRoom.YouAreAlreadyInRoom);
            }

            var publicId = idGenerator.GenerateId(length: ProjectConstants.PlayerIdLength);
            var isHost = room.Players.Count == 0;

            var newPlayer = new SpyPlayer(request.ConnectionId, publicId)
            {
                Name = $"Player {room.Players.Count + 1}",
                IsHost = isHost,
                AvatarId = ProjectConstants.DefaultAvatarId,
                IsReady = false,
                IsConnected = true,
            };

            room.Players.Add(newPlayer);

            var roomStateDto = SpyGameStateMapper.GetRoomStateForPlayer(room, publicId);
            var meDto = roomStateDto.Players.First(x => x.Id == publicId);

            var responseDto = new JoinRoomResponseDto(meDto, roomStateDto);

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

        logger.LogInformation("User with connection id: {ConnectionId} and id: {UserId} joined to room with code: {RoomCode}",
            request.ConnectionId,
            response.Me.Id,
            response.RoomState.RoomCode);

        await publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, request.RoomCode);

        var eventDto = new PlayerJoinedEventDto(request.RoomCode, response.Me);

        await publisher.PublishPlayerJoinedAsync(eventDto);

        return Result.Ok(response);
    }
}
