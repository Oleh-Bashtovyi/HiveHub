import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/spy-game/SpyGameContext';
import { SpyGamePhase, SpyVotingType } from '../../../models/spy-game';
import { SpyGameHeader } from './SpyGameHeader/SpyGameHeader';
import { SpyGameRoleCard } from './SpyGameRoleCard/SpyGameRoleCard';
import { SpyGamePlayers } from './SpyGamePlayers/SpyGamePlayers';
import { SpyGameRules } from './SpyGameRules/SpyGameRules';
import { SpyGameChat } from './SpyGameChat/SpyGameChat';
import { AccusationVotingModal } from './AccusationVotingModal/AccusationVotingModal';
import { FinalVotingModal } from './FinalVotingModal/FinalVotingModal';
import { GuessWordModal } from './GuessWordModal/GuessWordModal';
import { Button } from '../../../components/ui/Button/Button';
import { ToastContainer } from '../../../components/ui/ToastContainer/ToastContainer';
import './SpyGame.scss';
import { RoomStatus } from "../../../models/shared.ts";

export const SpyGame = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        me,
        gameState,
        rules,
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
    const [toastMessage, setToastMessage] = useState<string | null>(null);

    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: unknown) {
            console.error(error);
            const msg = error instanceof Error ? error.message : 'Unknown error';
            setToastMessage(`Error: ${msg}`);
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

    useEffect(() => {
        if (!gameState) return;

        if (gameState.phase === SpyGamePhase.SpyLastChance) {
            if (gameState.caughtSpyId === me?.id) {
                // eslint-disable-next-line react-hooks/set-state-in-effect
                setToastMessage("⚠️ You are caught! Guess the location!");
            } else {
                setToastMessage(`🕵️ Spy caught! They are guessing the location...`);
            }
        }

        if (gameState.activeVoting?.type === SpyVotingType.Accusation) {
            setToastMessage(`🗳️ Voting started against: ${gameState.activeVoting.accusedPlayerName}`);
        }
    }, [gameState?.phase, gameState?.activeVoting?.type, gameState?.caughtSpyId, me?.id, gameState]);

    if (isInitializing || !gameState || !me || !roomCode || !rules) return null;

    const isSpyRole = me.isSpy ?? false;
    const isSearchPhase = gameState.phase === SpyGamePhase.Search;
    const isDead = me.isDead ?? false;

    const activeVoting = gameState.activeVoting;
    const isAccusation = activeVoting?.type === SpyVotingType.Accusation;
    const isFinal = activeVoting?.type === SpyVotingType.Final;
    const hasUsedAccusation = me.hasUsedAccusation ?? false;

    const isLastChancePhase = gameState.phase === SpyGamePhase.SpyLastChance;
    const amICaughtSpy = isLastChancePhase && gameState.caughtSpyId === me.id;
    const caughtSpyName = gameState.caughtSpyName;

    const showGuessModal = isGuessModalOpen || amICaughtSpy;
    const canAccuse = !activeVoting &&
        isSearchPhase &&
        !hasUsedAccusation &&
        !isDead;

    const shouldShowSpies = rules.isSpiesKnowEachOther && isSpyRole;

    return (
        <div className="spy-game-page">
            <ToastContainer
                message={toastMessage}
                onClose={() => setToastMessage(null)}
                duration={4000}
            />

            <div className="spy-game-container">
                <SpyGameHeader
                    roomCode={roomCode}
                    remainingSeconds={gameState.roundRemainingSeconds}
                    timerStatus={gameState.roundTimerStatus}
                    playersVoted={gameState.playersVotedToStopTimer}
                    votesRequired={gameState.votesRequiredToStopTimer}
                    hasVoted={me.isVotedToStopTimer || false}
                    onVoteStopTimer={() => safeExecute(voteStopTimer)}
                />

                {isLastChancePhase && (
                    <div className="spy-game-last-chance-banner">
                        <div className="spy-game-last-chance-banner__icon">🔥</div>
                        <div className="spy-game-last-chance-banner__content">
                            <div className="spy-game-last-chance-banner__title">
                                {amICaughtSpy ? "ВАС СПІЙМАЛИ!" : `ШПИГУН СПІЙМАНИЙ: ${caughtSpyName}`}
                            </div>
                            <div className="spy-game-last-chance-banner__text">
                                {amICaughtSpy
                                    ? "У вас є останній шанс: вгадайте локацію щоб виграти!"
                                    : "Шпигун обирає локацію. Якщо він вгадає — шпигуни переможуть!"}
                            </div>
                        </div>
                    </div>
                )}

                <div className="spy-game-layout">
                    <div className="spy-game-layout__column spy-game-layout__column--left">
                        <SpyGameRoleCard
                            isSpy={isSpyRole}
                            isDead={isDead}
                            secretWord={gameState.currentSecretWord}
                            category={gameState.currentCategory}
                            onGuessWord={isDead ? undefined : () => setIsGuessModalOpen(true)}
                        />
                        <SpyGamePlayers
                            players={players}
                            currentPlayerId={me.id}
                            shouldShowSpies={shouldShowSpies}
                            votesForTimer={gameState.playersVotedToStopTimer}
                            votesRequired={gameState.votesRequiredToStopTimer}
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
                    players={players.filter(p => p.id !== me.id && !p.isDead)}
                    hasVoted={!!activeVoting.againstVoting && me.id in activeVoting.againstVoting}
                    myVote={activeVoting.againstVoting?.[me.id] || null}
                    endsAt={activeVoting.endsAt}
                    onVote={(targetId) => safeExecute(() => vote(targetId, null))}
                />
            )}

            {showGuessModal && isSpyRole && !isDead && (
                <GuessWordModal
                    isOpen={true}
                    category={gameState.currentCategory}
                    isLastChance={amICaughtSpy}
                    endsAt={gameState.spyLastChanceEndsAt || null}
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