/*
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../context/SpyGameContext';
import { Button } from '../../components/ui/Button/Button';
import {type RoomGameSettingsDto, RoomState, WordsCategoryDto } from '../../models/spy-game';
import './SpyLobby.scss';

export const SpyLobby = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        me,
        players,
        settings,
        roomState,
        leaveRoom,
        toggleReady,
        updateSettings,
        startGame,
        kickPlayer,
        changeHost
    } = useSpyGame();

    // Local state for settings to avoid jitter, though usually direct update is fine with SignalR
    // We will use direct update for simplicity as SignalR is fast.

    useEffect(() => {
        // Guard: if no room code, go back
        if (!roomCode) {
            navigate('/spy');
            return;
        }

        // If game started, navigate to game
        if (roomState === RoomState.InGame) {
            navigate('/spy/game');
        }
    }, [roomCode, roomState, navigate]);

    if (!roomCode || !settings || !me) return <div>Loading Lobby...</div>;

    const copyCode = () => {
        navigator.clipboard.writeText(roomCode);
        alert('Код скопійовано!');
    };

    const handleLeave = async () => {
        if (confirm('Вийти з кімнати?')) {
            await leaveRoom();
            navigate('/spy');
        }
    };

    const handleStart = async () => {
        // Validate
        const readyCount = players.filter(p => p.isReady).length;
        if (readyCount < players.length && players.length > 1) {
            // Usually all must be ready, logic depends on your rules.
            // Let's assume all must be ready.
        }
        await startGame();
    };

    // --- Settings Helpers (Only for Host) ---
    const updateSetting = (key: keyof RoomGameSettingsDto, value: any) => {
        if (!me.isHost) return;
        const newSettings = { ...settings, [key]: value };
        updateSettings(newSettings);
    };

    const modifyNumber = (key: 'timerMinutes' | 'spiesCount', delta: number, min: number, max: number) => {
        if (!me.isHost) return;
        const current = settings[key];
        const next = Math.max(min, Math.min(max, current + delta));
        if (next !== current) {
            updateSetting(key, next);
        }
    };

    const removeCategory = (nameToRemove: string) => {
        if (!me.isHost) return;
        const newCats = settings.wordsCategories.filter(c => c.name !== nameToRemove);
        updateSetting('wordsCategories', newCats);
    };

    // Check readiness for start button
    const allReady = players.length >= 3 && players.every(p => p.isReady);
    // Logic: usually min 3 players for Spy

    return (
        <div className="spy-lobby-page theme-spy">
            <div className="lobby-container">
                {/!* Header *!/}
                <div className="lobby-header">
                    <div className="room-code-group">
                        <h2>Кімната</h2>
                        <div className="code-badge" onClick={copyCode} title="Клікніть, щоб скопіювати">
                            {roomCode}
                        </div>
                    </div>
                    <Button variant="danger" onClick={handleLeave} size="small">
                        Вийти
                    </Button>
                </div>

                <div className="lobby-content">
                    {/!* Players Section *!/}
                    <div className="section-panel">
                        <div className="section-title">
                            👥 Гравці ({players.length})
                        </div>
                        <div className="player-grid">
                            {players.map(p => (
                                <div key={p.id} className={`player-card ${p.isReady ? 'ready' : ''} ${p.isHost ? 'host' : ''}`}>
                                    {p.isHost && <div className="host-badge">👑 ХОСТ</div>}
                                    <div className="player-avatar">
                                        {/!* Avatar placeholder - maybe map avatarId to emoji later *!/}
                                        {p.avatarId || '😎'}
                                    </div>
                                    <div className="player-name">
                                        {p.name} {p.id === me.id && '(Ви)'}
                                    </div>

                                    {p.isReady ? (
                                        <span className="ready-badge">✓ Готовий</span>
                                    ) : (
                                        <span className="not-ready-text">Не готовий</span>
                                    )}

                                    {/!* Host Actions against other players *!/}
                                    {me.isHost && p.id !== me.id && (
                                        <div className="player-actions">
                                            <button className="icon-btn" title="Вигнати" onClick={() => kickPlayer(p.id)}>🚫</button>
                                            <button className="icon-btn" title="Передати права" onClick={() => changeHost(p.id)}>👑</button>
                                        </div>
                                    )}
                                </div>
                            ))}

                            {/!* Empty slots visual fillers (optional) *!/}
                            {Array.from({ length: Math.max(0, 8 - players.length) }).map((_, i) => (
                                <div key={`empty-${i}`} className="player-card" style={{ opacity: 0.3, border: '2px dashed #444', background: 'transparent' }}>
                                    <div className="player-avatar" style={{background: '#333'}}>❓</div>
                                    <div className="player-name">Очікування...</div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/!* Settings Section *!/}
                    <div className="section-panel">
                        <div className="section-title">⚙️ Налаштування</div>

                        <div className="settings-list">
                            <div className="setting-item">
                                <span>Час гри (хв)</span>
                                <div className="setting-control">
                                    <button className="btn-mini" onClick={() => modifyNumber('timerMinutes', -1, 1, 30)} disabled={!me.isHost}>-</button>
                                    <span className="val-display">{settings.timerMinutes}</span>
                                    <button className="btn-mini" onClick={() => modifyNumber('timerMinutes', 1, 1, 30)} disabled={!me.isHost}>+</button>
                                </div>
                            </div>

                            <div className="setting-item">
                                <span>Кількість шпигунів</span>
                                <div className="setting-control">
                                    <button className="btn-mini" onClick={() => modifyNumber('spiesCount', -1, 1, 3)} disabled={!me.isHost}>-</button>
                                    <span className="val-display">{settings.spiesCount}</span>
                                    <button className="btn-mini" onClick={() => modifyNumber('spiesCount', 1, 1, 3)} disabled={!me.isHost}>+</button>
                                </div>
                            </div>

                            <div className="setting-item">
                                <span>Шпигуни знають один одного</span>
                                <label className="switch">
                                    <input
                                        type="checkbox"
                                        checked={settings.spiesKnowEachOther}
                                        onChange={(e) => updateSetting('spiesKnowEachOther', e.target.checked)}
                                        disabled={!me.isHost || settings.spiesCount < 2}
                                    />
                                    <span className="slider"></span>
                                </label>
                            </div>

                            <div className="setting-item">
                                <span>Показувати категорію шпигунам</span>
                                <label className="switch">
                                    <input
                                        type="checkbox"
                                        checked={settings.showCategoryToSpy}
                                        onChange={(e) => updateSetting('showCategoryToSpy', e.target.checked)}
                                        disabled={!me.isHost}
                                    />
                                    <span className="slider"></span>
                                </label>
                            </div>

                            <div style={{marginTop: 10}}>
                                <div style={{display: 'flex', justifyContent: 'space-between'}}>
                                    <span>📚 Категорії слів</span>
                                </div>
                                <div className="category-list">
                                    {settings.wordsCategories.map((cat, idx) => (
                                        <div key={idx} className="category-item">
                                            <span>{cat.name} ({cat.words.length})</span>
                                            {me.isHost && (
                                                <button
                                                    style={{background:'none', border:'none', color:'#E53935', cursor:'pointer'}}
                                                    onClick={() => removeCategory(cat.name)}
                                                >✕</button>
                                            )}
                                        </div>
                                    ))}
                                    {settings.wordsCategories.length === 0 && (
                                        <div style={{color:'#666', fontStyle:'italic', padding: 5}}>Немає категорій</div>
                                    )}
                                </div>
                                {/!* Add Category Button Placeholder *!/}
                                {me.isHost && (
                                    <Button size="small" variant="secondary" fullWidth style={{marginTop: 10, borderStyle: 'dashed'}}>
                                        + Додати категорію
                                    </Button>
                                )}
                            </div>
                        </div>

                        <div className="lobby-footer">
                            <Button
                                fullWidth
                                variant={me.isReady ? "danger" : "secondary"}
                                onClick={toggleReady}
                            >
                                {me.isReady ? "⏸️ Не готовий" : "✓ Я готовий"}
                            </Button>

                            {me.isHost && (
                                <Button
                                    fullWidth
                                    disabled={!allReady}
                                    onClick={handleStart}
                                >
                                    🎮 Почати гру
                                </Button>
                            )}
                            {me.isHost && !allReady && (
                                <div style={{textAlign: 'center', fontSize: 12, color: '#666'}}>
                                    Всі гравці (мін. 3) мають бути готові
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};*/
