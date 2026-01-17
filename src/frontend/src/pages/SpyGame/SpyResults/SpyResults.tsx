import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../../components/ui/Button/Button';
import { SpyGameEndReason, SpyGameTeam } from '../../../models/spy-game';
import './SpyResults.scss';
import { useSpyGame } from "../../../context/spy-game/SpyGameContext.tsx";
import { SpyGameChat } from "../SpyGame/SpyGameChat/SpyGameChat.tsx";
import { RoomStatus } from "../../../models/shared.ts";

const END_REASON_TEXT: Record<SpyGameEndReason, string> = {
    [SpyGameEndReason.RoundTimeExpired]: 'Час вийшов! Шпигуни не були знайдені.',
    [SpyGameEndReason.CivilianKicked]: 'Мирного гравця вигнали помилково!',
    [SpyGameEndReason.SpyGuessedWord]: 'Шпигун вгадав секретне слово!',
    [SpyGameEndReason.SpyWrongGuess]: 'Шпигун не вгадав слово!',
    [SpyGameEndReason.FinalVoteFailed]: 'Фінальне голосування провалилося!',
    [SpyGameEndReason.AllSpiesEliminated]: 'Всі шпигуни були вигнані!',
    [SpyGameEndReason.SpyLastChanceFailed]: 'Шпигун був спійманий і не вгадав слово!',
    [SpyGameEndReason.ParanoiaSacrifice]: 'В режимі Параної вигнали невинного!',
    [SpyGameEndReason.ParanoiaSurvived]: 'Мирні вижили в режимі Параної!',
    [SpyGameEndReason.InsufficientPlayers]: 'Недостатньо гравців для продовження гри.',
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
        spiesReveal,
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
        gameState,
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
        } else if (roomState === RoomStatus.InGame) {
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

    const secretWord = gameState?.currentSecretWord;
    const category = gameState?.currentCategory;

    return (
        <div className="spy-results">
            <div className="spy-results__content">
                <div className="spy-results__main">
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

                        {/* Secret Word Section */}
                        {(secretWord || category) && (
                            <div className="secret-info">
                                {category && (
                                    <div className="secret-item">
                                        <span className="secret-label">Категорія:</span>
                                        <span className="secret-value">{category}</span>
                                    </div>
                                )}
                                {secretWord && (
                                    <div className="secret-item">
                                        <span className="secret-label">Секретне слово:</span>
                                        <span className="secret-value secret-word">{secretWord}</span>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Results List */}
                        <div className="results-list">
                            {spiesReveal.map(reveal => {
                                const player = players.find(p => p.id === reveal.playerId);
                                const isOnline = player?.isConnected ?? false;
                                const isMe = me?.id === reveal.playerId;

                                const itemClasses = [
                                    'result-item',
                                    reveal.isSpy ? 'is-spy' : 'is-civilian',
                                    !isOnline ? 'is-offline' : '',
                                    reveal.isDead ? 'is-dead' : ''
                                ].filter(Boolean).join(' ');

                                return (
                                    <div
                                        key={reveal.playerId}
                                        className={itemClasses}
                                    >
                                        <div className="player-info">
                                            <div className="role-icon">
                                                {reveal.isDead ? '💀' : reveal.isSpy ? '🥷' : '🕵️'}
                                            </div>
                                            <div className="player-name">
                                                {reveal.playerName} {isMe && '(Ви)'}
                                                {!isOnline && ' [Офлайн]'}
                                            </div>
                                        </div>

                                        <div className="role-status">
                                            {reveal.isDead && (
                                                <div className="status-badge dead">Вибув</div>
                                            )}
                                            <div className="role-label">
                                                {reveal.isSpy ? 'ШПИГУН' : 'Мирний'}
                                            </div>
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
                                        🛋️ В лобі
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