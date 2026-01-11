using FluentResults;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Queries.GetRoomState;

public record GetRoomStateQuery(
    string RoomCode,
    string ConnectionId
) : IRequest<Result<RoomStateDto>>;

public class GetRoomStateHandler : IRequestHandler<GetRoomStateQuery, Result<RoomStateDto>>
{
    private readonly ISpyGameRepository _repository;
    private readonly ILogger<GetRoomStateHandler> _logger;

    public GetRoomStateHandler(
        ISpyGameRepository repository,
        ILogger<GetRoomStateHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<RoomStateDto>> Handle(GetRoomStateQuery request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);

        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        return await roomAccessor.ReadAsync((room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var currentPlayer))
            {
                return Results.NotFound<RoomStateDto>("Гравця не знайдено в кімнаті за цим з'єднанням.");
            }

            var playersDto = room.Players
                .Select(p => new PlayerDto(
                    p.IdInRoom,
                    p.Name,
                    p.IsHost,
                    p.IsReady,
                    p.AvatarId))
                .ToList();

            var settingsDto = new RoomGameSettingsDto(
                room.GameSettings.TimerMinutes,
                room.GameSettings.SpiesCount,
                room.GameSettings.SpiesKnowEachOther,
                room.GameSettings.ShowCategoryToSpy,
                room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
            );

            GameStateDto? gameState = null;

            if (room.State == RoomState.InGame || room.State == RoomState.Ended)
            {
                var isSpy = currentPlayer.PlayerState.IsSpy;

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
                
                var activeVotesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

                gameState = new GameStateDto(
                    CurrentSecretWord: secretWord,
                    Category: category,
                    GameStartTime: room.TimerState.GameStartTime ?? DateTime.UtcNow,
                    GameEndTime: room.TimerState.PlannedGameEndTime,
                    IsTimerStopped: room.TimerState.IsTimerStopped,
                    TimerStoppedAt: room.TimerState.TimerStoppedAt,
                    TimerVotesCount: activeVotesCount,
                    RecentMessages: room.ChatMessages
                        .TakeLast(50)
                        .Select(m => new ChatMessageDto(m.PlayerId, m.PlayerName, m.Message, m.Timestamp))
                        .ToList()
                );
            }

            var stateDto = new RoomStateDto(
                RoomCode: room.RoomCode,
                State: room.State,
                Players: playersDto,
                Settings: settingsDto,
                GameState: gameState,
                Version: room.StateVersion
            );

            return Result.Ok(stateDto);
        });
    }
}