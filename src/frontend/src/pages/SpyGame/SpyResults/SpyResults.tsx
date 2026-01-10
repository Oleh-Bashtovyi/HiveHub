/*
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../context/SpyGameContext';
import { Button } from '../../components/ui/Button/Button';
import './SpyResults.scss'; // Assuming simple styles similar to SpyEntry

export const SpyResults = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        me,
        gameResultSpies,
        returnToLobby,
        leaveRoom
    } = useSpyGame();

    useEffect(() => {
        if (!roomCode) navigate('/spy');
    }, [roomCode, navigate]);

    const handlePlayAgain = async () => {
        await returnToLobby();
        navigate('/spy/lobby');
    };

    const handleExit = async () => {
        await leaveRoom();
        navigate('/spy');
    };

    return (
        <div className="spy-game-page theme-spy" style={{display: 'flex', alignItems: 'center', justifyContent: 'center'}}>
            <div style={{
                background: '#1A1A20',
                padding: 40,
                borderRadius: 20,
                maxWidth: 600,
                width: '100%',
                border: '1px solid #E53935'
            }}>
                <div style={{textAlign: 'center', marginBottom: 30}}>
                    <div style={{fontSize: 60, marginBottom: 10}}>🎭</div>
                    <h1>Гра завершена!</h1>
                    <p style={{color: '#B0B0B0'}}>Ось хто ким був:</p>
                </div>

                <div style={{display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 30}}>
                    {players.map(p => {
                        const isSpy = gameResultSpies.some(s => s.playerId === p.id);
                        return (
                            <div key={p.id} style={{
                                background: isSpy ? 'rgba(229, 57, 53, 0.2)' : '#25252D',
                                border: isSpy ? '1px solid #E53935' : 'none',
                                padding: 15,
                                borderRadius: 10,
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'space-between'
                            }}>
                                <div style={{display: 'flex', alignItems: 'center', gap: 10}}>
                                    <div style={{fontSize: 24}}>{isSpy ? '🥷' : '🕵️'}</div>
                                    <div style={{fontWeight: 'bold'}}>{p.name}</div>
                                </div>
                                <div style={{
                                    color: isSpy ? '#E53935' : '#4CAF50',
                                    fontWeight: 'bold'
                                }}>
                                    {isSpy ? 'ШПИГУН' : 'Мирний'}
                                </div>
                            </div>
                        );
                    })}
                </div>

                <div style={{display: 'flex', gap: 15}}>
                    <Button fullWidth onClick={handlePlayAgain}>
                        🔄 Грати знову
                    </Button>
                    <Button fullWidth variant="secondary" onClick={handleExit}>
                        🚪 Вихід
                    </Button>
                </div>
            </div>
        </div>
    );
};*/
