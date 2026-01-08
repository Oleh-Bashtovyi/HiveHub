using FluentResults;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Reconnect;

public record ReconnectCommand(
    string RoomCode,
    string OldPlayerId,
    string NewConnectionId
) : IRequest<Result<JoinRoomResponseDto>>;

public class ReconnectHandler(
    ISpyGameRepository gameManager,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<ReconnectHandler> logger)
    : IRequestHandler<ReconnectCommand, Result<JoinRoomResponseDto>>
{
    public async Task<Result<JoinRoomResponseDto>> Handle(ReconnectCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null) return Results.NotFound("Room not found.");

        JoinRoomResponseDto? response = null;
        string? oldConnectionId = null;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            var playerPair = room.Players.FirstOrDefault(x => x.Value.IdInRoom == request.OldPlayerId);
            if (playerPair.Value == null)
                return Results.NotFound<JoinRoomResponseDto>("Player not found.");

            oldConnectionId = playerPair.Key;
            var player = playerPair.Value;

            if (room.Players.TryRemove(oldConnectionId, out var existingPlayer))
            {
                existingPlayer.ConnectionId = request.NewConnectionId;
                existingPlayer.IsConnected = true;
                room.Players.TryAdd(request.NewConnectionId, existingPlayer);
            }

            var myDto = new PlayerDto(player.IdInRoom, player.Name, player.IsHost, player.IsReady, player.AvatarId);

            var allPlayersDto = room.Players.Values
                .Select(p => new PlayerDto(p.IdInRoom, p.Name, p.IsHost, p.IsReady, p.AvatarId))
                .ToList();

            var settingsDto = new RoomGameSettingsDto(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategory(c.Name, c.Words)).ToList());

            response = new JoinRoomResponseDto(myDto, room.RoomCode, allPlayersDto, settingsDto);
            return Result.Ok();
        });

        if (result.IsFailed) return result;

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            mappingService.Unmap(oldConnectionId);
        }
        mappingService.Map(request.NewConnectionId, request.RoomCode);

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            await publisher.RemovePlayerFromRoomGroupAsync(oldConnectionId, request.RoomCode);
        }

        await publisher.AddPlayerToRoomGroupAsync(request.NewConnectionId, request.RoomCode);

        logger.LogInformation("Reconnect: {Old} -> {New} in {Room}", oldConnectionId, request.NewConnectionId, request.RoomCode);

        return Result.Ok(response!);
    }
}