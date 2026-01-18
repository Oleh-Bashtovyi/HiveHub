import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useSpyGame } from '../../../context/spy-game/SpyGameContext.tsx';
import { Button } from '../../../components/ui/Button/Button';
import { Modal } from '../../../components/ui/Modal/Modal';
import './SpyEntry.scss';
import {RoomStatus} from "../../../models/shared.ts";
import { en } from '../../../const/localization/en';

export const SpyEntry = () => {
    const navigate = useNavigate();
    const { isConnected, roomState, createRoom, joinRoom, roomCode } = useSpyGame();

    const [isJoinModalOpen, setJoinModalOpen] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const [joinCode, setJoinCode] = useState('');

    const t = en.spyGame.entry;

    useEffect(() => {
        if (roomCode) {
            if (roomState === RoomStatus.InGame) navigate('/spy/game');
            else navigate('/spy/lobby');
        }
    }, [roomCode, roomState, navigate]);

    const handleCreateRoom = async () => {
        setIsLoading(true);
        try {
            await createRoom();
        } catch (error) {
            const message = error instanceof Error ? error.message : t.errors.unknownError;
            alert(t.errors.createRoom + message);
        } finally {
            setIsLoading(false);
        }
    };

    const handleJoinRoom = async () => {
        if (!joinCode.trim() || joinCode.length < 6) {
            alert(t.errors.invalidCode);
            return;
        }

        setIsLoading(true);
        try {
            await joinRoom(joinCode.toUpperCase());
        } catch (error) {
            const message = error instanceof Error ? error.message : t.errors.unknownError;
            alert(t.errors.joinRoom + message);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="spy-entry-page theme-spy">
            <div className="spy-card">
                <div className="spy-logo">
                    <div className="spy-logo-icon">🕵️</div>
                    <h1 className="spy-title">{t.title}</h1>
                    <p className="spy-subtitle">{t.subtitle}</p>
                </div>

                {!isConnected && (
                    <div className="connection-status">
                        {t.connectingToServer}
                    </div>
                )}

                <div className="btn-group">
                    <Button
                        fullWidth
                        onClick={handleCreateRoom}
                        disabled={!isConnected}
                        isLoading={isLoading && !isJoinModalOpen}
                    >
                        {t.createRoom}
                    </Button>
                    <Button
                        variant="secondary"
                        fullWidth
                        onClick={() => setJoinModalOpen(true)}
                        disabled={!isConnected}
                    >
                        {t.joinGame}
                    </Button>
                </div>

                <div className="features">
                    <div className="feature-item">
                        <div className="feature-icon">👥</div>
                        <span>{t.features.players}</span>
                    </div>
                    <div className="feature-item">
                        <div className="feature-icon">⏱️</div>
                        <span>{t.features.duration}</span>
                    </div>
                    <div className="feature-item">
                        <div className="feature-icon">🎮</div>
                        <span>{t.features.noRegistration}</span>
                    </div>
                </div>

                <div className="back-link">
                    <Link to="/">{t.backToHiveHub}</Link>
                </div>
            </div>

            <Modal
                isOpen={isJoinModalOpen}
                onClose={() => setJoinModalOpen(false)}
                title={t.joinModal.title}
            >
                <p className="modal-description">
                    {t.joinModal.description}
                </p>
                <div className="input-group">
                    <label>{t.joinModal.roomCodeLabel}</label>
                    <input
                        className="uppercase"
                        value={joinCode}
                        onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                        placeholder={t.joinModal.roomCodePlaceholder}
                        maxLength={8}
                        autoFocus
                    />
                </div>
                <Button fullWidth onClick={handleJoinRoom} isLoading={isLoading}>
                    {t.joinModal.joinButton}
                </Button>
            </Modal>
        </div>
    );
};