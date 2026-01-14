import { Button } from '../../../../components/ui/Button/Button';
import './SpyGameCenter.scss';

interface SpyGameCenterProps {
    isTimerStopped: boolean;
    isHost: boolean;
    onRevealSpies: () => Promise<void>;
    onAbortGame: () => void;
    onLeaveGame: () => void;
}

export const SpyGameCenter = ({
                                  isTimerStopped,
                                  isHost,
                                  onRevealSpies,
                                  onAbortGame,
                                  onLeaveGame
                              }: SpyGameCenterProps) => {
    const handleReveal = async () => {
        try {
            await onRevealSpies();
        } catch (error) {
            console.error('Failed to reveal spies:', error);
        }
    };

    return (
        <div className="spy-game-center">
            <div className="spy-game-center__tips">
                <h3 className="spy-game-center__title">💡 Як грати?</h3>
                <ul className="spy-game-center__tips-list">
                    <li><strong>По черзі</strong> задавайте один одному питання про секретне слово.</li>
                    <li>Питання мають бути <strong>не надто прямими</strong>, щоб шпигун не здогадався.</li>
                    <li>Але й <strong>не надто абстрактними</strong>, щоб інші зрозуміли, що ви "свій".</li>
                    <li>Якщо підозрюєте когось — тисніть "Стоп" і голосуйте!</li>
                </ul>
            </div>

            {isTimerStopped && (
                <div className="spy-game-center__discussion">
                    <h3 className="spy-game-center__discussion-title">📢 Час обговорення!</h3>
                    <p className="spy-game-center__discussion-text">Таймер зупинено. Обговоріть свої підозри.</p>

                    {isHost ? (
                        <Button
                            fullWidth
                            onClick={handleReveal}
                            style={{ marginTop: 10 }}
                        >
                            🎭 РОЗКРИТИ ШПИГУНІВ
                        </Button>
                    ) : (
                        <div className="spy-game-center__waiting">Чекаємо рішення Хоста...</div>
                    )}
                </div>
            )}

            <div className="spy-game-center__actions">
                {isHost && (
                    <Button
                        variant="secondary"
                        fullWidth
                        onClick={onAbortGame}
                        title="Повернути всіх в лобі та скинути гру"
                    >
                        🛑 Перервати гру (В Лобі)
                    </Button>
                )}

                <Button variant="danger" fullWidth onClick={onLeaveGame}>
                    🚪 Покинути гру
                </Button>
            </div>
        </div>
    );
};