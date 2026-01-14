namespace HiveHub.Application.Dtos.Shared;

public record PlayerDto(
    string Id,
    string Name,
    string AvatarId,
    bool IsHost,
    bool IsReady,
    bool IsConnected
);

public record ChatMessageDto(
    string PlayerId,
    string PlayerName,
    string Message,
    DateTime Timestamp
);


public record JoinRoomResponseDto<TMe, TRoomState>(
    TMe Me,
    TRoomState RoomState
);

public record CreateRoomResponseDto<TMe, TRoomState>(
    TMe Me,
    TRoomState RoomState
);

