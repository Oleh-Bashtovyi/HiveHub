import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { SpyHubEvents } from '../const/spy-game-events';
import {
    type PlayerDto,
    type RoomGameSettingsDto,
    type GameStateDto,
    RoomState,
    type SpyRevealDto,
    type PlayerJoinedEventDto,
    type PlayerLeftEventDto,
    type PlayerChangedNameEventDto,
    type PlayerKickedEventDto,
    type PlayerReadyStatusChangedEventDto,
    type PlayerChangedAvatarEventDto,
    type HostChangedEventDto,
    type GameSettingsUpdatedEventDto,
    type GameStartedEventDto,
    type ChatMessageEventDto,
    type TimerStoppedEventDto,
    type SpiesRevealedEventDto,
} from '../models/spy-game';
import {SpySignalRService} from "../api/spy-signal-r-service.ts";

interface SpyGameContextType {
    isConnected: boolean;
    roomCode: string | null;
    me: PlayerDto | null;
    players: PlayerDto[];
    settings: RoomGameSettingsDto | null;
    roomState: RoomState;
    gameState: GameStateDto | null;
    gameResultSpies: SpyRevealDto[];

    // Actions
    connect: () => Promise<void>;
    createRoom: (playerName: string) => Promise<void>;
    joinRoom: (roomCode: string) => Promise<void>;
    leaveRoom: () => Promise<void>;
    sendMessage: (msg: string) => Promise<void>;
    toggleReady: () => Promise<void>;
    startGame: () => Promise<void>;
    voteStopTimer: () => Promise<void>;
    revealSpies: () => Promise<void>;
    returnToLobby: () => Promise<void>;

    // Setters (Settings)
    updateSettings: (newSettings: RoomGameSettingsDto) => Promise<void>;
    changeName: (newName: string) => Promise<void>;
    changeAvatar: (avatarId: string) => Promise<void>;
    kickPlayer: (playerId: string) => Promise<void>;
    changeHost: (playerId: string) => Promise<void>;
}

const SpyGameContext = createContext<SpyGameContextType | undefined>(undefined);

