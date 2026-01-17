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

namespace HiveHub.Application.MediatR.SpyGame.Commands.UpdateGameSettings;

public record UpdateWordPacksCommand(
    string RoomCode,
    string HostConnectionId,
    SpyGameWordPacksDto Packs
) : IRequest<Result>;

public class UpdateWordPacksHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<UpdateWordPacksHandler> logger)
    : IRequestHandler<UpdateWordPacksCommand, Result>
{
    public async Task<Result> Handle(UpdateWordPacksCommand request, CancellationToken cancellationToken)
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

            // Custom words
            if (request.Packs.CustomCategories.Count > ProjectConstants.SpyGame.MaxCustomCategoriesCount)
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.MaxCustomCategoriesCountCannotBeGraterThan);
            }
            if (request.Packs.CustomCategories.SelectMany(x => x.Words).Any(x => x.Length > ProjectConstants.SpyGame.MaxWordInCategoryLength))
            {
                return Results.ValidationFailed(ProjectMessages.SpyGameUpdateSettings.SomeCustomWordsExceedingLengthLimit);
            }

            // Apply changes
            room.GameSettings.CustomCategories = request.Packs.CustomCategories
                .Select(c => new SpyGameWordsCategory
                {
                    Name = c.Name,
                    Words = c.Words
                })
                .ToList();

            context.AddEvent(new SpyGameWordPacksUpdatedEventDto(request.RoomCode, request.Packs));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Word packs updated. " +
                "Categories count: {CategoriesCount}, total words: {TotalWordsCount}", 
                request.RoomCode,
                request.Packs.CustomCategories.Count,
                request.Packs.CustomCategories.SelectMany(x => x.Words).Count());
        }

        return result;
    }
}
