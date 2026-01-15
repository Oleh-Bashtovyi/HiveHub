import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../../components/ui/Button/Button';
import { RoomStatus, SpyGameEndReason, SpyGameTeam } from '../../../models/spy-game';
import './SpyResults.scss';
import {useSpyGame} from "../../../context/spy-game/SpyGameContext.tsx";
import {SpyGameChat} from "../SpyGame/SpyGameChat/SpyGameChat.tsx";

const END_REASON_TEXT: Record<SpyGameEndReason, string> = {
    [SpyGameEndReason.TimerExpired]: 'Час вийшов! Шпигуни не були знайдені.',
    [SpyGameEndReason.CivilianKicked]: 'Мирного гравця вигнали помилково!',
    [SpyGameEndReason.SpyGuessedWord]: 'Шпигун вгадав секретне слово!',
    [SpyGameEndReason.SpyWrongGuess]: 'Шпигун не вгадав слово!',
    [SpyGameEndReason.FinalVotingFailed]: 'Фінальне голосування провалилося!',
    [SpyGameEndReason.SpyFound]: 'Шпигуна знайдено та викрито!',
};

const TEAM_TEXT: Record<SpyGameTeam, string> = {
    [SpyGameTeam.Civilians]: 'Перемогли мирні',
    [SpyGameTeam.Spies]: 'Перемогли шпигуни',
};

export const SpyResults = () => {
    const navigate = useNavigate();
    const {
        isInitializing,
        roomCode,
        players,
        returnToLobby,
        leaveRoom,
        roomState,
        startGame,
        me,
        messages,
        sendMessage,
        winnerTeam,
        gameEndReason,
        gameEndMessage,
    } = useSpyGame();

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
        }
        else if (roomState === RoomStatus.InGame) {
            navigate('/spy/game');
        }
    }, [roomCode, roomState, navigate, isInitializing]);

    const handleReturnToLobby = async () => {
        void safeExecute(async () => {
            await returnToLobby();
            navigate('/spy/lobby');
        });
    };

    const handlePlayAgain = async () => {
        if (confirm("Почати нову гру з поточними налаштуваннями?")) {
            void safeExecute(async () => {
                await startGame();
            });
        }
    };

    const handleExit = async () => {
        if (confirm("Ви дійсно хочете покинути кімнату?")) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy');
            });
        }
    };

    return (
        <div className="spy-results">
            <div className="spy-results__content">
                <div className="spy-card">
                    {/* Header Section */}
                    <div className="spy-header">
                        <div className="icon-wrapper">
                            {winnerTeam === SpyGameTeam.Spies ? '🥷' : '🕵️'}
                        </div>
                        <h1>Гра завершена!</h1>
                        {winnerTeam && (
                            <p className="winner-text">{TEAM_TEXT[winnerTeam]}</p>
                        )}
                        {gameEndReason && (
                            <p className="reason-text">
                                {END_REASON_TEXT[gameEndReason] || gameEndMessage}
                            </p>
                        )}
                    </div>

                    {/* Results List */}
                    <div className="results-list">
                        {players.map(p => {
                            const isSpy = p.isSpy === true;

                            return (
                                <div
                                    key={p.id}
                                    className={`result-item ${isSpy ? 'is-spy' : 'is-civilian'}`}
                                >
                                    <div className="player-info">
                                        <div className="role-icon">
                                            {isSpy ? '🥷' : '🕵️'}
                                        </div>
                                        <div className="player-name">
                                            {p.name} {p.id === me?.id && '(Ви)'}
                                        </div>
                                    </div>

                                    <div className="role-label">
                                        {isSpy ? 'ШПИГУН' : 'Мирний'}
                                    </div>
                                </div>
                            );
                        })}
                    </div>

                    {/* Buttons */}
                    <div className="spy-actions">
                        {me?.isHost && (
                            <div className="host-actions">
                                <Button fullWidth onClick={handleReturnToLobby} variant="secondary">
                                    🛋️ В лобі (Всіх)
                                </Button>

                                <Button fullWidth onClick={handlePlayAgain}>
                                    🔄 Грати знову
                                </Button>
                            </div>
                        )}

                        <Button
                            fullWidth
                            variant="secondary"
                            onClick={handleExit}
                        >
                            🚪 Покинути кімнату
                        </Button>
                    </div>
                </div>

                <div className="spy-results__chat">
                    <SpyGameChat
                        messages={messages}
                        currentPlayerId={me?.id || ''}
                        onSendMessage={sendMessage}
                    />
                </div>
            </div>
        </div>
    );
};