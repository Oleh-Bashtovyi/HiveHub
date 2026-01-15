using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.SpyGame;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.JoinRoom;

public record JoinRoomCommand(
    string ConnectionId,
    string RoomCode
) : IRequest<Result<JoinRoomResponseDto<SpyPlayerDto, SpyRoomStateDto>>>;

public class JoinRoomHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<JoinRoomHandler> logger,
    IIdGenerator idGenerator)
    : IRequestHandler<JoinRoomCommand, Result<JoinRoomResponseDto<SpyPlayerDto, SpyRoomStateDto>>>
{
    public async Task<Result<JoinRoomResponseDto<SpyPlayerDto, SpyRoomStateDto>>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
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

            var responseDto = new JoinRoomResponseDto<SpyPlayerDto, SpyRoomStateDto>(meDto, roomStateDto);

            context.AddEvent(new AddPlayerToGroupEvent(request.ConnectionId, request.RoomCode));
            context.AddEvent(new PlayerJoinedEventDto<SpyPlayerDto>(request.RoomCode, meDto));

            return Result.Ok(responseDto);
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("User with connection id: {ConnectionId} and id: {UserId} joined to room with code: {RoomCode}",
                request.ConnectionId,
                result.Value.Me.Id,
                result.Value.RoomState.RoomCode);
        }

        return result;
    }
}
