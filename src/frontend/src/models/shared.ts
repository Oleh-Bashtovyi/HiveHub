// --- Enums ---
export const ResponseStatus = {
    Success: "Success",
    ActionFailed: "Failed",
    Forbidden: "Forbidden",
    ValidationFailed: "ValidationFailed",
    NotFound: "Not Found",
    UnknownError: "UnknownError",
} as const;
export type ResponseStatus = (typeof ResponseStatus)[keyof typeof ResponseStatus];

export const RoomStatus = {
    Lobby: 'Lobby',
    InGame: 'InGame',
    Ended: 'Ended',
} as const;
export type RoomStatus = (typeof RoomStatus)[keyof typeof RoomStatus];

export const TargetVoteType = {
    Yes: 'Yes',
    No: 'No',
    Skip: 'Skip',
} as const;
export type TargetVoteType = (typeof TargetVoteType)[keyof typeof TargetVoteType];

// --- API Responses ---
export interface ApiResponse<T> {
    status: ResponseStatus;
    success: boolean;
    data?: T;
    error?: string;
}

// --- DTOs ---
export interface PlayerDto {
    id: string;
    name: string;
    avatarId: string;
    isHost: boolean;
    isReady: boolean;
    isConnected: boolean;
}

export interface ChatMessageDto {
    playerId: string;
    playerName: string;
    message: string;
    timestamp: string;
}

// --- Events DTOs ---
export interface ReturnToLobbyEventDto {
    roomCode: string;
}

export interface ChatMessageEventDto {
    roomCode: string;
    message: ChatMessageDto;
}

export interface PlayerLeftEventDto {
    roomCode: string;
    playerId: string;
}

export interface PlayerChangedNameEventDto {
    roomCode: string;
    playerId: string;
    newName: string;
}

export interface PlayerKickedEventDto {
    roomCode: string;
    playerId: string;
    kickedByPlayerId: string;
}

export interface PlayerConnectionChangedEventDto {
    roomCode: string;
    playerId: string;
    isConnected: boolean;
}

export interface PlayerReadyStatusChangedEventDto {
    roomCode: string;
    playerId: string;
    isReady: boolean;
}

export interface PlayerChangedAvatarEventDto {
    roomCode: string;
    playerId: string;
    newAvatarId: string;
}

export interface HostChangedEventDto {
    roomCode: string;
    newHostId: string;
}
