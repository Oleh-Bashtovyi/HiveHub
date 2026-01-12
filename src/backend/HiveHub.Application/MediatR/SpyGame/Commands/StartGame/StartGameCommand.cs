using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Models;
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
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<StartGameHandler> logger)
    : IRequestHandler<StartGameCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = repository;
    private readonly ITaskScheduler _scheduler = scheduler;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<StartGameHandler> _logger = logger;

    public async Task<Result> Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        List<(string ConnectionId, GameStartedEventDto Payload)> notifications = new();
        TimeSpan? timerDuration = null!;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State == RoomState.InGame)
            {
                return Results.ActionFailed(ProjectMessages.StartGame.GameIsAlreadyStarted);
            }

            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.StartGame.OnlyHostCanStartGame);
            }

            if (room.Players.Count < 3)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameStartGame.MinimumThreePlayersRequiredToStart);
            }

            if (room.Players.Any(p => !p.IsReady))
            {
                return Results.ActionFailed(ProjectMessages.StartGame.NotAllPlayersIsReady);
            }

            if (room.GameSettings.Categories.Count == 0)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameStartGame.NoCategoriesWasSet);
            }

            if (room.GameSettings.Categories.All(x => x.Words.Count == 0))
            {
                return Results.ActionFailed(ProjectMessages.SpyGameStartGame.NoCategoriesWithAtLeastOneWord);
            }

            var random = Random.Shared;
            var randomCategory = room.GameSettings.Categories[random.Next(room.GameSettings.Categories.Count)];
            var randomWord = randomCategory.Words[random.Next(randomCategory.Words.Count)];

            foreach (var player in room.Players)
            {
                player.PlayerState.IsSpy = false;
                player.PlayerState.VotedToStopTimer = false;
            }

            int spiesCount = Math.Min(room.GameSettings.SpiesCount, room.Players.Count - 1);
            var playerConnectionIds = room.Players.Select(x => x.ConnectionId).ToList();

            for (int i = 0; i < spiesCount; i++)
            {
                int index = random.Next(playerConnectionIds.Count);
                var spyConnectionId = playerConnectionIds[index];

                while (room.Players.First(x => x.ConnectionId == spyConnectionId).PlayerState.IsSpy)
                {
                    index = random.Next(playerConnectionIds.Count);
                    spyConnectionId = playerConnectionIds[index];
                }

                room.Players.First(x => x.ConnectionId == spyConnectionId).PlayerState.IsSpy = true;
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

            foreach (var player in room.Players)
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

            timerDuration = duration;
            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        if (timerDuration.HasValue)
        {
            var timerTask = new ScheduledTask(TaskType.SpyGameEndTimeUp, request.RoomCode, null);
            await _scheduler.ScheduleAsync(timerTask, timerDuration.Value);
        }

        _logger.LogInformation("Game started in room {RoomCode}. Word: {Word}", request.RoomCode, "HIDDEN_IN_LOGS");

        foreach (var notification in notifications)
        {
            await _publisher.PublishGameStartedAsync(notification.ConnectionId, notification.Payload);
        }

        return Result.Ok();
    }
}