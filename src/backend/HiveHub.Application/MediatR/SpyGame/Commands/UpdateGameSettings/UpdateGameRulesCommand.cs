using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;
using System;

namespace HiveHub.Application.MediatR.SpyGame.Commands.UpdateGameSettings;

public record UpdateGameRulesCommand(
    string RoomCode,
    string HostConnectionId,
    SpyGameRulesDto NewRules
) : IRequest<Result>;

public class UpdateGameRulesHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<UpdateGameRulesHandler> logger) 
    : IRequestHandler<UpdateGameRulesCommand, Result>
{
    public async Task<Result> Handle(UpdateGameRulesCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            // State and Permissions
            if (!room.IsInLobby())
            {
                return Results.ActionFailed(ProjectMessages.UpdateSettings.CanNotChangeGameSettingsMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.UpdateSettings.OnlyHostCanChangeGameSettings);
            }

            // Validation: Timer
            if (request.NewRules.TimerMinutes < ProjectConstants.SpyGame.MinGameDurationMinutes ||
                request.NewRules.TimerMinutes > ProjectConstants.SpyGame.MaxGameDurationMinutes)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.GameTimeMustBeInRange);
            }

            // Spy Counts (Min/Max Logic)
            if (request.NewRules.MinSpiesCount < 0)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MinSpiesMustBeNonNegative);
            }

            if (request.NewRules.MaxSpiesCount < 1)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxSpiesMustBeAtLeastOne);
            }

            if (request.NewRules.MaxSpiesCount > ProjectConstants.SpyGame.MaxPlayersCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxSpiesCannotBeGraterThan);
            }

            if (request.NewRules.MinSpiesCount > request.NewRules.MaxSpiesCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MinSpiesCannotExceedMax);
            }

            // Max players
            if (request.NewRules.MaxPlayersCount < 3)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MustBeAtLeastThreePlayers);
            }
            if (request.NewRules.MaxPlayersCount > ProjectConstants.SpyGame.MaxPlayersCount)
            {
                return Results.ValidationFailed(
                    ProjectMessages.SpyGameUpdateSettings.ExceedingMaxPlayersCount(ProjectConstants.SpyGame.MaxPlayersCount));
            }

            // Apply changes
            room.GameSettings.RoundDurationMinutes = request.NewRules.TimerMinutes;
            room.GameSettings.MinSpiesCount = request.NewRules.MinSpiesCount;
            room.GameSettings.MaxSpiesCount = request.NewRules.MaxSpiesCount;
            room.GameSettings.MaxPlayerCount = request.NewRules.MaxPlayersCount;
            room.GameSettings.SpiesKnowEachOther = request.NewRules.IsSpiesKnowEachOther;
            room.GameSettings.ShowCategoryToSpy = request.NewRules.IsShowCategoryToSpy;
            room.GameSettings.SpiesPlayAsTeam = request.NewRules.IsSpiesPlayAsTeam;

            context.AddEvent(new SpyGameRulesUpdatedEventDto(request.RoomCode, request.NewRules));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Game rules updated. " +
                "Spies: [{MaxSpiesCount}-{MaxSpiesCount}], Timer: {TimerMinutes}", 
                request.RoomCode,
                request.NewRules.MinSpiesCount,
                request.NewRules.MaxSpiesCount,
                request.NewRules.TimerMinutes);
        }

        return result;
    }
}
