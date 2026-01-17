using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.SpyGame;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.StartGame;

public record StartGameCommand(
    string RoomCode,
    string HostConnectionId
) : IRequest<Result>;

public class StartGameHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<StartGameHandler> logger)
    : IRequestHandler<StartGameCommand, Result>
{
    public async Task<Result> Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var secretCategory = string.Empty;
        var secretWord = string.Empty;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (room.IsInGame())
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

            if (room.GameSettings.CustomCategories.Count == 0)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameStartGame.NoCategoriesWasSet);
            }

            if (room.GameSettings.CustomCategories.Any(x => x.Words.Count == 0))
            {
                return Results.ActionFailed(ProjectMessages.SpyGameStartGame.SomeCategoryIsEmpty);
            }

            var random = Random.Shared;
            var newGameState = new SpyRoomGameState();

            // Assign category and word
            var randomCategory = room.GameSettings.CustomCategories[random.Next(room.GameSettings.CustomCategories.Count)];
            var randomWord = randomCategory.Words[random.Next(randomCategory.Words.Count)];
            newGameState.CurrentSecretWord = randomWord;
            newGameState.CurrentCategory = randomCategory.Name;


            // Assigning spy roles
            int targetSpyCount = random.Next(room.GameSettings.MinSpiesCount, room.GameSettings.MaxSpiesCount + 1);
            int spiesCount = Math.Clamp(value: targetSpyCount, min: 0, max: room.Players.Count);

            var spies = room.Players
                .OrderBy(_ => random.Next())
                .Take(spiesCount);

            // Cleanup players states
            foreach (var player in room.Players)
            {
                player.PlayerState.IsSpy = false;
                player.PlayerState.IsDead = false;
                player.PlayerState.VotedToStopTimer = false;
                player.PlayerState.HasUsedAccusation = false;
            }

            foreach (var spy in spies)
            {
                spy.PlayerState.IsSpy = true;
            }

            // Setup Timer
            var roundDuration = TimeSpan.FromMinutes(room.GameSettings.RoundDurationMinutes);
            newGameState.RoundTimerState.Start(roundDuration);
            newGameState.CurrentPhase = SpyGamePhase.Search;
            newGameState.SetResultSnapshot(room.Players);
            
            room.StartGame(newGameState);

            foreach (var player in room.Players)
            {
                var personalState = SpyGameStateMapper.GetRoomStateForPlayer(room, player.IdInRoom);

                var dto = new SpyGameStartedEventDto(personalState);

                context.AddEvent(new TargetedGameStartedEventDto(player.ConnectionId, new(personalState)));
            }

            context.AddEvent(new ScheduleTaskEvent(TaskType.SpyGameRoundTimeUp, request.RoomCode, null, roundDuration));

            secretCategory = room.GameState.CurrentCategory;
            secretWord = room.GameState.CurrentSecretWord;

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: <GAME STARTED> - Category {SecretCategory} Word: {Word}", 
                request.RoomCode, 
                secretCategory, 
                secretWord);
        }

        return result;
    }
}