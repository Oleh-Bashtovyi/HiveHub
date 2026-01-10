/*
import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext.tsx';
import { Button } from '../../../components/ui/Button/Button.tsx';
import { RoomState } from '../../../models/spy-game.ts';
import './SpyGame.scss';

export const SpyGame = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        me,
        gameState,
        roomState,
        sendMessage,
        voteStopTimer,
        revealSpies,
        leaveRoom
    } = useSpyGame();

    const [timeLeft, setTimeLeft] = useState(0);
    const [msgText, setMsgText] = useState('');
    const chatEndRef = useRef<HTMLDivElement>(null);

    // Redirect if invalid state
    useEffect(() => {
        if (!roomCode) navigate('/spy');
        if (roomState === RoomState.Ended) navigate('/spy/results');
        if (roomState === RoomState.Lobby) navigate('/spy/lobby');
    }, [roomCode, roomState, navigate]);

    // Timer Logic
    useEffect(() => {
        if (!gameState?.gameEndTime || gameState.isTimerStopped) return;

        const interval = setInterval(() => {
            const end = new Date(gameState.gameEndTime!).getTime();
            const now = new Date().getTime();
            const diff = Math.floor((end - now) / 1000);

            if (diff <= 0) {
                setTimeLeft(0);
                clearInterval(interval);
            } else {
                setTimeLeft(diff);
            }
        }, 1000);

        return () => clearInterval(interval);
    }, [gameState?.gameEndTime, gameState?.isTimerStopped]);

    // Auto-scroll chat
    useEffect(() => {
        chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [gameState?.recentMessages]);

    if (!gameState || !me) return <div>Loading Game...</div>;

    const formatTime = (seconds: number) => {
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const handleSend = async () => {
        if (!msgText.trim()) return;
        await sendMessage(msgText);
        setMsgText('');
    };

    const handleVote = async () => {
        await voteStopTimer();
    };

    const isSpy = !gameState.currentSecretWord; // If no word, you are spy (usually)
    // OR check based on role logic if backend sends "IsSpy" flag explicitly.
    // In our DTO `GameStartedEvent` had `isSpy`, but `GameState` has `currentSecretWord` null for spy.
    // Let's rely on secretWord being null.

    return (
        <div className="spy-game-page theme-spy">
            <div className="game-container">
                {/!* Header *!/}
                <div className="game-header">
                    <div className="timer-section">
                        <div>
                            <div className={`timer-display ${timeLeft < 60 && !gameState.isTimerStopped ? 'warning' : ''}`}>
                                {gameState.isTimerStopped ? "PAUSED" : formatTime(timeLeft)}
                            </div>
                            <div style={{fontSize: 12, color: '#888'}}>
                                {gameState.isTimerStopped ? "Таймер зупинено" : "Залишилось часу"}
                            </div>
                        </div>
                        <div>
                            {!gameState.isTimerStopped && (
                                <>
                                    <Button
                                        size="small"
                                        variant="secondary"
                                        onClick={handleVote}
                                        // Disable if I already voted? Backend should handle,
                                        // but for UI feedback we'd need "hasVoted" in DTO.
                                    >
                                        ⏸️ Стоп таймер
                                    </Button>
                                    <div className="vote-info">
                                        Голосів: {gameState.timerVotesCount}/2
                                    </div>
                                </>
                            )}
                        </div>
                    </div>
                    <div style={{fontWeight: 'bold', fontSize: 20}}>
                        {roomCode}
                    </div>
                </div>

                <div className="game-layout">
                    {/!* Left Panel: Role & Info *!/}
                    <div>
                        <div className={`role-card ${isSpy ? '' : 'civilian'}`}>
                            <div className="role-icon">{isSpy ? '🥷' : '🕵️'}</div>
                            <div className="role-title">
                                {isSpy ? "ВИ ШПИГУН" : "Мирний Житель"}
                            </div>

                            {isSpy ? (
                                <div style={{fontSize: 14, marginTop: 15, opacity: 0.9}}>
                                    Ваша ціль: відгадати слово та не видати себе.
                                    {gameState.category && (
                                        <div style={{marginTop: 10, fontWeight: 'bold'}}>
                                            Категорія: {gameState.category}
                                        </div>
                                    )}
                                </div>
                            ) : (
                                <>
                                    <div className="secret-word-box">
                                        {gameState.currentSecretWord}
                                    </div>
                                    <div style={{marginTop: 10, fontSize: 14}}>
                                        Категорія: {gameState.category}
                                    </div>
                                </>
                            )}
                        </div>

                        <div className="panel" style={{background: '#1A1A20', padding: 20, borderRadius: 15}}>
                            <h3 style={{marginBottom: 15}}>Гравці</h3>
                            <div className="player-list-game">
                                {players.map(p => (
                                    <div key={p.id} className="player-row">
                                        <div className="mini-avatar">{p.avatarId || '👤'}</div>
                                        <div style={{fontWeight: 600}}>
                                            {p.name} {p.id === me.id && '(Ви)'}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>

                    {/!* Center Panel: Actions & Tips *!/}
                    <div style={{display: 'flex', flexDirection: 'column', gap: 20}}>
                        <div className="panel" style={{background: '#1A1A20', padding: 20, borderRadius: 15}}>
                            <h3>💡 Підказки</h3>
                            <ul style={{paddingLeft: 20, color: '#B0B0B0', lineHeight: 1.5}}>
                                <li>Ставте питання по черзі.</li>
                                <li>Не будьте занадто конкретними.</li>
                                <li>Шпигун не знає слова, але знає категорію (можливо).</li>
                            </ul>
                        </div>

                        {gameState.isTimerStopped && (
                            <div className="panel" style={{background: 'rgba(76, 175, 80, 0.1)', border: '1px solid #4CAF50', padding: 20, borderRadius: 15, textAlign: 'center'}}>
                                <h3 style={{color: '#4CAF50', marginBottom: 10}}>Обговорення!</h3>
                                <p style={{marginBottom: 20}}>Таймер зупинено. Визначте шпигуна.</p>
                                <Button fullWidth onClick={revealSpies} disabled={!me.isHost}>
                                    🎭 Показати шпигунів (Хост)
                                </Button>
                            </div>
                        )}

                        <div style={{marginTop: 'auto'}}>
                            <Button variant="danger" fullWidth onClick={() => {
                                if(confirm('Вийти?')) leaveRoom();
                            }}>
                                🚪 Покинути гру
                            </Button>
                        </div>
                    </div>

                    {/!* Right Panel: Chat *!/}
                    <div className="chat-panel">
                        <h3 style={{marginBottom: 10}}>Чат</h3>
                        <div className="chat-messages">
                            {gameState.recentMessages.map((msg, idx) => (
                                <div key={idx} className={`chat-msg ${msg.playerId === me.id ? 'mine' : ''}`}>
                                    <div className="msg-header">
                                        <span>{msg.playerName}</span>
                                        <span>{new Date(msg.timestamp).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
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
};*/