export const SpyGameProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [isConnected, setIsConnected] = useState(false);
    const [roomCode, setRoomCode] = useState<string | null>(null);
    const [me, setMe] = useState<PlayerDto | null>(null);
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [settings, setSettings] = useState<RoomGameSettingsDto | null>(null);
    const [roomState, setRoomState] = useState<RoomState>(RoomState.Lobby);
    const [gameState, setGameState] = useState<GameStateDto | null>(null);
    const [gameResultSpies, setGameResultSpies] = useState<SpyRevealDto[]>([]);

    // useRef для збереження singleton інстансу SignalR сервісу
    const signalRRef = useRef<SpySignalRService | null>(null);

    // Функція для отримання інстансу (Singleton в межах провайдера)
    const getService = useCallback(() => {
        if (!signalRRef.current) {
            const hubUrl = import.meta.env.VITE_HUB_URL;

            if (!hubUrl) {
                throw new Error('Missing HUB_URL');
            }
            console.log('HUB_URL', hubUrl);
            signalRRef.current = new SpySignalRService(hubUrl);
        }
        return signalRRef.current;
    }, []);

    // --- Connection Handling ---
    const connect = useCallback(async () => {
        if (isConnected) return;
        const signalRService = getService();
        await signalRService.start();
        setIsConnected(true);
    }, [isConnected, getService]);

    // --- Event Listeners Setup ---
    useEffect(() => {
        if (!isConnected) return;

        const svc = getService();
        const meId = me?.id;

        const handlePlayerJoined = (e: PlayerJoinedEventDto) => {
            setPlayers(prev => [...prev, e.player]);
        };

        const handlePlayerLeft = (e: PlayerLeftEventDto) => {
            setPlayers(prev => prev.filter(p => p.id !== e.playerId));
        };

        const handleNameChanged = (e: PlayerChangedNameEventDto) => {
            setPlayers(prev => prev.map(p =>
                p.id === e.playerId ? { ...p, name: e.newName } : p
            ));
            if (meId === e.playerId) {
                setMe(prev => prev ? { ...prev, name: e.newName } : null);
            }
        };

        const handlePlayerKicked = (e: PlayerKickedEventDto) => {
            if (meId === e.playerId) {
                alert("Вас було вигнано з кімнати.");
                svc.clearSessionInfo();
                setRoomCode(null);
                setMe(null);
                setPlayers([]);
                setSettings(null);
                setRoomState(RoomState.Lobby);
                setGameState(null);
            } else {
                setPlayers(prev => prev.filter(p => p.id !== e.playerId));
            }
        };

        const handleReadyChanged = (e: PlayerReadyStatusChangedEventDto) => {
            setPlayers(prev => prev.map(p =>
                p.id === e.playerId ? { ...p, isReady: e.isReady } : p
            ));
            if (meId === e.playerId) {
                setMe(prev => prev ? { ...prev, isReady: e.isReady } : null);
            }
        };

        const handleAvatarChanged = (e: PlayerChangedAvatarEventDto) => {
            setPlayers(prev => prev.map(p =>
                p.id === e.playerId ? { ...p, avatarId: e.newAvatarId } : p
            ));
            if (meId === e.playerId) {
                setMe(prev => prev ? { ...prev, avatarId: e.newAvatarId } : null);
            }
        };

        const handleHostChanged = (e: HostChangedEventDto) => {
            setPlayers(prev => prev.map(p => ({
                ...p,
                isHost: p.id === e.newHostId
            })));

            // Check if I became host
            setMe(prev => {
                if (!prev) return null;
                if (prev.id === e.newHostId) {
                    return { ...prev, isHost: true };
                } else if (prev.isHost) {
                    return { ...prev, isHost: false };
                }
                return prev;
            });
        };

        const handleSettingsUpdated = (e: GameSettingsUpdatedEventDto) => {
            setSettings(e.settings);
        };

        const handleGameStarted = (e: GameStartedEventDto) => {
            setRoomState(RoomState.InGame);
            setGameResultSpies([]);
            setGameState({
                currentSecretWord: e.secretWord,
                category: e.category,
                gameStartTime: new Date().toISOString(),
                gameEndTime: e.gameEndTime,
                isTimerStopped: false,
                timerStoppedAt: null,
                timerVotesCount: 0,
                recentMessages: []
            });
        };

        const handleChatMessage = (e: ChatMessageEventDto) => {
            setGameState(prev => {
                if (!prev) return null;
                return {
                    ...prev,
                    recentMessages: [...prev.recentMessages, e.message]
                };
            });
        };

        const handleTimerStopped = (e: TimerStoppedEventDto) => {
            setGameState(prev => {
                if (!prev) return null;
                const isStopped = e.votesCount >= e.requiredVotes;
                return {
                    ...prev,
                    timerVotesCount: e.votesCount,
                    isTimerStopped: isStopped,
                    timerStoppedAt: isStopped ? new Date().toISOString() : null
                };
            });
        };

        const handleSpiesRevealed = (e: SpiesRevealedEventDto) => {
            setRoomState(RoomState.Ended);
            setGameResultSpies(e.spies);
        };

        const handleReturnToLobby = () => {
            setRoomState(RoomState.Lobby);
            setGameState(null);
            setGameResultSpies([]);
            // Reset readiness
            setPlayers(prev => prev.map(p => ({ ...p, isReady: false })));
            setMe(prev => prev ? { ...prev, isReady: false } : null);
        };

        // Register all event handlers
        svc.on(SpyHubEvents.PlayerJoined, handlePlayerJoined);
        svc.on(SpyHubEvents.PlayerLeft, handlePlayerLeft);
        svc.on(SpyHubEvents.PlayerChangedName, handleNameChanged);
        svc.on(SpyHubEvents.PlayerKicked, handlePlayerKicked);
        svc.on(SpyHubEvents.PlayerReadyStatusChanged, handleReadyChanged);
        svc.on(SpyHubEvents.PlayerChangedAvatar, handleAvatarChanged);
        svc.on(SpyHubEvents.HostChanged, handleHostChanged);
        svc.on(SpyHubEvents.GameSettingsUpdated, handleSettingsUpdated);
        svc.on(SpyHubEvents.GameStarted, handleGameStarted);
        svc.on(SpyHubEvents.ChatMessageReceived, handleChatMessage);
        svc.on(SpyHubEvents.TimerVoteUpdated, handleTimerStopped);
        svc.on(SpyHubEvents.SpiesRevealed, handleSpiesRevealed);
        svc.on(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);

        return () => {
            // Cleanup - unsubscribe from all events
            svc.off(SpyHubEvents.PlayerJoined, handlePlayerJoined);
            svc.off(SpyHubEvents.PlayerLeft, handlePlayerLeft);
            svc.off(SpyHubEvents.PlayerChangedName, handleNameChanged);
            svc.off(SpyHubEvents.PlayerKicked, handlePlayerKicked);
            svc.off(SpyHubEvents.PlayerReadyStatusChanged, handleReadyChanged);
            svc.off(SpyHubEvents.PlayerChangedAvatar, handleAvatarChanged);
            svc.off(SpyHubEvents.HostChanged, handleHostChanged);
            svc.off(SpyHubEvents.GameSettingsUpdated, handleSettingsUpdated);
            svc.off(SpyHubEvents.GameStarted, handleGameStarted);
            svc.off(SpyHubEvents.ChatMessageReceived, handleChatMessage);
            svc.off(SpyHubEvents.TimerVoteUpdated, handleTimerStopped);
            svc.off(SpyHubEvents.SpiesRevealed, handleSpiesRevealed);
            svc.off(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
        };
    }, [isConnected, me?.id, getService]);

    // --- Actions ---
    const createRoom = useCallback(async (playerName: string) => {
        const signalRService = getService();
        const response = await signalRService.createRoom();

        // save for reconnect
        signalRService.setSessionInfo(response.roomCode, response.me.id);

        setRoomCode(response.roomCode);
        setMe(response.me);
        setSettings(response.settings);
        setPlayers([response.me]);
        setRoomState(RoomState.Lobby);

        // If the user provided a specific name but Hub created default:
        if (playerName && playerName !== response.me.name) {
            await signalRService.changeName(response.roomCode, playerName);
        }
    }, [getService]);

    const joinRoom = useCallback(async (code: string) => {
        const svc = getService();
        const response = await svc.joinRoom(code);

        svc.setSessionInfo(response.roomCode, response.me.id);

        setRoomCode(response.roomCode);
        setMe(response.me);
        setSettings(response.settings);
        setPlayers(response.players);
        setRoomState(RoomState.Lobby);
    }, [getService]);

    const leaveRoom = useCallback(async () => {
        if (!roomCode) return;

        const svc = getService();
        await svc.leaveRoom(roomCode);

        svc.clearSessionInfo();

        setRoomCode(null);
        setMe(null);
        setPlayers([]);
        setSettings(null);
        setRoomState(RoomState.Lobby);
        setGameState(null);
        setGameResultSpies([]);
    }, [roomCode, getService]);

    const updateSettings = useCallback(async (newSettings: RoomGameSettingsDto) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.updateSettings(roomCode, newSettings);
    }, [roomCode, getService]);

    const changeName = useCallback(async (newName: string) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.changeName(roomCode, newName);
    }, [roomCode, getService]);

    const changeAvatar = useCallback(async (avatarId: string) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.changeAvatar(roomCode, avatarId);
    }, [roomCode, getService]);

    const toggleReady = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        await svc.toggleReady(roomCode);
    }, [roomCode, getService]);

    const kickPlayer = useCallback(async (targetId: string) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.kickPlayer(roomCode, targetId);
    }, [roomCode, getService]);

    const changeHost = useCallback(async (targetId: string) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.changeHost(roomCode, targetId);
    }, [roomCode, getService]);

    const startGame = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        await svc.startGame(roomCode);
    }, [roomCode, getService]);

    const sendMessage = useCallback(async (msg: string) => {
        if (!roomCode) return;
        const svc = getService();
        await svc.sendMessage(roomCode, msg);
    }, [roomCode, getService]);

    const voteStopTimer = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        await svc.voteStopTimer(roomCode);
    }, [roomCode, getService]);

    const revealSpies = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        await svc.revealSpies(roomCode);
    }, [roomCode, getService]);

    const returnToLobby = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        await svc.returnToLobby(roomCode);
    }, [roomCode, getService]);

    const value: SpyGameContextType = {
        isConnected,
        roomCode,
        me,
        players,
        settings,
        roomState,
        gameState,
        gameResultSpies,

        connect,
        createRoom,
        joinRoom,
        leaveRoom,
        sendMessage,
        toggleReady,
        startGame,
        voteStopTimer,
        revealSpies,
        returnToLobby,

        updateSettings,
        changeName,
        changeAvatar,
        kickPlayer,
        changeHost
    };

    return (
        <SpyGameContext.Provider value={value}>
            {children}
        </SpyGameContext.Provider>
    );
};

// eslint-disable-next-line react-refresh/only-export-components
export function useSpyGame() {
    const context = useContext(SpyGameContext);
    if (!context) {
        throw new Error('useSpyGame must be used within a SpyGameProvider');
    }
    return context;
}