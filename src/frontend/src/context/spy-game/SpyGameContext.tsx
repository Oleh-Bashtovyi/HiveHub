import React, { createContext, useContext, useState, useCallback, useRef } from 'react';
import {
    RoomStatus,
    type SpyPlayerDto,
    type SpyRoomGameSettingsDto,
    type GameStateDto,
    type ChatMessageDto,
    type SpyGameEndReason,
    type SpyGameTeam,
} from '../../models/spy-game';
import { SpySignalRService } from "../../api/spy-signal-r-service";
import { useSpyGameEvents } from './useSpyGameEvents';
import { useSpyGameSession, type StateSetters } from './useSpyGameSession';

interface SpyGameContextType {
    isConnected: boolean;
    isConnecting: boolean;
    isReconnecting: boolean;
    isInitializing: boolean;
    roomCode: string | null;
    me: SpyPlayerDto | null;
    players: SpyPlayerDto[];
    settings: SpyRoomGameSettingsDto | null;
    roomState: RoomStatus;
    gameState: GameStateDto | null;
    messages: ChatMessageDto[];

    // Game End Info
    winnerTeam: SpyGameTeam | null;
    gameEndReason: SpyGameEndReason | null;
    gameEndMessage: string | null;

    // Actions
    createRoom: (playerName: string) => Promise<void>;
    joinRoom: (roomCode: string) => Promise<void>;
    leaveRoom: () => Promise<void>;
    sendMessage: (msg: string) => Promise<void>;
    toggleReady: () => Promise<void>;
    startGame: () => Promise<void>;
    voteStopTimer: () => Promise<void>;
    returnToLobby: () => Promise<void>;
    updateSettings: (newSettings: SpyRoomGameSettingsDto) => Promise<void>;
    changeName: (newName: string) => Promise<void>;
    changeAvatar: (avatarId: string) => Promise<void>;
    kickPlayer: (playerId: string) => Promise<void>;
    changeHost: (playerId: string) => Promise<void>;
    startAccusation: (targetPlayerId: string) => Promise<void>;
    vote: (targetPlayerId: string, voteType: string | null) => Promise<void>;
    makeGuess: (word: string) => Promise<void>;
}

const SpyGameContext = createContext<SpyGameContextType | undefined>(undefined);

export const SpyGameProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [isConnected, setIsConnected] = useState(false);
    const [isConnecting, setIsConnecting] = useState(false);
    const [isReconnecting, setIsReconnecting] = useState(false);
    const [isInitializing, setIsInitializing] = useState(true);

    const [roomCode, setRoomCode] = useState<string | null>(null);
    const [me, setMe] = useState<SpyPlayerDto | null>(null);
    const [players, setPlayers] = useState<SpyPlayerDto[]>([]);
    const [settings, setSettings] = useState<SpyRoomGameSettingsDto | null>(null);
    const [roomState, setRoomState] = useState<RoomStatus>(RoomStatus.Lobby);
    const [gameState, setGameState] = useState<GameStateDto | null>(null);
    const [messages, setMessages] = useState<ChatMessageDto[]>([]);

    const [winnerTeam, setWinnerTeam] = useState<SpyGameTeam | null>(null);
    const [gameEndReason, setGameEndReason] = useState<SpyGameEndReason | null>(null);
    const [gameEndMessage, setGameEndMessage] = useState<string | null>(null);

    const signalRRef = useRef<SpySignalRService | null>(null);

    const getService = useCallback(() => {
        if (!signalRRef.current) {
            const hubUrl = import.meta.env.VITE_HUB_URL;
            if (!hubUrl) throw new Error('Missing HUB_URL');
            signalRRef.current = new SpySignalRService(hubUrl);
        }
        return signalRRef.current;
    }, []);

    const stateSetters: StateSetters = {
        setRoomCode,
        setRoomState,
        setSettings,
        setPlayers,
        setMessages,
        setGameState,
        setMe,
        setWinnerTeam,
        setGameEndReason,
        setGameEndMessage,
        setIsReconnecting,
    };

    // Session management (connect, reconnect, save/clear session)
    const {
        reconnect,
        resetState,
        saveSession,
        clearSession,
    } = useSpyGameSession({
        getService,
        stateSetters,
        isConnected,
        isConnecting,
        isReconnecting,
        setIsConnecting,
        setIsConnected,
        setIsInitializing,
    });

    // Event listeners
    useSpyGameEvents({
        isConnected,
        getService,
        me,
        stateSetters,
        clearSession,
        resetState,
        reconnect,
    });

    // Actions
    const createRoom = useCallback(async (playerName: string) => {
        const svc = getService();
        const response = await svc.createRoom();

        saveSession(response.roomState.roomCode, response.me.id);

        setRoomCode(response.roomState.roomCode);
        setMe(response.me);
        setSettings(response.roomState.settings);
        setPlayers([response.me]);
        setRoomState(RoomStatus.Lobby);

        if (playerName && playerName !== response.me.name) {
            await svc.changeName(response.roomState.roomCode, playerName);
        }
    }, [getService, saveSession]);

    const joinRoom = useCallback(async (code: string) => {
        const svc = getService();
        const response = await svc.joinRoom(code);

        saveSession(response.roomState.roomCode, response.me.id);

        setRoomCode(response.roomState.roomCode);
        setMe(response.me);
        setSettings(response.roomState.settings);
        setPlayers(response.roomState.players);
        setRoomState(RoomStatus.Lobby);
    }, [getService, saveSession]);

    const leaveRoom = useCallback(async () => {
        if (!roomCode) return;
        const svc = getService();
        clearSession();
        try {
            await svc.leaveRoom(roomCode);
        } catch (error) {
            console.error("Error leaving room:", error);
        }
        resetState();
    }, [roomCode, getService, clearSession, resetState]);

    const updateSettings = useCallback(async (newSettings: SpyRoomGameSettingsDto) => {
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
        setPlayers(prev => prev.map(p => p.id === me?.id ? {...p, isVotedToStopTimer: true} : p));
        setMe(prev => prev ? {...prev, isVotedToStopTimer: true} : null);
    }, [roomCode, getService, me?.id]);

    const returnToLobby = useCallback(async () => {
        if (roomCode) await getService().returnToLobby(roomCode);
    }, [roomCode, getService]);

    const startAccusation = useCallback(async (targetId: string) => {
        if (roomCode) await getService().startAccusation(roomCode, targetId);
    }, [roomCode, getService]);

    const vote = useCallback(async (targetId: string, voteType: string | null) => {
        console.log("trying to vote");
        console.log(voteType);
        console.log(targetId);
        if (roomCode) await getService().vote(roomCode, targetId, voteType);
    }, [roomCode, getService]);

    const makeGuess = useCallback(async (word: string) => {
        if (roomCode) await getService().makeGuess(roomCode, word);
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
        messages,
        winnerTeam,
        gameEndReason,
        gameEndMessage,
        createRoom,
        joinRoom,
        leaveRoom,
        sendMessage,
        toggleReady,
        startGame,
        voteStopTimer,
        returnToLobby,
        updateSettings,
        changeName,
        changeAvatar,
        kickPlayer,
        changeHost,
        startAccusation,
        vote,
        makeGuess
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