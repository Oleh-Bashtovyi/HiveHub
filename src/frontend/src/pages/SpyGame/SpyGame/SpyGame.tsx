import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext';
import { Button } from '../../../components/ui/Button/Button';
import { RoomState } from '../../../models/spy-game';
import { AVATAR_MAP } from '../../../const/avatars';
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
        sendMessage,
        voteStopTimer,
        revealSpies,
        leaveRoom,
        returnToLobby
    } = useSpyGame();

    const [timeLeft, setTimeLeft] = useState(0);
    const [msgText, setMsgText] = useState('');
    const chatEndRef = useRef<HTMLDivElement>(null);

    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: unknown) {
            console.error(error);
            const msg = error instanceof Error ? error.message : 'Unknown error';
            alert(`Помилка: ${msg}`);
        }
    };

    useEffect(() => {
        if (isInitializing) return;

        if (!roomCode) {
            navigate('/spy');
            return;
        }

        if (roomState === RoomState.Lobby) {
            navigate('/spy/lobby');
            return;
        }

        if (roomState === RoomState.Ended) {
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

    // --- Chat Auto-Scroll ---
    useEffect(() => {
        if (gameState?.recentMessages.length) {
            chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
        }
    }, [gameState?.recentMessages]);

    // --- Rendering ---
    if (isInitializing || !gameState || !me) {
        return null;
    }

    const formatTime = (seconds: number) => {
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const handleSend = () => {
        if (!msgText.trim()) return;
        void safeExecute(async () => {
            await sendMessage(msgText);
            setMsgText('');
        });
    };

    const handleVote = () => {
        if (confirm("Зупинити таймер для обговорення?")) {
            void safeExecute(async () => await voteStopTimer());
        }
    };

    const handleLeave = () => {
        if (confirm('Ви впевнені? Це завершить гру для вас.')) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy'); // Force redirect
            });
        }
    };

    const handleAbortGame = () => {
        if (confirm('УВАГА: Це примусово завершить гру для всіх і поверне всіх в лобі. Продовжити?')) {
            void safeExecute(async () => await returnToLobby());
        }
    };

    const isSpyRole = !gameState.currentSecretWord;

    const getVoteString = () => {
        if (!gameState) return "";
        const activePlayers = players.filter(p => p.isConnected).length;
        const required = Math.max(1, Math.ceil(activePlayers / 2.0));
        return `${gameState.timerVotesCount} / ${required}`;
    }

    return (
        <div className="spy-game-page theme-spy">
            <div className="game-container">

                {/* --- HEADER --- */}
                <div className="game-header">
                    <div className="timer-section">
                        <div className="timer-wrapper">
                            <div className={`timer-display ${timeLeft < 60 && !gameState.isTimerStopped ? 'warning' : ''} ${gameState.isTimerStopped ? 'paused' : ''}`}>
                                {gameState.isTimerStopped ? "PAUSED" : formatTime(timeLeft)}
                            </div>
                            <div className="timer-label">
                                {gameState.isTimerStopped ? "Таймер зупинено" : "Залишилось часу"}
                            </div>
                        </div>

                        {!gameState.isTimerStopped && (
                            <div className="vote-controls">
                                <Button
                                    size="small"
                                    variant="secondary"
                                    onClick={handleVote}
                                    title="Голосувати за зупинку таймера"
                                    disabled={me.isVotedToStopTimer}
                                >
                                    {me.isVotedToStopTimer ? "Ви проголосували" : "⏸️ Стоп"}
                                </Button>
                                <div className="vote-info">
                                    Голосів: {getVoteString()}
                                </div>
                            </div>
                        )}

                    </div>
                    <div className="room-code-display">
                        КІМНАТА: {roomCode}
                    </div>
                </div>

                <div className="game-layout">
                    {/* --- LEFT COLUMN: Role & Players --- */}
                    <div className="game-col left-col">
                        <div className={`role-card ${isSpyRole ? '' : 'civilian'}`}>
                            <div className="role-icon">{isSpyRole ? '🥷' : '🕵️'}</div>
                            <div className="role-title">
                                {isSpyRole ? "ВИ ШПИГУН" : "Мирний Житель"}
                            </div>

                            <div className="role-desc">
                                {isSpyRole ? (
                                    <>Ваша ціль: дізнатися слово з розмов інших або протриматися до кінця і не видати себе.</>
                                ) : (
                                    <>Ваша ціль: знайти шпигуна серед гравців, задаючи навідні питання.</>
                                )}
                            </div>

                            {isSpyRole ? (
                                gameState.category && <div className="category-badge">Категорія: {gameState.category}</div>
                            ) : (
                                <>
                                    <div className="secret-word-box">{gameState.currentSecretWord}</div>
                                    <div style={{ fontSize: 14 }}>Категорія: <strong>{gameState.category}</strong></div>
                                </>
                            )}
                        </div>

                        <div className="panel">
                            <h3>Гравці</h3>
                            <div className="player-list-game">
                                {players.map(p => (
                                    <div key={p.id} className="player-row" style={{ opacity: p.isConnected ? 1 : 0.5 }}>
                                        <div className="mini-avatar">
                                            {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                                        </div>
                                        <div className="player-info">
                                            <div className="p-name-row">
                                                <span className="p-name">{p.name} {p.id === me.id && '(Ви)'}</span>
                                                {/* Status Icons */}
                                                {p.isHost && <span title="Хост">👑</span>}
                                                {/* Show voted hand if timer running */}
                                                {!gameState.isTimerStopped && p.isVotedToStopTimer && (
                                                    <span title="Голосував за стоп" className="vote-hand">✋</span>
                                                )}
                                                {/* Show spy icon ONLY if it's me (or teammate if implemented later) */}
                                                {p.isSpy && <span title="Шпигун">🥷</span>}
                                            </div>
                                            {!p.isConnected && <span className="offline-status">🔌 Офлайн</span>}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>

                    {/* --- CENTER COLUMN: Tips & Actions --- */}
                    <div className="game-col center-col">
                        <div className="panel">
                            <h3>💡 Як грати?</h3>
                            <ul className="tips-list">
                                <li><strong>По черзі</strong> задавайте один одному питання про секретне слово.</li>
                                <li>Питання мають бути <strong>не надто прямими</strong>, щоб шпигун не здогадався.</li>
                                <li>Але й <strong>не надто абстрактними</strong>, щоб інші зрозуміли, що ви "свій".</li>
                                <li>Якщо підозрюєте когось — тисніть "Стоп" і голосуйте!</li>
                            </ul>
                        </div>

                        {/* Discussion / Host Actions Panel */}
                        {gameState.isTimerStopped && (
                            <div className="discussion-panel">
                                <h3>📢 Час обговорення!</h3>
                                <p>Таймер зупинено. Обговоріть свої підозри.</p>

                                {me.isHost ? (
                                    <Button
                                        fullWidth
                                        onClick={() => void safeExecute(async () => await revealSpies())}
                                        style={{ marginTop: 10 }}
                                    >
                                        🎭 РОЗКРИТИ ШПИГУНІВ
                                    </Button>
                                ) : (
                                    <div className="host-waiting-msg">Чекаємо рішення Хоста...</div>
                                )}
                            </div>
                        )}

                        <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                            {me.isHost && (
                                <Button
                                    variant="secondary" // Або інший стиль, щоб відрізнявся
                                    fullWidth
                                    onClick={handleAbortGame}
                                    title="Повернути всіх в лобі та скинути гру"
                                >
                                    🛑 Перервати гру (В Лобі)
                                </Button>
                            )}

                            <Button variant="danger" fullWidth onClick={handleLeave}>
                                🚪 Покинути гру
                            </Button>
                        </div>
                    </div>

                    {/* --- RIGHT COLUMN: Chat --- */}
                    <div className="chat-panel panel game-col">
                        <h3>💬 Чат</h3>
                        <div className="chat-messages">
                            {gameState.recentMessages.length === 0 && (
                                <div className="empty-chat-msg">Повідомлень ще немає...</div>
                            )}
                            {gameState.recentMessages.map((msg, idx) => (
                                <div key={idx} className={`chat-msg ${msg.playerId === me.id ? 'mine' : ''}`}>
                                    <div className="msg-header">
                                        <span className="msg-author">{msg.playerName}</span>
                                        <span>{new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                    </div>
                                    <div>{msg.message}</div>
                                </div>
                            ))}
                            <div ref={chatEndRef} />
                        </div>
                        <div className="chat-input-area">
                            <input
                                value={msgText}
                                onChange={e => setMsgText(e.target.value)}
                                onKeyDown={e => e.key === 'Enter' && handleSend()}
                                placeholder="Повідомлення..."
                                maxLength={200}
                            />
                            <Button size="small" onClick={handleSend}>📤</Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};