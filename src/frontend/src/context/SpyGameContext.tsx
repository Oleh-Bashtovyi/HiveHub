import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import { SpyHubEvents } from '../const/spy-game-events';
import {
    RoomState,
    type PlayerDto,
    type RoomGameSettingsDto,
    type GameStateDto,
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
    type PlayerConnectionChangedEventDto
} from '../models/spy-game';
import { SpySignalRService } from "../api/spy-signal-r-service";

const SESSION_KEYS = {
    ROOM: 'hive_room',
    PLAYER: 'hive_player'
};

interface SpyGameContextType {
    isConnected: boolean;
    isConnecting: boolean;
    isReconnecting: boolean;
    isInitializing: boolean;
    roomCode: string | null;
    me: PlayerDto | null;
    players: PlayerDto[];
    settings: RoomGameSettingsDto | null;
    roomState: RoomState;
    gameState: GameStateDto | null;
    gameResultSpies: SpyRevealDto[];
    createRoom: (playerName: string) => Promise<void>;
    joinRoom: (roomCode: string) => Promise<void>;
    leaveRoom: () => Promise<void>;
    sendMessage: (msg: string) => Promise<void>;
    toggleReady: () => Promise<void>;
    startGame: () => Promise<void>;
    voteStopTimer: () => Promise<void>;
    revealSpies: () => Promise<void>;
    returnToLobby: () => Promise<void>;
    updateSettings: (newSettings: RoomGameSettingsDto) => Promise<void>;
    changeName: (newName: string) => Promise<void>;
    changeAvatar: (avatarId: string) => Promise<void>;
    kickPlayer: (playerId: string) => Promise<void>;
    changeHost: (playerId: string) => Promise<void>;
}

const SpyGameContext = createContext<SpyGameContextType | undefined>(undefined);

export const SpyGameProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [isConnected, setIsConnected] = useState(false);
    const [isConnecting, setIsConnecting] = useState(false);
    const [isReconnecting, setIsReconnecting] = useState(false);
    const [isInitializing, setIsInitializing] = useState(true);
    const [roomCode, setRoomCode] = useState<string | null>(null);
    const [me, setMe] = useState<PlayerDto | null>(null);
    const [players, setPlayers] = useState<PlayerDto[]>([]);
    const [settings, setSettings] = useState<RoomGameSettingsDto | null>(null);
    const [roomState, setRoomState] = useState<RoomState>(RoomState.Lobby);
    const [gameState, setGameState] = useState<GameStateDto | null>(null);
    const [gameResultSpies, setGameResultSpies] = useState<SpyRevealDto[]>([]);

    const signalRRef = useRef<SpySignalRService | null>(null);
    const isInitialized = useRef(false);

    const getService = useCallback(() => {
        if (!signalRRef.current) {
            const hubUrl = import.meta.env.VITE_HUB_URL;
            if (!hubUrl) throw new Error('Missing HUB_URL');
            signalRRef.current = new SpySignalRService(hubUrl);
        }
        return signalRRef.current;
    }, []);

    const saveSession = useCallback((code: string, pid: string) => {
        sessionStorage.setItem(SESSION_KEYS.ROOM, code);
        sessionStorage.setItem(SESSION_KEYS.PLAYER, pid);
    }, []);

    const clearSession = useCallback(() => {
        sessionStorage.removeItem(SESSION_KEYS.ROOM);
        sessionStorage.removeItem(SESSION_KEYS.PLAYER);
    }, []);

    const resetState = useCallback(() => {
        setRoomCode(null);
        setMe(null);
        setPlayers([]);
        setSettings(null);
        setRoomState(RoomState.Lobby);
        setGameState(null);
        setGameResultSpies([]);
    }, []);

    const reconnect = useCallback(async (svc: SpySignalRService) => {
        if (isReconnecting) {
            return;
        }

        const savedRoom = sessionStorage.getItem(SESSION_KEYS.ROOM);
        const savedPlayer = sessionStorage.getItem(SESSION_KEYS.PLAYER);

        if (savedRoom && savedPlayer) {
            console.log(`[Context] Attempting logical reconnect to ${savedRoom}`);
            try {
                setIsReconnecting(true);
                const fullState = await svc.reconnect(savedRoom, savedPlayer);

                // Restore State
                setRoomCode(fullState.roomCode);
                setRoomState(fullState.state);
                setSettings(fullState.settings);
                setPlayers(fullState.players);
                setGameState(fullState.gameState);

                const myPlayer = fullState.players.find(p => p.id === savedPlayer);
                if (myPlayer) setMe(myPlayer);

                saveSession(fullState.roomCode, savedPlayer);
                console.log("[Context] Reconnect success");
            } catch (e) {
                console.warn("[Context] Reconnect failed. Clearing session.", e);
                clearSession();
                resetState();
            } finally {
                setIsReconnecting(false);
            }
        }
    }, [isReconnecting, clearSession, resetState, saveSession]);

    const connect = useCallback(async (svc: SpySignalRService) => {
        if (isConnected || isConnecting) {
            return;
        }

        setIsConnecting(true);
        svc.onTransportReconnected(async (newId) => {
            console.log(`[Context] Transport recovered (ID: ${newId}). Triggering logical reconnect...`);
            await reconnect(svc);
        });
        
        await svc.start();
        setIsConnected(true);
        setIsConnecting(false);
    }, [isConnected, isConnecting, reconnect]);
    
    useEffect(() => {
        if (isInitialized.current) return;

        isInitialized.current = true;

        const bootstrap = async () => {
            const svc = getService();

            try {
                await connect(svc);
                await reconnect(svc);
            } catch (error) {
                console.error("[Context] Initialization failed", error);
                setIsConnecting(false);
            } finally {
                setIsInitializing(false);
            }
        };

        bootstrap();
    }, [connect, getService, reconnect]);
    
    // --- Cleanup on Unmount ---
