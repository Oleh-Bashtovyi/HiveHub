import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/spy-game/SpyGameContext';
import { RoomStatus, SpyGamePhase, SpyVotingType } from '../../../models/spy-game';
import { SpyGameHeader } from './SpyGameHeader/SpyGameHeader';
import { SpyGameRoleCard } from './SpyGameRoleCard/SpyGameRoleCard';
import { SpyGamePlayers } from './SpyGamePlayers/SpyGamePlayers';
import { SpyGameRules } from './SpyGameRules/SpyGameRules';
import { SpyGameChat } from './SpyGameChat/SpyGameChat';
import { AccusationVotingModal } from './AccusationVotingModal/AccusationVotingModal';
import { FinalVotingModal } from './FinalVotingModal/FinalVotingModal';
import { GuessWordModal } from './GuessWordModal/GuessWordModal';
import { Button } from '../../../components/ui/Button/Button';
import './SpyGame.scss';

export const SpyGame = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        me,
        gameState,
        roomState,
        isInitializing,
        messages,
        sendMessage,
        voteStopTimer,
        leaveRoom,
        returnToLobby,
        startAccusation,
        vote,
        makeGuess,
    } = useSpyGame();

    const [isGuessModalOpen, setIsGuessModalOpen] = useState(false);

    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: unknown) {
            console.error(error);
            const msg = error instanceof Error ? error.message : 'Невідома помилка';
            alert(`Помилка: ${msg}`);
        }
    };

    useEffect(() => {
        if (isInitializing) return;

        if (!roomCode) {
            navigate('/spy');
            return;
        }

        if (roomState === RoomStatus.Lobby) {
            navigate('/spy/lobby');
            return;
        }

        if (roomState === RoomStatus.Ended) {
            navigate('/spy/results');
            return;
        }
    }, [roomCode, roomState, navigate, isInitializing]);

    if (isInitializing || !gameState || !me || !roomCode) return null;

    // --- LOGIC ---
    const isSpyRole = !gameState.currentSecretWord;
    const isSearchPhase = gameState.phase === SpyGamePhase.Search;

    // Voting Logic
    const activeVoting = gameState.activeVoting;
    const isAccusation = activeVoting?.type === SpyVotingType.Accusation;
    const isFinal = activeVoting?.type === SpyVotingType.Final;
    const hasUsedAccusation = me.hasUsedAccusation ?? false;

    // Last Chance Logic
    const isLastChancePhase = gameState.phase === SpyGamePhase.SpyLastChance;
    const amICaughtSpy = isLastChancePhase && gameState.caughtSpyId === me.id;

    // Force open modal if it's Last Chance for me
    const showGuessModal = isGuessModalOpen || amICaughtSpy;
    const canAccuse = !activeVoting &&
        isSearchPhase &&
        !hasUsedAccusation;

    return (
        <div className="spy-game-page">
            <div className="spy-game-container">
                <SpyGameHeader
                    roomCode={roomCode}
                    stopAt={gameState.roundTimerWillStopAt}
                    isTimerStopped={gameState.isRoundTimerStopped}
                    timerVotesCount={gameState.timerVotesCount}
                    activePlayers={players.filter(p => p.isConnected).length}
                    hasVoted={me.isVotedToStopTimer || false}
                    onVoteStopTimer={() => safeExecute(voteStopTimer)}
                />

                <div className="spy-game-layout">
                    <div className="spy-game-layout__column spy-game-layout__column--left">
                        <SpyGameRoleCard
                            isSpy={isSpyRole}
                            secretWord={gameState.currentSecretWord}
                            category={gameState.category}
                            onGuessWord={() => setIsGuessModalOpen(true)}
                        />
                        <SpyGamePlayers
                            players={players}
                            currentPlayerId={me.id}
                            isTimerStopped={gameState.isRoundTimerStopped}
                            caughtSpyId={gameState.caughtSpyId}
                            canAccuse={canAccuse}
                            onAccuse={(id) => safeExecute(() => startAccusation(id))}
                        />
                    </div>

                    <div className="spy-game-layout__column spy-game-layout__column--center">
                        <SpyGameRules />
                        <div className="spy-game-actions">
                            {me.isHost && (
                                <Button variant="secondary" fullWidth onClick={() => confirm('Всі в лобі?') && safeExecute(returnToLobby)}>
                                    🛑 В лобі (Всіх)
                                </Button>
                            )}
                            <Button variant="danger" fullWidth onClick={() => confirm('Вийти?') && safeExecute(async () => { await leaveRoom(); navigate('/spy'); })}>
                                🚪 Покинути гру
                            </Button>
                        </div>
                    </div>

                    <div className="spy-game-layout__column spy-game-layout__column--right">
                        <SpyGameChat
                            messages={messages}
                            currentPlayerId={me.id}
                            onSendMessage={(msg) => safeExecute(() => sendMessage(msg))}
                        />
                    </div>
                </div>
            </div>

            {/* MODALS */}
            {isAccusation && activeVoting && (
                <AccusationVotingModal
                    isOpen={true}
                    targetName={activeVoting.accusedPlayerName || 'Unknown'}
                    isAccused={me.id === activeVoting.accusedPlayerId}
                    myVote={activeVoting.targetVoting ? activeVoting.targetVoting[me.id] : undefined}
                    endsAt={activeVoting.endsAt}
                    onVote={(type) => safeExecute(() => vote(activeVoting.accusedPlayerId!, type))}
                />
            )}

            {isFinal && activeVoting && (
                <FinalVotingModal
                    isOpen={true}
                    players={players.filter(p => p.id !== me.id)}
                    hasVoted={!!activeVoting.againstVoting && me.id in activeVoting.againstVoting}
                    endsAt={activeVoting.endsAt}
                    onVote={(targetId) => safeExecute(() => vote(targetId, null))}
                />
            )}

            {showGuessModal && isSpyRole && (
                <GuessWordModal
                    isOpen={true}
                    category={gameState.category}
                    isLastChance={amICaughtSpy}
                    endsAt={gameState.lastChanceEndsAt || null}
                    onClose={() => setIsGuessModalOpen(false)}
                    onGuess={(word) => safeExecute(async () => {
                        await makeGuess(word);
                        setIsGuessModalOpen(false);
                    })}
                />
            )}
        </div>
    );
};