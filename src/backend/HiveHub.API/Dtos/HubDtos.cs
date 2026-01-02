namespace HiveHub.API.Dtos;

public record CreateRoomRequestDto
{
    public required string PlayerName { get; init; }
}
