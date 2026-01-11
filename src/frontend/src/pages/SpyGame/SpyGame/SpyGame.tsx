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
        isInitializing, // Check loading state
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
        // Wait for initialization to finish before redirecting
        if (isInitializing) return;

        if (!roomCode) {
            navigate('/spy');
            return;
        }
        if (roomState === RoomState.Lobby) navigate('/spy/lobby');
        // If Ended, we stay here to show results/overlay or redirect to results page if exists
        // Assuming Logic handles result display within SpyGame or SpyResults
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

    useEffect(() => {
        if (gameState?.recentMessages.length) {
            chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
        }
    }, [gameState?.recentMessages]);

    if (isInitializing || !gameState || !me) {
        return <div className="spy-game-page theme-spy center-msg">🔄 Відновлення з'єднання...</div>;
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
                navigate('/spy');
            });
        }
    };

    const isSpyRole = !gameState.currentSecretWord;
    const isGameEnded = roomState === RoomState.Ended;

    return (
        <div className="spy-game-page theme-spy">
            <div className="game-container">
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

                        {!gameState.isTimerStopped && !isGameEnded && (
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
                                    Голосів: {gameState.timerVotesCount}
                                </div>
                            </div>
                        )}
                    </div>
                    <div className="room-code-display">
                        КІМНАТА: {roomCode}
                    </div>
                </div>

                <div className="game-layout">
                    {/* Left Column */}
                    <div>
                        <div className={`role-card ${isSpyRole ? '' : 'civilian'}`}>
                            <div className="role-icon">{isSpyRole ? '🥷' : '🕵️'}</div>
                            <div className="role-title">
                                {isSpyRole ? "ВИ ШПИГУН" : "Мирний Житель"}
                            </div>
                            <div className="role-desc">
                                {isSpyRole ? "Не видайте себе та вгадайте слово." : "Знайдіть шпигуна."}
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
                                {players.map(p => {
                                    // Show Spy icon if game ended OR explicitly set (e.g. spy teammates)
                                    const showSpyIcon = (isGameEnded || p.isSpy === true) && p.isSpy;

                                    return (
                                        <div key={p.id} className="player-row" style={{ opacity: p.isConnected ? 1 : 0.5 }}>
                                            <div className="mini-avatar">
                                                {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                                            </div>
                                            <div className="player-info">
                                                <div className="p-name-row">
                                                    <span className="p-name">{p.name} {p.id === me.id && '(Ви)'}</span>
                                                    {/* DISPLAY ICONS */}
                                                    {showSpyIcon && <span title="Шпигун">🥷</span>}
                                                    {p.isHost && <span title="Хост">👑</span>}
                                                    {/* Vote Hand Icon */}
                                                    {!isGameEnded && !gameState.isTimerStopped && p.isVotedToStopTimer && (
                                                        <span title="Голосував за стоп" className="vote-hand">✋</span>
                                                    )}
                                                </div>
                                                {!p.isConnected && <span className="offline-status">🔌 Офлайн</span>}
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    </div>

                    {/* Center Column */}
                    <div className="center-column">
                        {gameState.isTimerStopped && !isGameEnded && (
                            <div className="discussion-panel">
                                <h3>📢 Час обговорення!</h3>
                                <p>Таймер зупинено. Хост може розкрити карти.</p>
                                {me.isHost ? (
                                    <Button fullWidth onClick={() => void safeExecute(async () => await revealSpies())} style={{ marginTop: 10 }}>
                                        🎭 РОЗКРИТИ ШПИГУНІВ
                                    </Button>
                                ) : (
                                    <div className="host-waiting-msg">Чекаємо на Хоста...</div>
                                )}
                            </div>
                        )}

                        {isGameEnded && (
                            <div className="discussion-panel ended">
                                <h3>🏁 Гра завершена!</h3>
                                <p>Шпигунами були:</p>
                                <ul style={{listStyle:'none', padding:0}}>
                                    {players.filter(p => p.isSpy).map(s => (
                                        <li key={s.id}>🥷 {s.name}</li>
                                    ))}
                                </ul>
                                {me.isHost && (
                                    <Button fullWidth onClick={() => void safeExecute(async () => await returnToLobby())} style={{ marginTop: 10 }}>
                                        ↩️ До лобі
                                    </Button>
                                )}
                            </div>
                        )}

                        <div style={{ marginTop: 'auto' }}>
                            <Button variant="danger" fullWidth onClick={handleLeave}>🚪 Покинути гру</Button>
                        </div>
                    </div>

                    {/* Right Column: Chat */}
                    <div className="chat-panel panel">
                        <h3>💬 Чат</h3>
                        <div className="chat-messages">
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