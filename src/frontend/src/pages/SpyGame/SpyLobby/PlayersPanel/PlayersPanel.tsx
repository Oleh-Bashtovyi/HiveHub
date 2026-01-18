import { useState } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import { Modal } from '../../../../components/ui/Modal/Modal';
import { AVAILABLE_AVATARS, AVATAR_MAP } from '../../../../const/avatars';
import type { SpyPlayerDto } from '../../../../models/spy-game';
import './PlayersPanel.scss';
import { en } from '../../../../const/localization/en';

interface PlayersPanelProps {
    players: SpyPlayerDto[];
    me: SpyPlayerDto;
    maxPlayersCount: number;
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
                                 maxPlayersCount,
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

    const t = en.spyGame.players;

    const openProfileModal = () => {
        setTempName(me.name);
        setProfileModalOpen(true);
    };

    const handleSaveName = () => {
        if (!tempName.trim()) return alert(t.profile.errors.emptyName);
        if (tempName === me.name) return;
        onChangeName(tempName.trim());
    };

    const handleSelectAvatar = (avatarId: string) => {
        if (avatarId === me.avatarId) return;
        onChangeAvatar(avatarId);
    };

    const emptySlots = Math.max(0, maxPlayersCount - players.length);

    return (
        <div className="section-panel players-panel">
            <div className="section-title">
                {t.title} ({players.length}/{maxPlayersCount})
            </div>

            <div className="player-grid">
                {players.map(p => (
                    <div
                        key={p.id}
                        className={`player-card ${p.isReady ? 'ready' : ''} ${p.isHost ? 'host' : ''} ${!p.isConnected ? 'disconnected' : ''}`}
                    >
                        {p.isHost && <div className="host-badge">{t.hostBadge}</div>}

                        {!p.isConnected && (
                            <>
                                <div className="offline-icon">🔌</div>
                                <div className="offline-tooltip">{t.connectionLost}</div>
                            </>
                        )}

                        {p.id === me.id && (
                            <button className="edit-profile-btn" onClick={openProfileModal}>
                                ✏️
                            </button>
                        )}

                        <div className="player-avatar">
                            {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                        </div>

                        <div className="player-name">
                            {p.name} {p.id === me.id && t.you}
                        </div>

                        {p.isReady ? (
                            <span className="ready-badge">{t.ready}</span>
                        ) : (
                            <span className="not-ready-text">{t.notReady}</span>
                        )}

                        {isHost && p.id !== me.id && (
                            <div className="player-actions">
                                <button
                                    className="icon-btn"
                                    onClick={() => onKickPlayer(p.id)}
                                >
                                    {t.kick}
                                </button>
                                <button
                                    className="icon-btn"
                                    onClick={() => onChangeHost(p.id)}
                                >
                                    {t.makeHost}
                                </button>
                            </div>
                        )}
                    </div>
                ))}

                {Array.from({ length: emptySlots }).map((_, i) => (
                    <div key={`empty-${i}`} className="player-card empty-slot">
                        <div className="player-avatar avatar-placeholder">❓</div>
                        <div className="player-name">{t.waitingForPlayers}</div>
                    </div>
                ))}
            </div>

            <div className="lobby-actions-area">
                <Button
                    fullWidth
                    variant={isReady ? "danger" : "secondary"}
                    onClick={onToggleReady}
                >
                    {isReady ? t.actions.notReady : t.actions.ready}
                </Button>

                {isHost && (
                    <Button fullWidth disabled={!allReady} onClick={onStartGame} className="mt-2">
                        {t.actions.startGame}
                    </Button>
                )}
                {isHost && !allReady && (
                    <div className="lobby-footer-msg">{t.actions.allPlayersMustBeReady}</div>
                )}
            </div>

            <Modal isOpen={isProfileModalOpen} onClose={() => setProfileModalOpen(false)} title={t.profile.title}>
                <div className="profile-modal-content">
                    <div className="form-group">
                        <label>{t.profile.yourName}</label>
                        <div className="name-edit-row">
                            <div className="input-wrapper">
                                <input
                                    value={tempName}
                                    onChange={(e) => setTempName(e.target.value)}
                                    placeholder={t.profile.namePlaceholder}
                                    maxLength={50}
                                />
                            </div>
                            <Button size="small" onClick={handleSaveName}>{t.profile.save}</Button>
                        </div>
                    </div>
                    <div className="avatar-selection-section">
                        <h4>{t.profile.selectAvatar}</h4>
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
                            {t.profile.close}
                        </Button>
                    </div>
                </div>
            </Modal>
        </div>
    );
};