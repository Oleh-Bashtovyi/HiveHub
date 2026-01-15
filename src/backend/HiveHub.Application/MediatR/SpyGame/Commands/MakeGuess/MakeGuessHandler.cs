using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
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
    SpyGameEventsContext context,
    ILogger<StartAccusationHandler> logger) : IRequestHandler<MakeGuessCommand, Result>
{
    public async Task<Result> Handle(MakeGuessCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        SpyTeam? winnerTeam = null;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) => 
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

            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameRoundTimeUp, request.RoomCode, null));
            context.AddEvent(new CancelTaskEvent(TaskType.SpyGameVotingTimeUp, request.RoomCode, null));

            if (isCorrect)
            {
                room.WinnerTeam = SpyTeam.Spies;
                room.GameEndReason = SpyGameEndReason.SpyGuessedWord;

                context.AddEvent(new SpyGameEndedEventDto(
                    room.RoomCode, 
                    SpyTeam.Spies, 
                    SpyGameEndReason.SpyGuessedWord,
                    SpiesReveal: room.GetSpyRevealDto(),
                    $"Spy guessed correctly! The word was {room.CurrentSecretWord}"));
            }
            else
            {
                room.WinnerTeam = SpyTeam.Civilians;
                room.GameEndReason = room.CurrentPhase == SpyGamePhase.SpyLastChance ? SpyGameEndReason.SpyFound : SpyGameEndReason.SpyWrongGuess;

                context.AddEvent(new SpyGameEndedEventDto(
                    room.RoomCode, 
                    SpyTeam.Civilians, 
                    room.GameEndReason.Value,
                    SpiesReveal: room.GetSpyRevealDto(),
                    $"Wrong guess! The word was {room.CurrentSecretWord}"));
            }

            winnerTeam = room.WinnerTeam;

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Game ended in room {Room} via Guess. Winner: {Winner}", 
                request.RoomCode, winnerTeam);
        }

        return result;
    }
}