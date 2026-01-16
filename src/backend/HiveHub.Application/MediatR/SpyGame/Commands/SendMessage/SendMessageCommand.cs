using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Extensions;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.SendMessage;

public record SendMessageCommand(
    string RoomCode,
    string ConnectionId,
    string Message
) : IRequest<Result>;

public class SendMessageHandler(
    ISpyGameRepository repository,
    SpyGameEventsContext context,
    ILogger<SendMessageHandler> logger)
    : IRequestHandler<SendMessageCommand, Result>
{
    public async Task<Result> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        ChatMessageDto messageDto = null!;

        var result = await roomAccessor.ExecuteAndDispatchAsync(context, (room) =>
        {
            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var player))
            {
                return Results.NotFound(ProjectMessages.PlayerNotFound);
            }

            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > ProjectConstants.MessageMaxLength)
            {
                return Results.ValidationFailed(ProjectMessages.SendMessage.BadMessageFormat);
            }

            var chatMessage = new ChatMessage(player.IdInRoom, player.Name, request.Message.Trim(), DateTime.UtcNow);
            room.ChatMessages.Add(chatMessage);

            if (room.ChatMessages.Count >= ProjectConstants.MessagesMaxCount)
            {
                room.ChatMessages.RemoveAt(0);
            }

            messageDto = new ChatMessageDto(
                chatMessage.PlayerId,
                chatMessage.PlayerName,
                chatMessage.Message,
                chatMessage.Timestamp
            );

            context.AddEvent(new ChatMessageEventDto(request.RoomCode, messageDto));

            return Result.Ok();
        });

        if (result.IsSuccess)
        {
            logger.LogInformation("Room [{RoomCode}]: Player {PlayerId} sent a message in chat",
                messageDto.PlayerId,
                request.RoomCode);
        }

        return result;
    }
}