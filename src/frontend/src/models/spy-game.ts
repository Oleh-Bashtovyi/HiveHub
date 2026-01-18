import { SpyHubEvents } from "../const/spy-game-events";
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
} from "./shared";

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
    Expired: 'Expired'
} as const;
export type TimerStatus = (typeof TimerStatus)[keyof typeof TimerStatus];

export const TimerChangeReason = {
    Started: 'Started',
    Paused: 'Paused',
    Resumed: 'Resumed',
    Stopped: 'Stopped',
    Expired: 'Expired'
} as const;
export type TimerChangeReason = (typeof TimerChangeReason)[keyof typeof TimerChangeReason];

export const EliminationReason = {
    VotedOut: 'VotedOut',
    FailedGuess: 'FailedGuess'
} as const;
export type EliminationReason = (typeof EliminationReason)[keyof typeof EliminationReason];

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
    roundStartedAt: string;
    roundTimerStatus: TimerStatus;
    roundRemainingSeconds: number;
    spyLastChanceEndsAt: string | null;
    playersVotedToStopTimer: number;
    votesRequiredToStopTimer: number;
    spiesReveal: SpyRevealDto[] | null;
    roundEndReason: SpyGameEndReason | null;
    winnerTeam: SpyGameTeam | null;
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

export interface JoinRoomResponseDto {
    me: SpyPlayerDto;
    roomState: SpyRoomStateDto;
}

export interface CreateRoomResponseDto {
    me: SpyPlayerDto;
    roomState: SpyRoomStateDto;
}

export interface SpyGameStartedEventDto {
    state: SpyRoomStateDto;
}

export interface SpyGameEndedEventDto {
    roomCode: string;
    winnerTeam: SpyGameTeam;
    reason: SpyGameEndReason;
    spiesReveal: SpyRevealDto[];
    category: string;
    secretWord: string;
    reasonMessage: string | null;
}

export interface SpyMadeGuessEventDto {
    roomCode: string;
    playerId: string;
    word: string;
    isGuessCorrect: boolean;
    isSpyDead: boolean;
}

export interface SpyGameRulesUpdatedEventDto {
    roomCode: string;
    rules: SpyGameRulesDto;
}

export interface SpyGameWordPacksUpdatedEventDto {
    roomCode: string;
    packs: SpyGameWordPacksDto;
}

export interface GamePhaseChangedEventDto {
    roomCode: string;
    newPhase: SpyGamePhase;
    previousPhase: SpyGamePhase;
}

export interface SpyGameRoundTimerStateChangedEventDto {
    roomCode: string;
    status: TimerStatus;
    remainingSeconds: number;
    reason: TimerChangeReason;
}

export interface PlayerVotedToStopTimerEventDto {
    roomCode: string;
    playerId: string;
    currentVotes: number;
    requiredVotes: number;
}

export interface VotingStartedEventDto {
    roomCode: string;
    initiatorId: string;
    targetId: string | null;
    targetName: string | null;
    votingType: SpyVotingType;
    endsAt: string;
}

export interface VoteCastEventDto {
    roomCode: string;
    voterId: string;
    voterName: string;
    targetVoteType: TargetVoteType | null;
    againstPlayerId: string | null;
    currentVotes: number;
    requiredVotes: number;
}

export interface VotingCompletedEventDto {
    roomCode: string;
    isSuccess: boolean;
    votingType: SpyVotingType;
    resultMessage: string;
}

export interface PlayerEliminatedEventDto {
    roomCode: string;
    playerId: string;
    playerName: string;
    wasSpy: boolean;
    reason: EliminationReason;
}

export interface SpyRevealedEventDto {
    roomCode: string;
    spyId: string;
    spyName: string;
}

export interface SpyLastChanceStartedEventDto {
    roomCode: string;
    spyId: string;
    spyName: string;
    endsAt: string;
}

export interface SpyGuessAttemptedEventDto {
    roomCode: string;
    spyId: string;
    guessedWord: string;
    isCorrect: boolean;
}

export interface SpyPlayerJoinedEventDto {
    roomCode: string;
    player: SpyPlayerDto;
}

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
    [SpyHubEvents.ReturnedToLobby]: ReturnToLobbyEventDto;
    [SpyHubEvents.GameEnded]: SpyGameEndedEventDto;
    [SpyHubEvents.PlayerConnectionStatusChanged]: PlayerConnectionChangedEventDto;
    [SpyHubEvents.VotingStarted]: VotingStartedEventDto;
    [SpyHubEvents.VoteCast]: VoteCastEventDto;
    [SpyHubEvents.VotingCompleted]: VotingCompletedEventDto;
    [SpyHubEvents.TimerVoteUpdated]: PlayerVotedToStopTimerEventDto;
    [SpyHubEvents.RoundTimerStateChanged]: SpyGameRoundTimerStateChangedEventDto;
    [SpyHubEvents.GamePhaseChanged]: GamePhaseChangedEventDto;
    [SpyHubEvents.PlayerEliminated]: PlayerEliminatedEventDto;
    [SpyHubEvents.SpyRevealed]: SpyRevealedEventDto;
    [SpyHubEvents.SpyLastChanceStarted]: SpyLastChanceStartedEventDto;
    [SpyHubEvents.SpyGuessAttempted]: SpyGuessAttemptedEventDto;
}