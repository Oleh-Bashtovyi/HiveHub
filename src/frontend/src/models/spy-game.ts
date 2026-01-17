import { SpyHubEvents } from "../const/spy-game-events.ts";
import type {
    ChatMessageDto,
    ChatMessageEventDto,
    HostChangedEventDto,
    PlayerChangedAvatarEventDto,
    PlayerChangedNameEventDto,
    PlayerConnectionChangedEventDto,
    PlayerDto,
    PlayerKickedEventDto,
    PlayerLeftEventDto,
    PlayerReadyStatusChangedEventDto,
    ReturnToLobbyEventDto,
    RoomStatus,
    TargetVoteType
} from "./shared.ts";

export const SpyVotingType = {
    Accusation: 'Accusation',
    Final: 'Final',
} as const;
export type SpyVotingType = (typeof SpyVotingType)[keyof typeof SpyVotingType];

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
    RoundTimeExpired: 'RoundTimeExpired',
    CivilianKicked: 'CivilianKicked',
    SpyGuessedWord: 'SpyGuessedWord',
    SpyWrongGuess: 'SpyWrongGuess',
    FinalVoteFailed: 'FinalVoteFailed',
    AllSpiesEliminated: 'AllSpiesEliminated',
    SpyLastChanceFailed: 'SpyLastChanceFailed',
    ParanoiaSacrifice: 'ParanoiaSacrifice',
    ParanoiaSurvived: 'ParanoiaSurvived',
    InsufficientPlayers: 'InsufficientPlayers'
} as const;
export type SpyGameEndReason = (typeof SpyGameEndReason)[keyof typeof SpyGameEndReason];

export const TimerStatus = {
    Stopped: 'Stopped',
    Running: 'Running',
    Paused: 'Paused',
} as const;
export type TimerStatus = (typeof TimerStatus)[keyof typeof TimerStatus];

// --- DTOs ---
export interface SpyPlayerDto extends PlayerDto {
    isSpy?: boolean | null;
    isVotedToStopTimer?: boolean | null;
    hasUsedAccusation?: boolean | null;
    isDead?: boolean | null;
}

export interface SpyGameRulesDto {
    timerMinutes: number;
    minSpiesCount: number;
    maxSpiesCount: number;
    maxPlayersCount: number;
    isSpiesKnowEachOther: boolean;
    isShowCategoryToSpy: boolean;
    isSpiesPlayAsTeam: boolean;
}

export interface SpyGameWordPacksDto {
    customCategories: WordsCategoryDto[];
}

export interface WordsCategoryDto {
    name: string;
    words: string[];
}

export interface SpyRoomStateDto {
    roomCode: string;
    status: RoomStatus;
    players: SpyPlayerDto[];
    messages: ChatMessageDto[];
    rules: SpyGameRulesDto;
    wordPacks: SpyGameWordPacksDto;
    gameState: SpyGameStateDto | null;
    version: number;
}

export interface SpyGameStateDto {
    currentSecretWord: string | null;
    currentCategory: string | null;

    caughtSpyId: string | null;
    caughtSpyName: string | null;

    phase: SpyGamePhase;
    activeVoting: VotingStateDto | null;

    // Round timer
    roundStartedAt: string;
    roundTimerStatus: TimerStatus;
    roundRemainingSeconds: number;
    spyLastChanceEndsAt: string | null;

    // Timer stop requirements
    playersVotedToStopTimer: number;
    votesRequiredToStopTimer: number;

    // Final Results
    spiecsReveal: SpyRevealDto[] | null;
    roundEndReason: SpyGameEndReason | null;
}

export interface SpyRevealDto {
    playerId: string;
    playerName: string;
    isSpy: boolean;
    isDead: boolean;
}

export interface VotingStateDto {
    type: SpyVotingType;
    accusedPlayerId: string | null;
    accusedPlayerName: string | null;
    targetVoting: Record<string, TargetVoteType> | null;
    againstVoting: Record<string, string | null> | null;
    votesRequired: number | null;
    startedAt: string;
    endsAt: string;
}

// --- API Responses ---
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

export interface SpyGameRulesUpdatedEventDto {
    roomCode: string;
    rules: SpyGameRulesDto;
}

export interface SpyGameWordPacksUpdatedEventDto {
    roomCode: string;
    packs: SpyGameWordPacksDto;
}

export interface SpyGameEndedEventDto {
    roomCode: string;
    winnerTeam: SpyGameTeam;
    reason: SpyGameEndReason;
    spiesReveal: SpyRevealDto[];
    reasonMessage: string | null;
}

export interface SpyGameStartedEventDto {
    state: SpyRoomStateDto;
}

export interface SpyMadeGuessEventDto {
    roomCode: string;
    playerId: string;
    word: string;
    isGuessCorrect: boolean;
    isSpyDead: boolean;
}

export interface SpyGameRoundTimerStateChangedEventDto {
    roomCode: string;
    timerStatus: TimerStatus;
    remainingSeconds: number;
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
    lastChanceEndsAt: string | null;
}

export interface VotingStartedEventDto {
    roomCode: string;
    initiatorId: string;
    targetId: string | null;
    targetName: string | null;
    votingType: SpyVotingType;
    currentGamePhase: SpyGamePhase;
    startedAt: string;
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
    [SpyHubEvents.RulesChanged]: SpyGameRulesUpdatedEventDto;
    [SpyHubEvents.WordPacksChanged]: SpyGameWordPacksUpdatedEventDto;
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
    [SpyHubEvents.SpyMadeGuess]: SpyMadeGuessEventDto;
}