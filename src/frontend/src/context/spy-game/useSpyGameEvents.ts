import { useEffect, useCallback } from 'react';
import { SpyHubEvents } from '../../const/spy-game-events';
import {
    RoomStatus,
    type SpyPlayerDto,
    type SpyPlayerJoinedEventDto,
    type PlayerLeftEventDto,
    type PlayerChangedNameEventDto,
    type PlayerKickedEventDto,
    type PlayerReadyStatusChangedEventDto,
    type PlayerChangedAvatarEventDto,
    type HostChangedEventDto,
    type SpyGameSettingsUpdatedEventDto,
    type SpyGameStartedEventDto,
    type ChatMessageEventDto,
    type PlayerVotedToStopTimerEventDto,
    type PlayerConnectionChangedEventDto,
    type VotingStartedEventDto,
    type VoteCastEventDto,
    type VotingResultEventDto,
    type SpyGameEndedEventDto,
    type SpyGameStateDto,
    SpyVotingType, type ChatMessageDto
} from '../../models/spy-game';
import { SpySignalRService } from "../../api/spy-signal-r-service";
import type { StateSetters } from './useSpyGameSession';

interface UseSpyGameEventsProps {
    isConnected: boolean;
    getService: () => SpySignalRService;
    me: SpyPlayerDto | null;
    stateSetters: StateSetters;
    clearSession: () => void;
    resetState: () => void;
    reconnect: (svc: SpySignalRService) => Promise<void>;
}

