import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext';
import { Button } from '../../../components/ui/Button/Button';
import { Modal } from '../../../components/ui/Modal/Modal';
import './SpyEntry.scss';
import {RoomState} from "../../../models/spy-game.ts";

export const SpyEntry = () => {
    const navigate = useNavigate();
    const { isConnected, roomState, createRoom, joinRoom, roomCode } = useSpyGame();

    // UI State
    const [isJoinModalOpen, setJoinModalOpen] = useState(false);
    const [isLoading, setIsLoading] = useState(false);

    // Form Data
    const [joinCode, setJoinCode] = useState('');

    useEffect(() => {
        if (roomCode) {
            if (roomState === RoomState.InGame) navigate('/spy/game');
            else navigate('/spy/lobby');
        }
    }, [roomCode, roomState, navigate]);

    const handleCreateRoom = async () => {
        setIsLoading(true);
        try {
            await createRoom('');
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Невідома помилка';
            alert("Помилка створення кімнати: " + message);
        } finally {
            setIsLoading(false);
        }
    };

    const handleJoinRoom = async () => {
        if (!joinCode.trim() || joinCode.length < 6) {
            alert("Введіть коректний код кімнати");
            return;
        }

        setIsLoading(true);
        try {
            await joinRoom(joinCode.toUpperCase());
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Невідома помилка';
            alert("Помилка входу: " + message);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="spy-entry-page theme-spy">
            <div className="spy-card">
                <div className="spy-logo">
                    <div className="spy-logo-icon">🕵️</div>
                    <h1 className="spy-title">Знайди Шпигуна</h1>
                    <p className="spy-subtitle">Хто з вас зрадник? Вичисліть його!</p>
                </div>

                {!isConnected && (
                    <div className="connection-status">
                        ⏳ Підключення до сервера...
                    </div>
                )}

                <div className="btn-group">
                    <Button
                        fullWidth
                        onClick={handleCreateRoom}
                        disabled={!isConnected}
                        isLoading={isLoading && !isJoinModalOpen}
                    >
                        Створити кімнату
                    </Button>
                    <Button
                        variant="secondary"
                        fullWidth
                        onClick={() => setJoinModalOpen(true)}
                        disabled={!isConnected}
                    >
                        Приєднатися до гри
                    </Button>
                </div>

                <div className="features">
                    <div className="feature-item">
                        <div className="feature-icon">👥</div>
                        <span>3-8 гравців</span>
                    </div>
                    <div className="feature-item">
                        <div className="feature-icon">⏱️</div>
                        <span>5-30 хвилин гри</span>
                    </div>
                    <div className="feature-item">
                        <div className="feature-icon">🎮</div>
                        <span>Без реєстрації</span>
                    </div>
                </div>

                <div className="back-link">
                    <Link to="/">← Назад до HiveHub</Link>
                </div>
            </div>

            {/* Join Room Modal */}
            <Modal
                isOpen={isJoinModalOpen}
                onClose={() => setJoinModalOpen(false)}
                title="Приєднатися до гри"
            >
                <p className="modal-description">
                    Введіть код кімнати, який надав вам хост гри.
                </p>
                <div className="input-group">
                    <label>Код кімнати</label>
                    <input
                        className="uppercase"
                        value={joinCode}
                        onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                        placeholder="ABC12345"
                        maxLength={8}
                        autoFocus
                    />
                </div>
                <Button fullWidth onClick={handleJoinRoom} isLoading={isLoading}>
                    Приєднатися
                </Button>
            </Modal>
        </div>
    );
};
