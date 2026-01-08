using FluentResults;
using HiveHub.Application.Dtos.Events;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.SendMessage;

public record SendMessageCommand(
    string RoomCode,
    string ConnectionId,
    string Message
) : IRequest<Result>;

public class SendMessageHandler(
    ISpyGameRepository gameManager,
    ISpyGamePublisher publisher,
    ILogger<SendMessageHandler> logger)
    : IRequestHandler<SendMessageCommand, Result>
{
    private readonly ISpyGameRepository _gameManager = gameManager;
    private readonly ISpyGamePublisher _publisher = publisher;
    private readonly ILogger<SendMessageHandler> _logger = logger;

    public async Task<Result> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var roomAccessor = _gameManager.GetRoom(request.RoomCode);
        if (roomAccessor == null)
        {
            return Results.NotFound("Кімната не знайдена.");
        }

        ChatMessageDto messageDto = null;

        var result = await roomAccessor.ExecuteAsync((room) =>
        {
            if (room.State != RoomState.InGame)
            {
                return Results.ActionFailed("Чат доступний тільки під час гри.");
            }

            if (!room.Players.TryGetValue(request.ConnectionId, out var player))
            {
                return Results.NotFound("Гравця не знайдено.");
            }

            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 500)
            {
                return Results.ActionFailed("Повідомлення повинно бути від 1 до 500 символів.");
            }

            var chatMessage = new ChatMessage(player.IdInRoom, player.Name, request.Message.Trim(), DateTime.UtcNow);

            room.ChatMessages.Add(chatMessage);

            messageDto = new ChatMessageDto(
                chatMessage.PlayerId,
                chatMessage.PlayerName,
                chatMessage.Message,
                chatMessage.Timestamp
            );

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        _logger.LogInformation("Chat message sent in room {RoomCode} by player {PlayerId}",
            request.RoomCode, 
            messageDto.PlayerId);

        var eventDto = new ChatMessageEventDto(request.RoomCode, messageDto);
        await _publisher.PublishChatMessageAsync(eventDto);

        return Result.Ok();
    }
}