export function useSpyGameEvents({
                                     isConnected,
                                     getService,
                                     me,
                                     stateSetters,
                                     clearSession,
                                     resetState,
                                     reconnect,
                                 }: UseSpyGameEventsProps) {
    const refreshState = useCallback(async () => {
        const svc = getService();
        await reconnect(svc);
    }, [getService, reconnect]);

    useEffect(() => {
        if (!isConnected) return;

        const svc = getService();
        const meId = me?.id;

        const handlePlayerJoined = (e: SpyPlayerJoinedEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) => {
                if (prev.some(p => p.id === e.player.id)) return prev;
                return [...prev, e.player];
            });
        };

        const handlePlayerLeft = (e: PlayerLeftEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.filter(p => p.id !== e.playerId)
            );
        };

        const handleNameChanged = (e: PlayerChangedNameEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, name: e.newName } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev: SpyPlayerDto | null) =>
                    prev ? { ...prev, name: e.newName } : null
                );
            }
        };

        const handlePlayerKicked = (e: PlayerKickedEventDto) => {
            if (meId === e.playerId) {
                alert("Вас було вигнано з кімнати.");
                clearSession();
                resetState();
            } else {
                stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                    prev.filter(p => p.id !== e.playerId)
                );
            }
        };

        const handleReadyChanged = (e: PlayerReadyStatusChangedEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, isReady: e.isReady } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev: SpyPlayerDto | null) =>
                    prev ? { ...prev, isReady: e.isReady } : null
                );
            }
        };

        const handleAvatarChanged = (e: PlayerChangedAvatarEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, avatarId: e.newAvatarId } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev: SpyPlayerDto | null) =>
                    prev ? { ...prev, avatarId: e.newAvatarId } : null
                );
            }
        };

        const handleHostChanged = (e: HostChangedEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => ({ ...p, isHost: p.id === e.newHostId }))
            );
            stateSetters.setMe((prev: SpyPlayerDto | null) => {
                if (!prev) return null;
                return { ...prev, isHost: prev.id === e.newHostId };
            });
        };

        const handleSettingsUpdated = (e: SpyGameSettingsUpdatedEventDto) => {
            stateSetters.setSettings(e.settings);
        };

        const handleGameStarted = (e: SpyGameStartedEventDto) => {
            const newState = e.state;
            stateSetters.setRoomState(newState.status);
            stateSetters.setGameState(newState.gameState);
            stateSetters.setPlayers(newState.players);
            stateSetters.setSettings(newState.settings);

            const myNewState = newState.players.find(p => p.id === me?.id);
            if (myNewState) stateSetters.setMe(myNewState);

            stateSetters.setWinnerTeam(null);
            stateSetters.setGameEndReason(null);
            stateSetters.setGameEndMessage(null);
        };

        const handleChatMessage = (e: ChatMessageEventDto) => {
            stateSetters.setMessages((prev: ChatMessageDto[]) => [...prev, e.message]);
        };

        const handleConnectionChanged = (e: PlayerConnectionChangedEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, isConnected: e.isConnected } : p)
            );
        };

        const handleTimerStopped = (e: PlayerVotedToStopTimerEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, isVotedToStopTimer: true } : p)
            );
            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
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

        const handleVotingStarted = (e: VotingStartedEventDto) => {
            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
                if (!prev) return null;
                return {
                    ...prev,
                    phase: e.currentGamePhase,
                    activeVoting: {
                        type: e.votingType,
                        accusedPlayerId: e.targetId,
                        accusedPlayerName: e.targetName,
                        startedAt: new Date().toISOString(),
                        endsAt: e.endsAt,
                        targetVoting: e.votingType === SpyVotingType.Accusation ? {} : null,
                        againstVoting: e.votingType === SpyVotingType.Final ? {} : null,
                        votesRequired: null
                    }
                };
            });
        };

        const handleVoteCast = (e: VoteCastEventDto) => {
            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
                if (!prev || !prev.activeVoting) return prev;

                const newActiveVoting = { ...prev.activeVoting };

                if (newActiveVoting.type === SpyVotingType.Accusation &&
                    e.targetVoteType !== undefined &&
                    newActiveVoting.targetVoting) {
                    newActiveVoting.targetVoting = {
                        ...newActiveVoting.targetVoting,
                        [e.voterId]: e.targetVoteType
                    };
                }
                else if (newActiveVoting.type === SpyVotingType.Final &&
                    e.againstPlayerId &&
                    newActiveVoting.againstVoting) {
                    newActiveVoting.againstVoting = {
                        ...newActiveVoting.againstVoting,
                        [e.voterId]: e.againstPlayerId
                    };
                }

                return { ...prev, activeVoting: newActiveVoting };
            });
        };

        const handleVotingResult = (e: VotingResultEventDto) => {
            console.log("Voting Result:", e.resultMessage);
            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
                if (!prev) return null;
                return {
                    ...prev,
                    activeVoting: null,
                    phase: e.currentGamePhase,
                    caughtSpyId: e.isSuccess && e.accusedId ? e.accusedId : prev.caughtSpyId
                };
            });
        };

        const handleGameEnded = (e: SpyGameEndedEventDto) => {
            stateSetters.setRoomState(RoomStatus.Ended);
            stateSetters.setWinnerTeam(e.winnerTeam);
            stateSetters.setGameEndReason(e.reason);
            stateSetters.setGameEndMessage(e.reasonMessage);
            stateSetters.setPlayers(prev =>
                prev.map(p => {
                    const reveal = e.spiesReveal.find(r => r.playerId === p.id);
                    return reveal ? { ...p, isSpy: reveal.isSpy } : p;
                })
            );
            refreshState();
        };

        const handleReturnToLobby = () => {
            stateSetters.setRoomState(RoomStatus.Lobby);
            stateSetters.setGameState(null);
            stateSetters.setWinnerTeam(null);
            stateSetters.setGameEndReason(null);
            stateSetters.setGameEndMessage(null);

            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => ({
                    ...p,
                    isReady: false,
                    isVotedToStopTimer: false,
                    isSpy: null
                }))
            );

            stateSetters.setMe((prev: SpyPlayerDto | null) =>
                prev ? {
                    ...prev,
                    isReady: false,
                    isVotedToStopTimer: false,
                    isSpy: null
                } : null
            );
        };

        // Subscribe to events
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
        svc.on(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
        svc.on(SpyHubEvents.VotingStarted, handleVotingStarted);
        svc.on(SpyHubEvents.VoteCast, handleVoteCast);
        svc.on(SpyHubEvents.VotingResult, handleVotingResult);
        svc.on(SpyHubEvents.GameEnded, handleGameEnded);

        // Cleanup
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
            svc.off(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
            svc.off(SpyHubEvents.VotingStarted, handleVotingStarted);
            svc.off(SpyHubEvents.VoteCast, handleVoteCast);
            svc.off(SpyHubEvents.VotingResult, handleVotingResult);
            svc.off(SpyHubEvents.GameEnded, handleGameEnded);
        };
    }, [isConnected, me?.id, getService, clearSession, resetState, stateSetters, refreshState]);
}