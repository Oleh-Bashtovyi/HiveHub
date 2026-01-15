import { SpyHubEvents } from "../const/spy-game-events.ts";

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

export const SpyVotingType = {
    Accusation: 'Accusation',
    Final: 'Final',
} as const;
export type SpyVotingType = (typeof SpyVotingType)[keyof typeof SpyVotingType];

export const TargetVoteType = {
    Yes: 'Yes',
    No: 'No',
    Skip: 'Skip',
} as const;
export type TargetVoteType = (typeof TargetVoteType)[keyof typeof TargetVoteType];

export const SpyGamePhase = {
    None: 'None',
    Search: 'Search',
    Accusation: 'Accusation',
    FinalVote: 'FinalVote',
    SpyLastChance: 'SpyLastChance',
} as const;
export type SpyGamePhase = (typeof SpyGamePhase)[keyof typeof SpyGamePhase];

export const SpyGameTeam = {
    Civilians: 'Civilians',
    Spies: 'Spies',
} as const;
export type SpyGameTeam = (typeof SpyGameTeam)[keyof typeof SpyGameTeam];

export const SpyGameEndReason = {
    TimerExpired: 'TimerExpired',
    CivilianKicked: 'CivilianKicked',
    SpyGuessedWord: 'SpyGuessedWord',
    SpyWrongGuess: 'SpyWrongGuess',
    FinalVotingFailed: 'FinalVotingFailed',
    SpyFound: 'SpyFound',
} as const;
export type SpyGameEndReason = (typeof SpyGameEndReason)[keyof typeof SpyGameEndReason];

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
    timestamp: string; // DateTime ISO string
}

export interface SpyPlayerDto extends PlayerDto {
    isSpy?: boolean | null;
    isVotedToStopTimer?: boolean | null;
    hasUsedAccusation?: boolean | null;
}

export interface SpyRoomGameSettingsDto {
    timerMinutes: number;
    minSpiesCount: number;
    maxSpiesCount: number;
    spiesKnowEachOther: boolean;
    showCategoryToSpy: boolean;
    customCategories: WordsCategoryDto[];
}

export interface WordsCategoryDto {
    name: string;
    words: string[];
}

export interface VotingStateDto {
    type: SpyVotingType;
    accusedPlayerId: string | null;
    accusedPlayerName: string | null;
    targetVoting: Record<string, TargetVoteType> | null;
    againstVoting: Record<string, string> | null;
    votesRequired: number | null;
    startedAt: string;
    endsAt: string;
}

export interface SpyGameStateDto {
    currentSecretWord: string | null;
    category: string | null;
    roundStartedAt: string;
    isRoundTimerStopped: boolean;
    roundTimerStartedAt: string | null;
    roundTimerWillStopAt: string | null;
    roundTimerPausedAt: string | null;
    lastChanceEndsAt?: string | null;
    timerVotesCount: number;
    phase: SpyGamePhase;
    activeVoting: VotingStateDto | null;
    caughtSpyId: string | null;
    caughtSpyName: string | null;
}

export interface SpyRoomStateDto {
    roomCode: string;
    status: RoomStatus;
    players: SpyPlayerDto[];
    messages: ChatMessageDto[];
    settings: SpyRoomGameSettingsDto;
    gameState: SpyGameStateDto | null;
    version: number;
}

export interface SpyRevealDto {
    playerId: string;
    isSpy: boolean;
}

// --- API Responses ---
export interface ApiResponse<T> {
    status: ResponseStatus;
    success: boolean;
    data?: T;
    error?: string;
}

export interface JoinRoomResponseDto {
    me: SpyPlayerDto;
    roomState: SpyRoomStateDto;
}

export interface CreateRoomResponseDto {
    me: SpyPlayerDto;
    roomState: SpyRoomStateDto;
}

// --- Events DTOs ---
export interface SpyPlayerJoinedEventDto {
    roomCode: string;
    player: SpyPlayerDto;
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

export interface ReturnToLobbyEventDto {
    roomCode: string;
}

export interface ChatMessageEventDto {
    roomCode: string;
    message: ChatMessageDto;
}



export interface SpyGameStartedEventDto {
    state: SpyRoomStateDto;
}

export interface SpyGameEndedEventDto {
    roomCode: string;
    winnerTeam: SpyGameTeam;
    reason: SpyGameEndReason;
    spiesReveal: SpyRevealDto[];
    reasonMessage: string | null;
}

export interface SpyGameRoundTimerStateChangedEventDto {
    roomCode: string;
    isRoundTimerStopped: boolean;
    roundTimerStartedAt: string | null;
    roundTimerWillStopAt: string | null;
    roundTimerPausedAt: string | null;
}

export interface SpyGameSettingsUpdatedEventDto {
    roomCode: string;
    settings: SpyRoomGameSettingsDto;
}

export interface PlayerVotedToStopTimerEventDto {
    roomCode: string;
    playerId: string;
    votesCount: number;
    requiredVotes: number;
}

export interface VotingResultEventDto {
    roomCode: string;
    isSuccess: boolean;
    currentGamePhase: SpyGamePhase;
    resultMessage: string | null;
    accusedId: string | null;
    isAccusedSpy: boolean | null;
    lastChanceEndsAt?: string | null;
}

export interface VotingStartedEventDto {
    roomCode: string;
    initiatorId: string;
    targetId: string | null;
    targetName: string | null;
    votingType: SpyVotingType;
    currentGamePhase: SpyGamePhase;
    endsAt: string;
}

export interface VoteCastEventDto {
    roomCode: string;
    voterId: string;
    targetVoteType?: TargetVoteType;
    againstPlayerId?: string;
}

// --- Event Map ---
export interface SpyGameEventMap {
    [SpyHubEvents.PlayerJoined]: SpyPlayerJoinedEventDto;
    [SpyHubEvents.PlayerLeft]: PlayerLeftEventDto;
    [SpyHubEvents.PlayerChangedName]: PlayerChangedNameEventDto;
    [SpyHubEvents.PlayerKicked]: PlayerKickedEventDto;
    [SpyHubEvents.PlayerReadyStatusChanged]: PlayerReadyStatusChangedEventDto;
    [SpyHubEvents.PlayerChangedAvatar]: PlayerChangedAvatarEventDto;
    [SpyHubEvents.HostChanged]: HostChangedEventDto;
    [SpyHubEvents.GameSettingsUpdated]: SpyGameSettingsUpdatedEventDto;
    [SpyHubEvents.GameStarted]: SpyGameStartedEventDto;
    [SpyHubEvents.ChatMessageReceived]: ChatMessageEventDto;
    [SpyHubEvents.TimerVoteUpdated]: PlayerVotedToStopTimerEventDto;
    [SpyHubEvents.ReturnedToLobby]: ReturnToLobbyEventDto;
    [SpyHubEvents.PlayerConnectionStatusChanged]: PlayerConnectionChangedEventDto;
    [SpyHubEvents.VotingStarted]: VotingStartedEventDto;
    [SpyHubEvents.VoteCast]: VoteCastEventDto;
    [SpyHubEvents.VotingResult]: VotingResultEventDto;
    [SpyHubEvents.GameEnded]: SpyGameEndedEventDto;
    [SpyHubEvents.RoundTimerStateChanged]: SpyGameRoundTimerStateChangedEventDto;
}