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

    const [timeLeft, setTimeLeft] = useState(0);
    const [showGuessModal, setShowGuessModal] = useState(false);

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

    useEffect(() => {
        if (!gameState?.gameEndTime || gameState.isTimerStopped) return;

        const updateTimer = () => {
            const end = new Date(gameState.gameEndTime!).getTime();
            const now = new Date().getTime();
            const diff = Math.floor((end - now) / 1000);
            setTimeLeft(Math.max(0, diff));
        };

        updateTimer();
        const interval = setInterval(updateTimer, 1000);
        return () => clearInterval(interval);
    }, [gameState?.gameEndTime, gameState?.isTimerStopped]);

    if (isInitializing || !gameState || !me || !roomCode) {
        return null;
    }

    const handleVoteStopTimer = () => {
        if (confirm("Зупинити таймер для обговорення?")) {
            void safeExecute(async () => await voteStopTimer());
        }
    };

    const handleLeave = () => {
        if (confirm('Ви впевнені? Це завершить гру для вас.')) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy');
            });
        }
    };

    const handleReturnToLobby = () => {
        if (confirm('УВАГА: Це примусово завершить гру для всіх і поверне всіх в лобі. Продовжити?')) {
            void safeExecute(async () => await returnToLobby());
        }
    };

    const handleAccuse = (playerId: string) => {
        void safeExecute(async () => await startAccusation(playerId));
    };

    const handleVote = (targetId: string, voteType: string | null) => {
        void safeExecute(async () => await vote(targetId, voteType));
    };

    const handleGuess = (word: string) => {
        void safeExecute(async () => {
            await makeGuess(word);
            setShowGuessModal(false);
        });
    };

    const isSpyRole = !gameState.currentSecretWord;
    const activePlayers = players.filter(p => p.isConnected).length;
    const isVotingActive = gameState.activeVoting !== null;
    const isAccusationVoting = gameState.activeVoting?.type === SpyVotingType.Accusation;
    const isFinalVoting = gameState.activeVoting?.type === SpyVotingType.Final;

    return (
        <div className="spy-game-page">
            <div className="spy-game-container">
                <SpyGameHeader
                    roomCode={roomCode}
                    timeLeft={timeLeft}
                    isTimerStopped={gameState.isTimerStopped}
                    timerVotesCount={gameState.timerVotesCount}
                    activePlayers={activePlayers}
                    hasVoted={me.isVotedToStopTimer}
                    onVoteStopTimer={handleVoteStopTimer}
                />

                <div className="spy-game-layout">
                    {/* LEFT: Role + Players */}
                    <div className="spy-game-layout__column spy-game-layout__column--left">
                        <SpyGameRoleCard
                            isSpy={isSpyRole}
                            secretWord={gameState.currentSecretWord}
                            category={gameState.category}
                            onGuessWord={() => setShowGuessModal(true)}
                        />

                        <SpyGamePlayers
                            players={players}
                            currentPlayerId={me.id}
                            isTimerStopped={gameState.isTimerStopped}
                            caughtSpyId={gameState.caughtSpyId}
                            canAccuse={!isVotingActive && gameState.phase === SpyGamePhase.Search}
                            onAccuse={handleAccuse}
                        />
                    </div>

                    {/* CENTER: Rules */}
                    <div className="spy-game-layout__column spy-game-layout__column--center">
                        <SpyGameRules />

                        {/* Host Actions */}
                        <div className="spy-game-actions">
                            {me.isHost && (
                                <Button
                                    variant="secondary"
                                    fullWidth
                                    onClick={handleReturnToLobby}
                                    title="Повернути всіх в лобі та скинути гру"
                                >
                                    🛑 В лобі (Всіх)
                                </Button>
                            )}

                            <Button variant="danger" fullWidth onClick={handleLeave}>
                                🚪 Покинути гру
                            </Button>
                        </div>
                    </div>

                    {/* RIGHT: Chat */}
                    <div className="spy-game-layout__column spy-game-layout__column--right">
                        <SpyGameChat
                            messages={messages}
                            currentPlayerId={me.id}
                            onSendMessage={sendMessage}
                        />
                    </div>
                </div>
            </div>

            {/* Voting Modals */}
            {isAccusationVoting && gameState.activeVoting && (
                <AccusationVotingModal
                    isOpen={true}
                    targetName={gameState.activeVoting.accusedPlayerName || 'Unknown'}
                    hasVoted={me.id in (gameState.activeVoting.targetVoting || {})}
                    onVote={(voteType) => handleVote(gameState.activeVoting!.accusedPlayerId || '', voteType)}
                />
            )}

            {isFinalVoting && gameState.activeVoting && (
                <FinalVotingModal
                    isOpen={true}
                    players={players.filter(p => p.id !== me.id)}
                    hasVoted={me.id in (gameState.activeVoting.againstVoting || {})}
                    onVote={(playerId) => handleVote(playerId, null)}
                />
            )}

            {/* Guess Word Modal (Spy only) */}
            {showGuessModal && isSpyRole && (
                <GuessWordModal
                    isOpen={showGuessModal}
                    category={gameState.category}
                    onClose={() => setShowGuessModal(false)}
                    onGuess={handleGuess}
                />
            )}
        </div>
    );
};