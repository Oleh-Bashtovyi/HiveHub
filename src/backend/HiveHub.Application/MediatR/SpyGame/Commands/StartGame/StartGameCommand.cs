using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.StartGame;

public record StartGameCommand(
    string RoomCode,
    string HostConnectionId
) : IRequest<Result>;

public class StartGameHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<StartGameHandler> logger)
    : IRequestHandler<StartGameCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<StartGameHandler> _logger = logger;

    public async Task<Result> Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        List<(string ConnectionId, GameStartedEventDto Payload)> notifications = new();

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State == RoomState.InGame)
            {
                return Results.ActionFailed("Гра вже йде. Спочатку завершіть поточну.");
            }

            if (!room.Players.TryGetValue(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.ActionFailed("Тільки хост може почати гру.");
            }

            if (room.Players.Count < 3)
            {
                return Results.ActionFailed("Недостатньо гравців (мінімум 3).");
            }

            if (room.Players.Values.Any(p => !p.IsReady))
            {
                return Results.ActionFailed("Не всі гравці готові. Всі повинні натиснути 'Готовий'.");
            }

            if (room.GameSettings.Categories.Count == 0)
            {
                return Results.ActionFailed("Немає категорій слів для гри.");
            }

            if (room.GameSettings.Categories.All(x => x.Words.Count == 0))
            {
                return Results.ActionFailed("Немає категорій що містять хоча б одне слово.");
            }

            var random = Random.Shared;
            var randomCategory = room.GameSettings.Categories[random.Next(room.GameSettings.Categories.Count)];
            var randomWord = randomCategory.Words[random.Next(randomCategory.Words.Count)];

            foreach (var player in room.Players.Values)
            {
                player.PlayerState.IsSpy = false;
                player.PlayerState.VotedToStopTimer = false;
            }

            int spiesCount = Math.Min(room.GameSettings.SpiesCount, room.Players.Count - 1);
            var playerIds = room.Players.Keys.ToList();

            for (int i = 0; i < spiesCount; i++)
            {
                int index = random.Next(playerIds.Count);
                var spyConnectionId = playerIds[index];

                while (room.Players[spyConnectionId].PlayerState.IsSpy)
                {
                    index = random.Next(playerIds.Count);
                    spyConnectionId = playerIds[index];
                }

                room.Players[spyConnectionId].PlayerState.IsSpy = true;
            }

            room.CurrentSecretWord = randomWord;
            room.CurrentCategory = randomCategory.Name;

            // Setup Timer
            var now = DateTime.UtcNow;
            var duration = TimeSpan.FromMinutes(room.GameSettings.TimerMinutes);
            room.TimerState.GameStartTime = now;
            room.TimerState.PlannedGameEndTime = now.Add(duration);
            room.TimerState.IsTimerStopped = false;
            room.TimerState.TimerStoppedAt = null;

            room.State = RoomState.InGame;
            room.ChatMessages.Clear();
            room.IncrementVersion();

            foreach (var player in room.Players.Values)
            {
                var isSpy = player.PlayerState.IsSpy;

                var dto = new GameStartedEventDto(
                    RoomCode: room.RoomCode,
                    IsSpy: isSpy,
                    SecretWord: isSpy ? null : randomWord,
                    Category: room.GameSettings.ShowCategoryToSpy || !isSpy ? randomCategory.Name : null,
                    GameEndTime: room.TimerState.PlannedGameEndTime.Value
                );

                notifications.Add((player.ConnectionId, dto));
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Game started in room {RoomCode}. Word: {Word}", request.RoomCode, "HIDDEN_IN_LOGS");

        foreach (var notification in notifications)
        {
            await _publisher.PublishGameStartedAsync(notification.ConnectionId, notification.Payload);
        }

        return Result.Ok();
    }
}