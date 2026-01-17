import React, { useCallback, useEffect, useRef } from 'react';
import {
    type SpyPlayerDto,
    type SpyGameRulesDto,
    type SpyGameWordPacksDto,
    type SpyGameStateDto,
    type SpyGameEndReason,
    type SpyGameTeam, type SpyRevealDto,
} from '../../models/spy-game';
import { SpySignalRService } from "../../api/spy-signal-r-service";
import {type ChatMessageDto, RoomStatus} from "../../models/shared.ts";

const SESSION_KEYS = {
    ROOM: 'hive_room',
    PLAYER: 'hive_player'
} as const;

export interface StateSetters {
    setRoomCode: React.Dispatch<React.SetStateAction<string | null>>;
    setRoomState: React.Dispatch<React.SetStateAction<RoomStatus>>;
    setRules: React.Dispatch<React.SetStateAction<SpyGameRulesDto | null>>;
    setWordPacks: React.Dispatch<React.SetStateAction<SpyGameWordPacksDto | null>>;
    setPlayers: React.Dispatch<React.SetStateAction<SpyPlayerDto[]>>;
    setMessages: React.Dispatch<React.SetStateAction<ChatMessageDto[]>>;
    setGameState: React.Dispatch<React.SetStateAction<SpyGameStateDto | null>>;
    setMe: React.Dispatch<React.SetStateAction<SpyPlayerDto | null>>;
    setWinnerTeam: React.Dispatch<React.SetStateAction<SpyGameTeam | null>>;
    setGameEndReason: React.Dispatch<React.SetStateAction<SpyGameEndReason | null>>;
    setGameEndMessage: React.Dispatch<React.SetStateAction<string | null>>;
    setSpiesReveal: React.Dispatch<React.SetStateAction<SpyRevealDto[]>>;
    setIsReconnecting: React.Dispatch<React.SetStateAction<boolean>>;
}

interface UseSpyGameSessionProps {
    getService: () => SpySignalRService;
    stateSetters: StateSetters;
    isConnected: boolean;
    isConnecting: boolean;
    isReconnecting: boolean;
    setIsConnecting: (val: boolean) => void;
    setIsConnected: (val: boolean) => void;
    setIsInitializing: (val: boolean) => void;
}

export function useSpyGameSession({
                                      getService,
                                      stateSetters,
                                      isConnected,
                                      isConnecting,
                                      isReconnecting,
                                      setIsConnecting,
                                      setIsConnected,
                                      setIsInitializing,
                                  }: UseSpyGameSessionProps) {
    const isInitialized = useRef(false);

    const saveSession = useCallback((code: string, pid: string) => {
        sessionStorage.setItem(SESSION_KEYS.ROOM, code);
        sessionStorage.setItem(SESSION_KEYS.PLAYER, pid);
    }, []);

    const clearSession = useCallback(() => {
        sessionStorage.removeItem(SESSION_KEYS.ROOM);
        sessionStorage.removeItem(SESSION_KEYS.PLAYER);
    }, []);

    const resetState = useCallback(() => {
        stateSetters.setRoomCode(null);
        stateSetters.setMe(null);
        stateSetters.setPlayers([]);
        stateSetters.setRules(null);
        stateSetters.setWordPacks(null);
        stateSetters.setRoomState(RoomStatus.Lobby);
        stateSetters.setGameState(null);
        stateSetters.setWinnerTeam(null);
        stateSetters.setGameEndReason(null);
        stateSetters.setGameEndMessage(null);
    }, [stateSetters]);

    const reconnect = useCallback(async (svc: SpySignalRService) => {
        if (isReconnecting) return;

        const savedRoom = sessionStorage.getItem(SESSION_KEYS.ROOM);
        const savedPlayer = sessionStorage.getItem(SESSION_KEYS.PLAYER);

        if (savedRoom && savedPlayer) {
            console.log(`[Session] Attempting logical reconnect to ${savedRoom}`);
            try {
                stateSetters.setIsReconnecting(true);
                const fullState = await svc.reconnect(savedRoom, savedPlayer);

                stateSetters.setRoomCode(fullState.roomCode);
                stateSetters.setRoomState(fullState.status);
                stateSetters.setRules(fullState.rules);
                stateSetters.setWordPacks(fullState.wordPacks);
                stateSetters.setPlayers(fullState.players);
                stateSetters.setMessages(fullState.messages);
                stateSetters.setSpiesReveal(fullState.gameState?.spiesReveal ?? []);
                stateSetters.setGameEndReason(fullState.gameState?.roundEndReason ?? null);
                stateSetters.setWinnerTeam(fullState.gameState?.winnerTeam ?? null);

                if (fullState.gameState) {
                    stateSetters.setGameState({ ...fullState.gameState });
                } else {
                    stateSetters.setGameState(null);
                }

                const myPlayer = fullState.players.find(p => p.id === savedPlayer);
                if (myPlayer) stateSetters.setMe(myPlayer);

                saveSession(fullState.roomCode, savedPlayer);
                console.log(fullState)
                console.log("[Session] Reconnect success");
            } catch (error) {
                console.warn("[Session] Reconnect failed. Clearing session.", error);
                clearSession();
                resetState();
            } finally {
                stateSetters.setIsReconnecting(false);
            }
        }
    }, [isReconnecting, clearSession, resetState, saveSession, stateSetters]);

    const connect = useCallback(async (svc: SpySignalRService) => {
        if (isConnected || isConnecting) return;

        setIsConnecting(true);
        svc.onTransportReconnected(async (newId) => {
            console.log(`[Session] Transport recovered (ID: ${newId}). Triggering logical reconnect...`);
            await reconnect(svc);
        });

        await svc.start();
        setIsConnected(true);
        setIsConnecting(false);
    }, [isConnected, isConnecting, reconnect, setIsConnecting, setIsConnected]);

    // Bootstrap: connect and try to reconnect on mount
    useEffect(() => {
        if (isInitialized.current) return;
        isInitialized.current = true;

        const bootstrap = async () => {
            const svc = getService();
            try {
                await connect(svc);
                await reconnect(svc);
            } catch (error) {
                console.error("[Session] Initialization failed", error);
                setIsConnecting(false);
            } finally {
                setIsInitializing(false);
            }
        };

        bootstrap();
    }, [connect, getService, reconnect, setIsConnecting, setIsInitializing]);

    return {
        reconnect,
        resetState,
        saveSession,
        clearSession,
    };
}