import { useEffect } from 'react';
import { SpyHubEvents } from '../../const/spy-game-events';
import {
    type SpyPlayerDto,
    type SpyPlayerJoinedEventDto,
    type SpyGameRulesUpdatedEventDto,
    type SpyGameWordPacksUpdatedEventDto,
    type SpyGameStartedEventDto,
    type PlayerVotedToStopTimerEventDto,
    type VotingStartedEventDto,
    type VoteCastEventDto,
    type VotingResultEventDto,
    type SpyGameEndedEventDto,
    type SpyGameRoundTimerStateChangedEventDto,
    SpyVotingType,
    type SpyGameStateDto,
    type SpyMadeGuessEventDto,
} from '../../models/spy-game';
import { SpySignalRService } from "../../api/spy-signal-r-service";
import type { StateSetters } from './useSpyGameSession';
import {
    type ChatMessageDto,
    type ChatMessageEventDto,
    type HostChangedEventDto,
    type PlayerChangedAvatarEventDto,
    type PlayerChangedNameEventDto,
    type PlayerConnectionChangedEventDto,
    type PlayerKickedEventDto,
    type PlayerLeftEventDto,
    type PlayerReadyStatusChangedEventDto,
    RoomStatus,
    TargetVoteType
} from "../../models/shared.ts";

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
                                 }: UseSpyGameEventsProps) {
    useEffect(() => {
        if (!isConnected) return;

        const svc = getService();
        const meId = me?.id;

        const handlePlayerJoined = (e: SpyPlayerJoinedEventDto) => {
            stateSetters.setPlayers((prev) => {
                if (prev.some(p => p.id === e.player.id)) return prev;
                return [...prev, e.player];
            });
        };

        const handlePlayerLeft = (e: PlayerLeftEventDto) => {
            stateSetters.setPlayers((prev) => prev.filter(p => p.id !== e.playerId));
        };

        const handleNameChanged = (e: PlayerChangedNameEventDto) => {
            stateSetters.setPlayers((prev) =>
                prev.map(p => p.id === e.playerId ? { ...p, name: e.newName } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev) => prev ? { ...prev, name: e.newName } : null);
            }
        };

        const handlePlayerKicked = (e: PlayerKickedEventDto) => {
            if (meId === e.playerId) {
                alert("You have been kicked from the room.");
                clearSession();
                resetState();
            } else {
                stateSetters.setPlayers((prev) => prev.filter(p => p.id !== e.playerId));
            }
        };

        const handleReadyChanged = (e: PlayerReadyStatusChangedEventDto) => {
            stateSetters.setPlayers((prev) =>
                prev.map(p => p.id === e.playerId ? { ...p, isReady: e.isReady } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev) => prev ? { ...prev, isReady: e.isReady } : null);
            }
        };

        const handleAvatarChanged = (e: PlayerChangedAvatarEventDto) => {
            stateSetters.setPlayers((prev) =>
                prev.map(p => p.id === e.playerId ? { ...p, avatarId: e.newAvatarId } : p)
            );
            if (meId === e.playerId) {
                stateSetters.setMe((prev) => prev ? { ...prev, avatarId: e.newAvatarId } : null);
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

        const handleRulesUpdated = (e: SpyGameRulesUpdatedEventDto) => {
            stateSetters.setRules(e.rules);
        };

        const handleWordPacksUpdated = (e: SpyGameWordPacksUpdatedEventDto) => {
            stateSetters.setWordPacks(e.packs);
        };

        const handleGameStarted = (e: SpyGameStartedEventDto) => {
            const newState = e.state;
            stateSetters.setRoomState(newState.status);
            stateSetters.setGameState(newState.gameState);
            stateSetters.setPlayers(newState.players);
            stateSetters.setRules(newState.rules);
            stateSetters.setWordPacks(newState.wordPacks);

            const myNewState = newState.players.find(p => p.id === me?.id);
            if (myNewState) stateSetters.setMe(myNewState);

            stateSetters.setWinnerTeam(null);
            stateSetters.setGameEndReason(null);
            stateSetters.setGameEndMessage(null);
            stateSetters.setSpiesReveal([]);
        };

        const handleChatMessage = (e: ChatMessageEventDto) => {
            stateSetters.setMessages((prev: ChatMessageDto[]) => [...prev, e.message]);
        };

        const handleConnectionChanged = (e: PlayerConnectionChangedEventDto) => {
            stateSetters.setPlayers((prev: SpyPlayerDto[]) =>
                prev.map(p => p.id === e.playerId ? { ...p, isConnected: e.isConnected } : p)
            );
        };

        const handleTimerVote = (e: PlayerVotedToStopTimerEventDto) => {
            stateSetters.setPlayers((prev) =>
                prev.map(p => p.id === e.playerId ? { ...p, isVotedToStopTimer: true } : p)
            );
            stateSetters.setGameState((prev) => {
                if (!prev) return null;
                return {
                    ...prev,
                    playersVotedToStopTimer: e.votesCount,
                    votesRequiredToStopTimer: e.requiredVotes
                };
            });
        };

        const handleRoundTimerStateChanged = (e: SpyGameRoundTimerStateChangedEventDto) => {
            stateSetters.setGameState((prev) => {
                if (!prev) return null;
                return {
                    ...prev,
                    roundTimerStatus: e.timerStatus,
                    roundRemainingSeconds: e.remainingSeconds
                };
            });
        };

        const handleVotingStarted = (e: VotingStartedEventDto) => {
            if (e.votingType === SpyVotingType.Accusation) {
                stateSetters.setPlayers((prev) =>
                    prev.map(p => p.id === e.initiatorId ? { ...p, hasUsedAccusation: true } : p)
                );

                if (me?.id === e.initiatorId) {
                    stateSetters.setMe((prev) => prev ? { ...prev, hasUsedAccusation: true } : null);
                }
            }

            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
                if (!prev) return null;

                let initialTargetVoting: Record<string, TargetVoteType> | null = null;

                if (e.votingType === SpyVotingType.Accusation) {
                    initialTargetVoting = {
                        [e.initiatorId]: TargetVoteType.Yes
                    };
                }

                return {
                    ...prev,
                    phase: e.currentGamePhase,
                    activeVoting: {
                        type: e.votingType,
                        accusedPlayerId: e.targetId,
                        accusedPlayerName: e.targetName,
                        startedAt: e.startedAt,
                        endsAt: e.endsAt,
                        targetVoting: initialTargetVoting,
                        againstVoting: e.votingType === SpyVotingType.Final ? {} : null,
                        votesRequired: null
                    }
                };
            });
        };

        const handleVoteCast = (e: VoteCastEventDto) => {
            stateSetters.setGameState((prev) => {
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
            stateSetters.setGameState((prev) => {
                if (!prev) return null;
                return {
                    ...prev,
                    activeVoting: null,
                    phase: e.currentGamePhase,
                    caughtSpyId: (e.isSuccess && e.accusedId) ? e.accusedId : prev.caughtSpyId,
                    caughtSpyName: e.accusedSpyName,
                    spyLastChanceEndsAt: e.lastChanceEndsAt ?? null
                };
            });

            if (e.resultMessage) {
                console.log(`Voting Result: ${e.resultMessage}`);
            }
        };

        const handleSpyMadeGuess = (e: SpyMadeGuessEventDto) => {
            if (e.isSpyDead) {
                stateSetters.setPlayers(prev =>
                    prev.map(p => p.id === e.playerId ? { ...p, isDead: true } : p)
                );

                // Update me if I'm the spy who failed
                if (me?.id === e.playerId) {
                    stateSetters.setMe(prev => prev ? { ...prev, isDead: true } : null);
                }
            }
            stateSetters.setGameState((prev: SpyGameStateDto | null) => {
                if (!prev) return null;
                return {
                    ...prev,
                    caughtSpyName: null,
                    caughtSpyId: null,
                    lastChanceEndsAt: null,
                };
            });

            console.log(`Spy ${e.playerId} guessed: ${e.word} - ${e.isGuessCorrect ? 'Correct!' : 'Wrong'}`);
        };

        const handleGameEnded = (e: SpyGameEndedEventDto) => {
            stateSetters.setRoomState(RoomStatus.Ended);
            stateSetters.setWinnerTeam(e.winnerTeam);
            stateSetters.setGameEndReason(e.reason);
            stateSetters.setGameEndMessage(e.reasonMessage);
            stateSetters.setSpiesReveal(e.spiesReveal);
            stateSetters.setPlayers(prev =>
                prev.map(p => {
                    const reveal = e.spiesReveal.find(r => r.playerId === p.id);
                    return reveal ? { ...p, isSpy: reveal.isSpy, isDead: reveal.isDead } : p;
                })
            );
            stateSetters.setGameState(prev => {
                if (!prev) return null;
                return {
                    ...prev,
                    spiesReveal: e.spiesReveal,
                    roundEndReason: e.reason
                };
            });
        };

        const handleReturnToLobby = () => {
            stateSetters.setRoomState(RoomStatus.Lobby);
            stateSetters.setGameState(null);
            stateSetters.setWinnerTeam(null);
            stateSetters.setGameEndReason(null);
            stateSetters.setGameEndMessage(null);
            stateSetters.setSpiesReveal([]);

            stateSetters.setPlayers((prev) =>
                prev.map(p => ({
                    ...p,
                    isReady: false,
                    isVotedToStopTimer: false,
                    isSpy: null,
                    isDead: false,
                    hasUsedAccusation: false
                }))
            );

            stateSetters.setMe((prev) =>
                prev ? {
                    ...prev,
                    isReady: false,
                    isVotedToStopTimer: false,
                    isSpy: null,
                    isDead: false,
                    hasUsedAccusation: false
                } : null
            );
        };

        svc.on(SpyHubEvents.PlayerJoined, handlePlayerJoined);
        svc.on(SpyHubEvents.PlayerLeft, handlePlayerLeft);
        svc.on(SpyHubEvents.PlayerChangedName, handleNameChanged);
        svc.on(SpyHubEvents.PlayerKicked, handlePlayerKicked);
        svc.on(SpyHubEvents.PlayerReadyStatusChanged, handleReadyChanged);
        svc.on(SpyHubEvents.PlayerChangedAvatar, handleAvatarChanged);
        svc.on(SpyHubEvents.HostChanged, handleHostChanged);
        svc.on(SpyHubEvents.RulesChanged, handleRulesUpdated);
        svc.on(SpyHubEvents.WordPacksChanged, handleWordPacksUpdated);
        svc.on(SpyHubEvents.GameStarted, handleGameStarted);
        svc.on(SpyHubEvents.ChatMessageReceived, handleChatMessage);
        svc.on(SpyHubEvents.PlayerConnectionStatusChanged, handleConnectionChanged);
        svc.on(SpyHubEvents.TimerVoteUpdated, handleTimerVote);
        svc.on(SpyHubEvents.RoundTimerStateChanged, handleRoundTimerStateChanged);
        svc.on(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
        svc.on(SpyHubEvents.VotingStarted, handleVotingStarted);
        svc.on(SpyHubEvents.VoteCast, handleVoteCast);
        svc.on(SpyHubEvents.VotingResult, handleVotingResult);
        svc.on(SpyHubEvents.SpyMadeGuess, handleSpyMadeGuess);
        svc.on(SpyHubEvents.GameEnded, handleGameEnded);

        return () => {
            svc.off(SpyHubEvents.PlayerJoined, handlePlayerJoined);
            svc.off(SpyHubEvents.PlayerLeft, handlePlayerLeft);
            svc.off(SpyHubEvents.PlayerChangedName, handleNameChanged);
            svc.off(SpyHubEvents.PlayerKicked, handlePlayerKicked);
            svc.off(SpyHubEvents.PlayerReadyStatusChanged, handleReadyChanged);
            svc.off(SpyHubEvents.PlayerChangedAvatar, handleAvatarChanged);
            svc.off(SpyHubEvents.HostChanged, handleHostChanged);
            svc.off(SpyHubEvents.RulesChanged, handleRulesUpdated);
            svc.off(SpyHubEvents.WordPacksChanged, handleWordPacksUpdated);
            svc.off(SpyHubEvents.GameStarted, handleGameStarted);
            svc.off(SpyHubEvents.ChatMessageReceived, handleChatMessage);
            svc.off(SpyHubEvents.PlayerConnectionStatusChanged, handleConnectionChanged);
            svc.off(SpyHubEvents.TimerVoteUpdated, handleTimerVote);
            svc.off(SpyHubEvents.RoundTimerStateChanged, handleRoundTimerStateChanged);
            svc.off(SpyHubEvents.ReturnedToLobby, handleReturnToLobby);
            svc.off(SpyHubEvents.VotingStarted, handleVotingStarted);
            svc.off(SpyHubEvents.VoteCast, handleVoteCast);
            svc.off(SpyHubEvents.VotingResult, handleVotingResult);
            svc.off(SpyHubEvents.SpyMadeGuess, handleSpyMadeGuess);
            svc.off(SpyHubEvents.GameEnded, handleGameEnded);
        };
    }, [isConnected, me?.id, getService, clearSession, resetState, stateSetters]);
}