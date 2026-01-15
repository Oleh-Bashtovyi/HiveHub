using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.SpyGame;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.UpdateSettings;

public record UpdateGameSettingsCommand(
    string RoomCode,
    string HostConnectionId,
    SpyRoomGameSettingsDto NewSettings
) : IRequest<Result>;

public class UpdateGameSettingsHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<UpdateGameSettingsHandler> logger)
    : IRequestHandler<UpdateGameSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken)
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
            if (request.NewSettings.TimerMinutes < ProjectConstants.SpyGame.MinGameDurationMinutes || 
                request.NewSettings.TimerMinutes > ProjectConstants.SpyGame.MaxGameDurationMinutes)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.GameTimeMustBeInRange);
            }

            // Spy Counts (Min/Max Logic)
            if (request.NewSettings.MinSpiesCount < 0)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MinSpiesMustBeNonNegative);
            }

            if (request.NewSettings.MaxSpiesCount < 1)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxSpiesMustBeAtLeastOne);
            }

            if (request.NewSettings.MaxSpiesCount > ProjectConstants.SpyGame.MaxPlayersCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxSpiesCannotBeGraterThan);
            }

            if (request.NewSettings.MinSpiesCount > request.NewSettings.MaxSpiesCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MinSpiesCannotExceedMax);
            }

            // Custom words
            if (request.NewSettings.CustomCategories.Count > ProjectConstants.SpyGame.MaxCustomCategoriesCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxCustomCategoriesCountCannotBeGraterThan);
            }

            // Apply changes
            room.GameSettings.RoundDurationMinutes = request.NewSettings.TimerMinutes;
            room.GameSettings.MinSpiesCount = request.NewSettings.MinSpiesCount;
            room.GameSettings.MaxSpiesCount = request.NewSettings.MaxSpiesCount;
            room.GameSettings.SpiesKnowEachOther = request.NewSettings.SpiesKnowEachOther;
            room.GameSettings.ShowCategoryToSpy = request.NewSettings.ShowCategoryToSpy;

            room.GameSettings.CustomCategories = request.NewSettings.CustomCategories
                .Select(c => new SpyGameWordsCategory
                {
                    Name = c.Name,
                    Words = c.Words
                })
                .ToList();

            context.AddEvent(new SpyGameSettingsUpdatedEventDto(request.RoomCode, request.NewSettings));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Game settings updated in room {RoomCode}", request.RoomCode);
        }

        return result;
    }
}