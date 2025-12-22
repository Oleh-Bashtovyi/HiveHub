using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.UpdateGameSettings;

public record UpdateGameSettingsCommand(
    string RoomCode,
    string HostConnectionId,
    RoomGameSettings Settings
) : IRequest<Result>;

public class UpdateGameSettingsHandler(
    SpyGameManager gameManager,
    ISpyGamePublisher publisher,
    ILogger<UpdateGameSettingsHandler> logger)
    : IRequestHandler<UpdateGameSettingsCommand, Result>
{
    private readonly SpyGameManager _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<UpdateGameSettingsHandler> _logger = logger;

    public async Task<Result> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.Lobby)
            {
                return Results.ActionFailed("Не можна змінювати налаштування під час гри.");
            }

            if (!room.Players.TryGetValue(request.HostConnectionId, out var host) || !host.IsHost)
            {
                return Results.ActionFailed("Тільки хост може змінювати налаштування.");
            }

            var s = request.Settings;
            if (s.TimerMinutes < 1 || s.TimerMinutes > 60)
                return Results.ActionFailed("Таймер має бути від 1 до 60 хвилин.");

            if (s.SpiesCount < 1)
                return Results.ActionFailed("Мінімум 1 шпигун.");

            if (s.WordsCategories.Any(c => string.IsNullOrWhiteSpace(c.Name) || c.Words.Count == 0))
                return Results.ActionFailed("Категорії не можуть бути порожніми.");

            var newSettings = new SpyRoomSettings
            {
                TimerMinutes = s.TimerMinutes,
                SpiesCount = s.SpiesCount,
                SpiesKnowEachOther = s.SpiesKnowEachOther,
                ShowCategoryToSpy = s.ShowCategoryToSpy,
                Categories = s.WordsCategories.Select(c => new SpyGameWordsCategory
                {
                    Name = c.Name,
                    Words = c.Words.Where(w => !string.IsNullOrWhiteSpace(w)).ToList()
                }).ToList()
            };

            room.GameSettings.TimerMinutes = newSettings.TimerMinutes;
            room.GameSettings.SpiesCount = newSettings.SpiesCount;
            room.GameSettings.SpiesKnowEachOther = newSettings.SpiesKnowEachOther;
            room.GameSettings.ShowCategoryToSpy = newSettings.ShowCategoryToSpy;
            room.GameSettings.Categories = newSettings.Categories;

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Game settings updated in room {RoomCode}", request.RoomCode);

        await _publisher.PublishGameSettingsUpdatedAsync(new GameSettingsUpdatedEventDto(request.RoomCode, request.Settings));

        return Result.Ok();
    }
}
