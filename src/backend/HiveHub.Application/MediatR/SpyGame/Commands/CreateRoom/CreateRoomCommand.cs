using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.CreateRoom;

public record CreateRoomCommand(string ConnectionId) : IRequest<Result<CreateRoomResponseDto>>;

public class CreateRoomHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ILogger<CreateRoomHandler> logger,
    IIdGenerator idGenerator)
    : IRequestHandler<CreateRoomCommand, Result<CreateRoomResponseDto>>
{
    public async Task<Result<CreateRoomResponseDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var roomCode = await repository.GenerateUniqueRoomCodeAsync();
        var room = new SpyRoom(roomCode);
        var hostPublicId = idGenerator.GenerateId(length: 16);

        var hostPlayer = new SpyPlayer(request.ConnectionId, hostPublicId)
        {
            Name = "Player 1",
            IsHost = true,
            AvatarId = "default",
            IsReady = false,
            IsConnected = true
        };

        room.GameSettings.Categories = CreateDefaultCategories();

        room.Players.Add(hostPlayer);

        if (!await repository.TryAddRoomAsync(room))
        {
            return Results.ActionFailed(ProjectMessages.CreateRoom.UnableToCreateRoom);
        }

        var myDto = new PlayerDto(
            hostPlayer.IdInRoom, 
            hostPlayer.Name,
            IsHost: true,
            IsReady: false,
            AvatarId: "default",
            IsConnected: true,
            IsSpy: null,
            IsVotedToStopTimer: null);

        var settingsDto = new RoomGameSettingsDto(
            room.GameSettings.TimerMinutes,
            room.GameSettings.SpiesCount,
            room.GameSettings.SpiesKnowEachOther,
            room.GameSettings.ShowCategoryToSpy,
            room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
        );

        var response = new CreateRoomResponseDto(roomCode, myDto, settingsDto);

        logger.LogInformation("Room created: {RoomCode}", roomCode);
        await publisher.AddPlayerToRoomGroupAsync(request.ConnectionId, roomCode);

        return Result.Ok(response);
    }

    private static List<SpyGameWordsCategory> CreateDefaultCategories()
    {
        return new List<SpyGameWordsCategory>
        {
            new SpyGameWordsCategory
            {
                Name = "Міста",
                Words = new List<string> { "Київ", "Париж", "Токіо", "Нью-Йорк", "Лондон", "Берлін", "Рим", "Мадрид" }
            },
            new SpyGameWordsCategory
            {
                Name = "Тварини",
                Words = new List<string> { "Собака", "Кіт", "Слон", "Жираф", "Лев", "Тигр", "Ведмідь", "Вовк" }
            },
            new SpyGameWordsCategory
            {
                Name = "Професії",
                Words = new List<string> { "Лікар", "Вчитель", "Інженер", "Художник", "Музикант", "Кухар", "Пілот", "Архітектор" }
            },
            new SpyGameWordsCategory
            {
                Name = "Їжа",
                Words = new List<string> { "Піца", "Суші", "Борщ", "Салат", "Стейк", "Паста", "Торт", "Морозиво" }
            }
        };
    }
}