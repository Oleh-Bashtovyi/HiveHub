using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.MediatR.SpyGame.Commands.StartAccusation;
using HiveHub.Application.MediatR.SpyGame.SharedFeatures;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;
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

        var guesserId = string.Empty;

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

            if (room.GameState.CurrentPhase == SpyGamePhase.SpyLastChance)
            {
                if (room.GameState.CaughtSpyId != player.IdInRoom)
                {
                    return Results.Forbidden(ProjectMessages.SpyGameMakeGuess.OnlyCaughtSpyCanGuessInFinalPhase);
                }
            }
            else if (room.GameState.CurrentPhase == SpyGamePhase.Search)
            {
                // Ok
            }
            else
            {
                return Results.ActionFailed(ProjectMessages.SpyGameMakeGuess.GuessNotAllowedInCurrentPhase);
            }

            var isCorrect = StringSimilarityHelper.IsCloseEnough(
                request.Word,
                room.GameState.CurrentSecretWord!);

            guesserId = player.IdInRoom;

            if (isCorrect)
            {
                room.GameState.WinnerTeam = SpyTeam.Spies;
                room.GameState.GameEndReason = SpyGameEndReason.SpyGuessedWord;

                context.AddEvent(new SpyGameEndedEventDto(
                    room.RoomCode, 
                    SpyTeam.Spies, 
                    SpyGameEndReason.SpyGuessedWord,
                    SpiesReveal: room.GetSpyRevealDto(),
                    $"Spy guessed correctly! The word was {room.GameState.CurrentSecretWord}"));
            }
            else
            {
                player.PlayerState.IsDead = true;
                room.GameState.SpyRevealSnapshot.First(x => x.IdInRoom == player.IdInRoom).IsDead = true;

                SpyGameLogicHelper.HandleSpyGuessedWrong(room, context);
            }

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} made guess: {GuessWord}", 
                request.RoomCode, 
                guesserId,
                request.Word.Trim());
        }

        return result;
    }
}