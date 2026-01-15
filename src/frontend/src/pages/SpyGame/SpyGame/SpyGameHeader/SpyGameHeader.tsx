import { Button } from '../../../../components/ui/Button/Button';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './SpyGameHeader.scss';

interface SpyGameHeaderProps {
    roomCode: string;
    stopAt: string | null;
    isTimerStopped: boolean;
    timerVotesCount: number;
    activePlayers: number;
    hasVoted: boolean;
    onVoteStopTimer: () => void;
}

export const SpyGameHeader = ({
                                  roomCode, stopAt, isTimerStopped, timerVotesCount,
                                  activePlayers, hasVoted, onVoteStopTimer
                              }: SpyGameHeaderProps) => {

    const secondsLeft = useGameTimer(stopAt, isTimerStopped);

    const formatTime = (sec: number) => {
        const m = Math.floor(sec / 60);
        const s = sec % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const required = Math.max(1, Math.ceil(activePlayers / 2.0)); // Simple majority usually

    return (
        <div className="spy-game-header">
            <div className="spy-game-header__timer-section">
                <div className="spy-game-header__timer-wrapper">
                    <div className={`spy-game-header__timer-display ${secondsLeft < 60 && !isTimerStopped ? 'warning' : ''} ${isTimerStopped ? 'paused' : ''}`}>
                        {isTimerStopped ? "PAUSED" : formatTime(secondsLeft)}
                    </div>
                    <div className="spy-game-header__timer-label">
                        {isTimerStopped ? "Таймер зупинено" : "Залишилось часу"}
                    </div>
                </div>

                {!isTimerStopped && (
                    <div className="spy-game-header__vote-controls">
                        <Button size="small" variant="secondary" onClick={onVoteStopTimer} disabled={hasVoted}>
                            {hasVoted ? "Ви проголосували" : "⏸️ Стоп"}
                        </Button>
                        <div className="spy-game-header__vote-info">
                            Голосів: {timerVotesCount} / {required}
                        </div>
                    </div>
                )}
            </div>
            <div className="spy-game-header__room-code">
                КІМНАТА: {roomCode}
            </div>
        </div>
    );
};