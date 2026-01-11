using FluentResults;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Mappers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Queries.GetRoomState;

public record GetRoomStateQuery(
    string RoomCode,
    string ConnectionId
) : IRequest<Result<RoomStateDto>>;

public class GetRoomStateHandler : IRequestHandler<GetRoomStateQuery, Result<RoomStateDto>>
{
    private readonly ISpyGameRepository _repository;
    private readonly ILogger<GetRoomStateHandler> _logger;

    public GetRoomStateHandler(
        ISpyGameRepository repository,
        ILogger<GetRoomStateHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<RoomStateDto>> Handle(GetRoomStateQuery request, CancellationToken cancellationToken)
    {
        var roomAccessor = _repository.GetRoom(request.RoomCode);

        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        return await roomAccessor.ReadAsync((room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var currentPlayer))
            {
                return Results.NotFound<RoomStateDto>("Гравця не знайдено в кімнаті за цим з'єднанням.");
            }

            var state = SpyGameStateMapper.GetRoomStateForPlayer(room, currentPlayer.IdInRoom);

            return Result.Ok(state);
        });
    }
}