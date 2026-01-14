using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.Commands.StartAccusation;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.MakeGuess;

public record MakeGuessCommand(
    string RoomCode,
    string ConnectionId,
    string Word
) : IRequest<Result>;

public class MakeGuessHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<StartAccusationHandler> logger) : IRequestHandler<MakeGuessCommand, Result>
{
    public async Task<Result> Handle(MakeGuessCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        SpyGameEndedEventDto? gameEndEvent = null;

        var result = await roomAccessor.ExecuteAsync((room) => 
        {
            if (!room.IsInGame())
            {
                return Results.ActionFailed(ProjectMessages.SpyGameMakeGuess.CanBeDoneOnlyMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (!player.PlayerState.IsSpy)
            {
                return Results.Forbidden(ProjectMessages.SpyGameMakeGuess.OnlySpyCanGuess);
            }

            if (room.CurrentPhase == SpyGamePhase.SpyLastChance)
            {
                if (room.CaughtSpyId != player.IdInRoom)
                {
                    return Results.Forbidden(ProjectMessages.SpyGameMakeGuess.OnlyCaughtSpyCanGuessInFinalPhase);
                }
            }
            else if (room.CurrentPhase == SpyGamePhase.Search)
            {
                // Ok
            }
            else
            {
                return Results.ActionFailed(ProjectMessages.SpyGameMakeGuess.GuessNotAllowedInCurrentPhase);
            }

            var isCorrect = string.Equals(request.Word, room.CurrentSecretWord, StringComparison.OrdinalIgnoreCase);

            room.Status = RoomStatus.Ended;
            room.ActiveVoting = null;

            if (isCorrect)
            {
                room.WinnerTeam = Team.Spies;
                room.GameEndReason = GameEndReason.SpyGuessedWord;
                gameEndEvent = new SpyGameEndedEventDto(room.RoomCode, Team.Spies, GameEndReason.SpyGuessedWord, $"Spy guessed correctly! The word was {room.CurrentSecretWord}");
            }
            else
            {
                room.WinnerTeam = Team.Civilians;
                room.GameEndReason = room.CurrentPhase == SpyGamePhase.SpyLastChance ? GameEndReason.SpyFound : GameEndReason.SpyWrongGuess;
                gameEndEvent = new SpyGameEndedEventDto(room.RoomCode, Team.Civilians, room.GameEndReason.Value, $"Wrong guess! The word was {room.CurrentSecretWord}");
            }

            return Result.Ok();
        });

        if (result.IsFailed) return result;

        if (gameEndEvent != null)
        {
            await scheduler.CancelAsync(new ScheduledTask(TaskType.SpyGameEndTimeUp, request.RoomCode, null));
            await scheduler.CancelAsync(new ScheduledTask(TaskType.SpyVotingTimeUp, request.RoomCode, null));

            await publisher.PublishGameEndedAsync(gameEndEvent);
            logger.LogInformation("Game ended in room {Room} via Guess. Winner: {Winner}", request.RoomCode, gameEndEvent.WinnerTeam);
        }

        return Result.Ok();
    }
}