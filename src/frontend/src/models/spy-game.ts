import {SpyHubEvents} from "../const/spy-game-events.ts";

export const RoomState = {
    Lobby: 0,
    InGame: 1,
    Ended: 2,
} as const;

export type RoomState = (typeof RoomState)[keyof typeof RoomState];

// --- Basic Entities ---

export interface PlayerDto {
    id: string;
    name: string;
    isHost: boolean;
    isReady: boolean;
    avatarId: string;
    connectionId?: string; // Optional, internal use
}

export interface WordsCategoryDto {
    name: string;
    words: string[];
}

export interface RoomGameSettingsDto {
    timerMinutes: number;
    spiesCount: number;
    spiesKnowEachOther: boolean;
    showCategoryToSpy: boolean;
    wordsCategories: WordsCategoryDto[];
}

export interface ChatMessageDto {
    playerId: string;
    playerName: string;
    message: string;
    timestamp: string; // DateTime string
}

export interface SpyRevealDto {
    playerId: string;
    playerName: string;
}

// --- Game State & Room State ---

export interface GameStateDto {
    currentSecretWord: string | null;
    category: string | null;
    gameStartTime: string | null;
    gameEndTime: string | null;
    isTimerStopped: boolean;
    timerStoppedAt: string | null;
    timerVotesCount: number;
    recentMessages: ChatMessageDto[];
}

export interface RoomStateDto {
    roomCode: string;
    state: RoomState;
    players: PlayerDto[];
    settings: RoomGameSettingsDto;
    gameState: GameStateDto | null;
    version: number;
}

// --- API Responses (Result of Invoke) ---

export interface ApiResponse<T> {
    success: boolean;
    data?: T;
    error?: string;
}

export interface CreateRoomResponseDto {
    roomCode: string;
    me: PlayerDto;
    settings: RoomGameSettingsDto;
}

export interface JoinRoomResponseDto {
    me: PlayerDto;
    roomCode: string;
    players: PlayerDto[];
    settings: RoomGameSettingsDto;
}

// --- SignalR Events (Server -> Client) ---

export interface PlayerJoinedEventDto {
    roomCode: string;
    player: PlayerDto;
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

export interface GameSettingsUpdatedEventDto {
    roomCode: string;
    settings: RoomGameSettingsDto;
}

export interface GameStartedEventDto {
    roomCode: string;
    isSpy: boolean;
    secretWord: string | null;
    category: string;
    gameEndTime: string;
}

export interface ChatMessageEventDto {
    roomCode: string;
    message: ChatMessageDto;
}

export interface TimerStoppedEventDto {
    roomCode: string;
    votesCount: number;
    requiredVotes: number;
}

export interface SpiesRevealedEventDto {
    roomCode: string;
    spies: SpyRevealDto[];
}

export interface GameEndedEventDto {
    roomCode: string;
}

export interface ReturnToLobbyEventDto {
    roomCode: string;
}

export interface SpyGameEventMap {
    [SpyHubEvents.PlayerJoined]: PlayerJoinedEventDto;
    [SpyHubEvents.PlayerLeft]: PlayerLeftEventDto;
    [SpyHubEvents.PlayerChangedName]: PlayerChangedNameEventDto;
    [SpyHubEvents.PlayerKicked]: PlayerKickedEventDto;
    [SpyHubEvents.PlayerReadyStatusChanged]: PlayerReadyStatusChangedEventDto;
    [SpyHubEvents.PlayerChangedAvatar]: PlayerChangedAvatarEventDto;
    [SpyHubEvents.HostChanged]: HostChangedEventDto;
    [SpyHubEvents.GameSettingsUpdated]: GameSettingsUpdatedEventDto;
    [SpyHubEvents.GameStarted]: GameStartedEventDto;
    [SpyHubEvents.ChatMessageReceived]: ChatMessageEventDto;
    [SpyHubEvents.TimerVoteUpdated]: TimerStoppedEventDto;
    [SpyHubEvents.SpiesRevealed]: SpiesRevealedEventDto;
    [SpyHubEvents.ReturnedToLobby]: ReturnToLobbyEventDto;
}