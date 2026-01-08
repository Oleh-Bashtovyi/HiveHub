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
    string ConnectionId,
    long? ClientVersion
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

        // Використовуємо ExecuteAsync, оскільки всередині є логіка валідації (Result)
        return await roomAccessor.ExecuteAsync<RoomStateDto>((room) =>
        {
            // Оптимізація трафіку: якщо версія не змінилась, повертаємо порожній успіх
            if (request.ClientVersion.HasValue && request.ClientVersion.Value == room.StateVersion)
            {
                // Повертаємо null як маркер "дані не змінились" (фронтенд має це обробити)
                return Result.Ok<RoomStateDto>(null!);
            }

            if (!room.Players.TryGetValue(request.ConnectionId, out var currentPlayer))
            {
                return Results.NotFound<RoomStateDto>("Гравця не знайдено в кімнаті.");
            }

            var playersDto = room.Players.Values
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
                room.GameSettings.Categories.Select(c => new WordsCategory(c.Name, c.Words)).ToList()
            );

            GameStateDto? gameState = null;
            if (room.State == RoomState.InGame || room.State == RoomState.Ended)
            {
                var isSpy = currentPlayer.PlayerState.IsSpy;
                var secretWord = isSpy ? null : room.CurrentSecretWord;
                var showCategory = room.GameSettings.ShowCategoryToSpy || !isSpy;
                var votesCount = room.Players.Values.Count(p => p.PlayerState.VotedToStopTimer);

                gameState = new GameStateDto(
                    CurrentSecretWord: secretWord,
                    Category: showCategory ? room.CurrentCategory : null, // Перевір чи є CurrentCategory в SpyRoom
                    GameStartTime: room.TimerState.GameStartTime ?? DateTime.UtcNow, // Використовуй TimerState
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
