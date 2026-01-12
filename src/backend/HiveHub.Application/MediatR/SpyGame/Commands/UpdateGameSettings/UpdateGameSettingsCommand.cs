using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.UpdateSettings;

public record UpdateGameSettingsCommand(
    string RoomCode,
    string HostConnectionId,
    RoomGameSettingsDto NewSettings
) : IRequest<Result>;

public class UpdateGameSettingsHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<UpdateGameSettingsHandler> logger)
    : IRequestHandler<UpdateGameSettingsCommand, Result>
{
    private readonly ISpyGameRepository _repository = repository;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<UpdateGameSettingsHandler> _logger = logger;

    public async Task<Result> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed(ProjectMessages.UpdateSettings.CanNotChangeGameSettingsMidGame);
            }

            if (!room.TryGetPlayerByConnectionId(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.Forbidden(ProjectMessages.UpdateSettings.OnlyHostCanChangeGameSettings);
            }

            if (request.NewSettings.TimerMinutes < ProjectConstants.SpyGameMinGameDurationMinutes || 
                request.NewSettings.TimerMinutes > ProjectConstants.SpyGameMaxGameDurationMinutes)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.GameTimeMustBeInRange);
            }

            if (request.NewSettings.SpiesCount < 1)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.SpiesCountMustBeMinimumOne);
            }

            room.GameSettings.TimerMinutes = request.NewSettings.TimerMinutes;
            room.GameSettings.SpiesCount = request.NewSettings.SpiesCount;
            room.GameSettings.SpiesKnowEachOther = request.NewSettings.SpiesKnowEachOther;
            room.GameSettings.ShowCategoryToSpy = request.NewSettings.ShowCategoryToSpy;

            room.GameSettings.Categories = request.NewSettings.WordsCategories
                .Select(c => new SpyGameWordsCategory
                {
                    Name = c.Name,
                    Words = c.Words
                })
                .ToList();

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Game settings updated in room {RoomCode}", request.RoomCode);

        var eventDto = new GameSettingsUpdatedEventDto(request.RoomCode, request.NewSettings);
        await _publisher.PublishGameSettingsUpdatedAsync(eventDto);

        return Result.Ok();
    }
}