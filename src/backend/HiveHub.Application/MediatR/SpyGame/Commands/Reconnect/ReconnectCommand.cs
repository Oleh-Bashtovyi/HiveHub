using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Reconnect;

public record ReconnectCommand(
    string RoomCode,
    string OldPlayerId,
    string NewConnectionId
) : IRequest<Result<RoomStateDto>>;

public class ReconnectHandler(
    ISpyGameRepository gameManager,
    IConnectionMappingService mappingService,
    ISpyGamePublisher publisher,
    ILogger<ReconnectHandler> logger)
    : IRequestHandler<ReconnectCommand, Result<RoomStateDto>>
{
    public async Task<Result<RoomStateDto>> Handle(ReconnectCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Room not found.");
        }

        string? oldConnectionId = null;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            var player = room.Players.FirstOrDefault(x => x.IdInRoom == request.OldPlayerId);
            if (player == null)
            {
                return Results.NotFound<RoomStateDto>("Player not found.");
            }

            oldConnectionId = player.ConnectionId;
            player.ConnectionId = request.NewConnectionId;
            player.IsConnected = true;

            var playersDto = room.Players
                .Select(p => new PlayerDto(p.IdInRoom, p.Name, p.IsHost, p.IsReady, p.AvatarId))
                .ToList();

            var settingsDto = new RoomGameSettingsDto(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList());

            GameStateDto? gameState = null;

            if (room.State == RoomState.InGame || room.State == RoomState.Ended)
            {
                var isSpy = player.PlayerState.IsSpy;
                string? secretWord;
                string? category;

                if (room.State == RoomState.Ended)
                {
                    secretWord = room.CurrentSecretWord;
                    category = room.CurrentCategory;
                }
                else
                {
                    secretWord = isSpy ? null : room.CurrentSecretWord;
                    var canSeeCategory = !isSpy || room.GameSettings.ShowCategoryToSpy;
                    category = canSeeCategory ? room.CurrentCategory : null;
                }

                var votesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

                gameState = new GameStateDto(
                    CurrentSecretWord: secretWord,
                    Category: category,
                    GameStartTime: room.TimerState.GameStartTime ?? DateTime.UtcNow,
                    GameEndTime: room.TimerState.PlannedGameEndTime,
                    IsTimerStopped: room.TimerState.IsTimerStopped,
                    TimerStoppedAt: room.TimerState.TimerStoppedAt,
                    TimerVotesCount: votesCount,
                    RecentMessages: room.ChatMessages
                        .TakeLast(50)
                        .Select(m => new ChatMessageDto(m.PlayerId, m.PlayerName, m.Message, m.Timestamp))
                        .ToList()
                );
            }

            return new RoomStateDto(
                RoomCode: room.RoomCode,
                State: room.State,
                Players: playersDto,
                Settings: settingsDto,
                GameState: gameState,
                Version: room.StateVersion
            );
        });

        if (result.IsFailed) return result;

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            mappingService.Unmap(oldConnectionId);
            await publisher.RemovePlayerFromRoomGroupAsync(oldConnectionId, request.RoomCode);
        }

        mappingService.Map(request.NewConnectionId, request.RoomCode);
        await publisher.AddPlayerToRoomGroupAsync(request.NewConnectionId, request.RoomCode);

        logger.LogInformation("Reconnect: {Old} -> {New} in {Room}",
            oldConnectionId, request.NewConnectionId, request.RoomCode);

        var connectionEvent = new PlayerConnectionChangedEventDto(request.RoomCode, request.OldPlayerId, true);
        await publisher.PublishPlayerConnectionChangedAsync(connectionEvent);

        return result;
    }
}