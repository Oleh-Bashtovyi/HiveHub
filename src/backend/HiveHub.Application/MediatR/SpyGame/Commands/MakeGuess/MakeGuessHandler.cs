using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
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
    ILogger<MakeGuessHandler> logger) : IRequestHandler<MakeGuessCommand, Result>
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
            else if (room.GameState.CurrentPhase != SpyGamePhase.Search)
            {
                return Results.ActionFailed(ProjectMessages.SpyGameMakeGuess.GuessNotAllowedInCurrentPhase);
            }

            var guessWord = request.Word.Trim();
            var isCorrect = StringSimilarityHelper.IsCloseEnough(
                guessWord,
                room.GameState.CurrentSecretWord!);

            guesserId = player.IdInRoom;

            SharedFeatures.MakeGuess.HandleSpyGuess(room, context, guesserId, isCorrect, guessWord);

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