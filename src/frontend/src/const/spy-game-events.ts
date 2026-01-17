export const SpyHubMethods = {
    CreateRoom: "CreateRoom",
    JoinRoom: "JoinRoom",
    Reconnect: "Reconnect",
    LeaveRoom: "LeaveRoom",
    ChangeName: "ChangeName",
    ChangeAvatar: "ChangeAvatar",
    ToggleReady: "ToggleReady",
    ChangeHost: "ChangeHost",
    KickPlayer: "KickPlayer",
    UpdateSettings: "UpdateSettings",
    ReturnToLobby: "ReturnToLobby",
    StartGame: "StartGame",
    SendMessage: "SendMessage",
    VoteStopTimer: "VoteStopTimer",
    StartAccusation: "StartAccusation",
    UpdateRules: "UpdateRules",
    UpdateWordPacks: "UpdateWordPacks",
    Vote: "Vote",
    MakeGuess: "MakeGuess"
} as const;

export const SpyHubEvents = {
    PlayerJoined: "PlayerJoined",
    PlayerLeft: "PlayerLeft",
    PlayerChangedName: "PlayerChangedName",
    PlayerKicked: "PlayerKicked",
    PlayerReadyStatusChanged: "PlayerReadyStatusChanged",
    PlayerChangedAvatar: "PlayerChangedAvatar",
    HostChanged: "HostChanged",
    RulesChanged: "RulesChanged",
    WordPacksChanged: "WordPacksChanged",
    GameStarted: "GameStarted",
    ChatMessageReceived: "ChatMessageReceived",
    ReturnedToLobby: "ReturnedToLobby",
    GameEnded: "GameEnded",
    PlayerConnectionStatusChanged: "PlayerConnectionStatusChanged",
    VotingStarted: "VotingStarted",
    VoteCast: "VoteCast",
    VotingCompleted: "VotingCompleted",
    TimerVoteUpdated: "TimerVoteUpdated",
    RoundTimerStateChanged: "RoundTimerStateChanged",
    GamePhaseChanged: "GamePhaseChanged",
    PlayerEliminated: "PlayerEliminated",
    SpyRevealed: "SpyRevealed",
    SpyLastChanceStarted: "SpyLastChanceStarted",
    SpyGuessAttempted: "SpyGuessAttempted"
} as const;

export type SpyHubMethod = typeof SpyHubMethods[keyof typeof SpyHubMethods];
export type SpyHubEvent = typeof SpyHubEvents[keyof typeof SpyHubEvents];