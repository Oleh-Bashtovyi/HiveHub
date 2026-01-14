// backend methods
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
    Vote: "Vote",
    MakeGuess: "MakeGuess"
} as const;

// backend events
export const SpyHubEvents = {
    PlayerJoined: "PlayerJoined",
    PlayerLeft: "PlayerLeft",
    PlayerChangedName: "PlayerChangedName",
    PlayerKicked: "PlayerKicked",
    PlayerReadyStatusChanged: "PlayerReadyStatusChanged",
    PlayerChangedAvatar: "PlayerChangedAvatar",
    HostChanged: "HostChanged",
    GameSettingsUpdated: "GameSettingsUpdated",
    GameStarted: "GameStarted",
    ChatMessageReceived: "ChatMessageReceived",
    TimerVoteUpdated: "TimerVoteUpdated",
    ReturnedToLobby: "ReturnedToLobby",
    PlayerConnectionStatusChanged: 'PlayerConnectionStatusChanged',
    VotingStarted: "VotingStarted",
    VoteCast: "VoteCast",
    VotingResult: "VotingResult",
    GameEnded: "GameEnded",
} as const;

export type SpyHubMethod = typeof SpyHubMethods[keyof typeof SpyHubMethods];
export type SpyHubEvent = typeof SpyHubEvents[keyof typeof SpyHubEvents];