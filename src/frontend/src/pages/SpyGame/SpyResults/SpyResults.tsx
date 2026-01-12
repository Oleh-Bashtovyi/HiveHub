import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext';
import { Button } from '../../../components/ui/Button/Button';
import './SpyResults.scss';
import {RoomState} from "../../../models/spy-game.ts";

export const SpyResults = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        gameResultSpies,
        returnToLobby,
        leaveRoom,
        roomState,
        startGame,
        me
    } = useSpyGame();

    useEffect(() => {
        if (!roomCode) navigate('/spy');
    }, [roomCode, navigate]);

    useEffect(() => {
        if (roomState === RoomState.Lobby) {
            navigate('/spy/lobby');
        }
        if (roomState === RoomState.InGame) {
            navigate('/spy/game');
        }
    }, [roomState, navigate]);

    const handleReturnToLobby = async () => {
        await returnToLobby();
    };

    const handlePlayAgain = async () => {
        if (confirm("Почати нову гру з поточними налаштуваннями?")) {
            await startGame();
        }
    };

    const handleExit = async () => {
        if (confirm("Ви дійсно хочете покинути кімнату?")) {
            await leaveRoom();
            navigate('/spy');
        }
    };

    return (
        <div className="spy-page-wrapper">
            <div className="spy-card">

                {/* Header Section */}
                <div className="spy-header">
                    <div className="icon-wrapper">🎭</div>
                    <h1>Гра завершена!</h1>
                    <p>Ось хто ким був у цьому раунді:</p>
                </div>

                {/* Results List */}
                <div className="results-list">
                    {players.map(p => {
                        const isSpy = gameResultSpies.some(s => s.playerId === p.id);

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
                        <div style={{ display: 'flex', gap: '10px', width: '100%', flexDirection: 'column' }}>
                            {/* Кнопка 1: Повернути всіх в лобі */}
                            <Button fullWidth onClick={handleReturnToLobby} variant="secondary">
                                🛋️ В лобі (Всіх)
                            </Button>

                            {/* Кнопка 2: Грати знову (Рестарт) */}
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
    );
};