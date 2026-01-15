import { useState } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import { Modal } from '../../../../components/ui/Modal/Modal';
import { AVAILABLE_AVATARS, AVATAR_MAP } from '../../../../const/avatars';
import type { SpyPlayerDto } from '../../../../models/spy-game';

interface PlayersPanelProps {
    players: SpyPlayerDto[];
    me: SpyPlayerDto;
    isHost: boolean;
    isReady: boolean;
    allReady: boolean;
    onToggleReady: () => void;
    onStartGame: () => void;
    onKickPlayer: (playerId: string) => void;
    onChangeHost: (playerId: string) => void;
    onChangeName: (name: string) => void;
    onChangeAvatar: (avatarId: string) => void;
}

export const PlayersPanel = ({
                                 players,
                                 me,
                                 isHost,
                                 isReady,
                                 allReady,
                                 onToggleReady,
                                 onStartGame,
                                 onKickPlayer,
                                 onChangeHost,
                                 onChangeName,
                                 onChangeAvatar,
                             }: PlayersPanelProps) => {
    const [isProfileModalOpen, setProfileModalOpen] = useState(false);
    const [tempName, setTempName] = useState('');

    const openProfileModal = () => {
        setTempName(me.name);
        setProfileModalOpen(true);
    };

    const handleSaveName = () => {
        if (!tempName.trim()) return alert("Name cannot be empty");
        if (tempName === me.name) return;
        onChangeName(tempName.trim());
    };

    const handleSelectAvatar = (avatarId: string) => {
        if (avatarId === me.avatarId) return;
        onChangeAvatar(avatarId);
    };

    return (
        <div className="section-panel players-panel">
            <div className="section-title">
                👥 Гравці ({players.length})
            </div>

            <div className="player-grid">
                {players.map(p => (
                    <div
                        key={p.id}
                        className={`player-card ${p.isReady ? 'ready' : ''} ${p.isHost ? 'host' : ''}`}
                        style={{ opacity: p.isConnected ? 1 : 0.5 }}
                    >
                        {p.isHost && <div className="host-badge">👑 ХОСТ</div>}

                        {!p.isConnected && (
                            <div title="Гравець втратив з'єднання" className="offline-icon">🔌</div>
                        )}

                        {p.id === me.id && (
                            <button className="edit-profile-btn" onClick={openProfileModal} title="Редагувати профіль">
                                ✏️
                            </button>
                        )}

                        <div className="player-avatar">
                            {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                        </div>

                        <div className="player-name">
                            {p.name} {p.id === me.id && '(Ви)'}
                        </div>

                        {p.isReady ? (
                            <span className="ready-badge">✓ Готовий</span>
                        ) : (
                            <span className="not-ready-text">Не готовий</span>
                        )}

                        {isHost && p.id !== me.id && (
                            <div className="player-actions">
                                <button
                                    className="icon-btn"
                                    title="Вигнати"
                                    onClick={() => onKickPlayer(p.id)}
                                >
                                    🚫
                                </button>
                                <button
                                    className="icon-btn"
                                    title="Передати права"
                                    onClick={() => onChangeHost(p.id)}
                                >
                                    👑
                                </button>
                            </div>
                        )}
                    </div>
                ))}

                {Array.from({ length: Math.max(0, 8 - players.length) }).map((_, i) => (
                    <div key={`empty-${i}`} className="player-card empty-slot">
                        <div className="player-avatar avatar-placeholder">❓</div>
                        <div className="player-name">Очікування...</div>
                    </div>
                ))}
            </div>

            <div className="lobby-actions-area">
                <Button
                    fullWidth
                    variant={isReady ? "danger" : "secondary"}
                    onClick={onToggleReady}
                >
                    {isReady ? "⏸️ Не готовий" : "✓ Я готовий"}
                </Button>

                {isHost && (
                    <Button fullWidth disabled={!allReady} onClick={onStartGame} className="mt-2">
                        🎮 Почати гру
                    </Button>
                )}
                {isHost && !allReady && (
                    <div className="lobby-footer-msg">Всі гравці (мін. 3) мають бути готові</div>
                )}
            </div>

            {/* Profile Edit Modal */}
            <Modal isOpen={isProfileModalOpen} onClose={() => setProfileModalOpen(false)} title="Мій Профіль">
                <div className="profile-modal-content">
                    <div className="form-group">
                        <label>Ваше ім'я</label>
                        <div className="name-edit-row">
                            <div className="input-wrapper">
                                <input
                                    value={tempName}
                                    onChange={(e) => setTempName(e.target.value)}
                                    placeholder="Введіть ім'я"
                                    maxLength={15}
                                />
                            </div>
                            <Button size="small" onClick={handleSaveName}>Зберегти</Button>
                        </div>
                    </div>
                    <div className="avatar-selection-section">
                        <h4>Оберіть аватар</h4>
                        <div className="avatar-grid-select">
                            {AVAILABLE_AVATARS.map(avatarKey => (
                                <div
                                    key={avatarKey}
                                    className={`avatar-option ${me.avatarId === avatarKey ? 'selected' : ''}`}
                                    onClick={() => handleSelectAvatar(avatarKey)}
                                >
                                    {AVATAR_MAP[avatarKey]}
                                </div>
                            ))}
                        </div>
                    </div>
                    <div className="modal-actions">
                        <Button variant="secondary" fullWidth onClick={() => setProfileModalOpen(false)}>
                            Закрити
                        </Button>
                    </div>
                </div>
            </Modal>
        </div>
    );
};