/*    useEffect(() => {
        return () => {
            console.log("SpyGameProvider unmounting.");
            const svc = signalRRef.current;
            if (svc) {
                svc.stop();
                signalRRef.current = null;
            }
        };
    }, []);*/

    // --- Event Listeners ---
    useEffect(() => {
        if (!isConnected) return;

        const svc = getService();
        const meId = me?.id;

        const handlePlayerJoined = (e: PlayerJoinedEventDto) => {
            setPlayers(prev => {
                if (prev.some(p => p.id === e.player.id)) return prev;
                return [...prev, e.player];
            });
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
                clearSession();
                resetState();
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

            setMe(prev => {
                if (!prev) return null;
                return { ...prev, isHost: prev.id === e.newHostId };
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
            // Reset votes visualization for new game
            setPlayers(prev => prev.map(p => ({...p, isVotedToStopTimer: false})));
        };

        const handleChatMessage = (e: ChatMessageEventDto) => {
            setGameState(prev => {
                if (!prev) return null;
                return { ...prev, recentMessages: [...prev.recentMessages, e.message] };
            });
        };

        const handleConnectionChanged = (e: PlayerConnectionChangedEventDto) => {
            setPlayers(prev => prev.map(p =>
                p.id === e.playerId ? { ...p, isConnected: e.isConnected } : p
            ));
        };

        const handleTimerStopped = (e: TimerStoppedEventDto) => {
            setPlayers(prev => prev.map(p =>
                p.id === e.playerId ? { ...p, isVotedToStopTimer: true } : p
            ));

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
            setPlayers(prev => prev.map(p => ({ ...p, isReady: false, isVotedToStopTimer: false, isSpy: undefined })));
            setMe(prev => prev ? { ...prev, isReady: false, isVotedToStopTimer: false, isSpy: undefined } : null);
        };

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
        svc.on(SpyHubEvents.PlayerConnectionStatusChanged, handleConnectionChanged);
        svc.on(SpyHubEvents.TimerVoteUpdated, handleTimerStopped);
        svc.on(SpyHubEvents.SpiesRevealed, handleSpiesRevealed);
        svc.on(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);

        return () => {
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
            svc.off(SpyHubEvents.PlayerConnectionStatusChanged, handleConnectionChanged);
            svc.off(SpyHubEvents.TimerVoteUpdated, handleTimerStopped);
            svc.off(SpyHubEvents.SpiesRevealed, handleSpiesRevealed);
            svc.off(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
        };
    }, [isConnected, me?.id, getService, clearSession, resetState]);
    
    const createRoom = useCallback(async (playerName: string) => {
        const svc = getService();
        const response = await svc.createRoom();

        // 1. Save Session
        saveSession(response.roomCode, response.me.id);

        // 2. Update State
        setRoomCode(response.roomCode);
        setMe(response.me);
        setSettings(response.settings);
        setPlayers([response.me]);
        setRoomState(RoomState.Lobby);

        if (playerName && playerName !== response.me.name) {
            await svc.changeName(response.roomCode, playerName);
        }
    }, [getService, saveSession]);

    const joinRoom = useCallback(async (code: string) => {
        const svc = getService();
        const response = await svc.joinRoom(code);

        // 1. Save Session
        saveSession(response.roomCode, response.me.id);

        // 2. Update State
        setRoomCode(response.roomCode);
        setMe(response.me);
        setSettings(response.settings);
        setPlayers(response.players);
        setRoomState(RoomState.Lobby);
    }, [getService, saveSession]);

    const leaveRoom = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();

        // 1. Clear Session
        clearSession();

        try {
            await svc.leaveRoom(roomCode);
        } catch (e) {
            console.error("Error leaving room:", e);
        }

        // 2. Reset State
        resetState();
    }, [roomCode, getService, clearSession, resetState]);

    // --- Simple Pass-through Actions ---
    const updateSettings = useCallback(async (newSettings: RoomGameSettingsDto) => {
        if (roomCode) await getService().updateSettings(roomCode, newSettings);
    }, [roomCode, getService]);

    const changeName = useCallback(async (newName: string) => {
        if (roomCode) await getService().changeName(roomCode, newName);
    }, [roomCode, getService]);

    const changeAvatar = useCallback(async (avatarId: string) => {
        if (roomCode) await getService().changeAvatar(roomCode, avatarId);
    }, [roomCode, getService]);

    const toggleReady = useCallback(async () => {
        if (roomCode) await getService().toggleReady(roomCode);
    }, [roomCode, getService]);

    const kickPlayer = useCallback(async (targetId: string) => {
        if (roomCode) await getService().kickPlayer(roomCode, targetId);
    }, [roomCode, getService]);

    const changeHost = useCallback(async (targetId: string) => {
        if (roomCode) await getService().changeHost(roomCode, targetId);
    }, [roomCode, getService]);

    const startGame = useCallback(async () => {
        if (roomCode) await getService().startGame(roomCode);
    }, [roomCode, getService]);

    const sendMessage = useCallback(async (msg: string) => {
        if (roomCode) await getService().sendMessage(roomCode, msg);
    }, [roomCode, getService]);

    const voteStopTimer = useCallback(async () => {
        if (!roomCode) return;
        await getService().voteStopTimer(roomCode);
        // Optimistic update
        setPlayers(prev => prev.map(p => p.id === me?.id ? {...p, isVotedToStopTimer: true} : p));
        setMe(prev => prev ? {...prev, isVotedToStopTimer: true} : null);
    }, [roomCode, getService, me?.id]);

    const revealSpies = useCallback(async () => {
        if (roomCode) await getService().revealSpies(roomCode);
    }, [roomCode, getService]);

    const returnToLobby = useCallback(async () => {
        if (roomCode) await getService().returnToLobby(roomCode);
    }, [roomCode, getService]);

    const value: SpyGameContextType = {
        isConnected,
        isConnecting,
        isReconnecting,
        isInitializing,
        roomCode,
        me,
        players,
        settings,
        roomState,
        gameState,
        gameResultSpies,
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