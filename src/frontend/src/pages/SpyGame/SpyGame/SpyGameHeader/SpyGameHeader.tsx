import { Button } from '../../../../components/ui/Button/Button';
import './SpyGameHeader.scss';

interface SpyGameHeaderProps {
    roomCode: string;
    timeLeft: number;
    isTimerStopped: boolean;
    timerVotesCount: number;
    activePlayers: number;
    hasVoted: boolean | undefined;
    onVoteStopTimer: () => void;
}

export const SpyGameHeader = ({
                                  roomCode,
                                  timeLeft,
                                  isTimerStopped,
                                  timerVotesCount,
                                  activePlayers,
                                  hasVoted,
                                  onVoteStopTimer
                              }: SpyGameHeaderProps) => {
    const formatTime = (seconds: number) => {
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const getVoteString = () => {
        const required = Math.max(1, Math.ceil(activePlayers / 2.0));
        return `${timerVotesCount} / ${required}`;
    };

    return (
        <div className="spy-game-header">
            <div className="spy-game-header__timer-section">
                <div className="spy-game-header__timer-wrapper">
                    <div className={`spy-game-header__timer-display ${timeLeft < 60 && !isTimerStopped ? 'spy-game-header__timer-display--warning' : ''} ${isTimerStopped ? 'spy-game-header__timer-display--paused' : ''}`}>
                        {isTimerStopped ? "PAUSED" : formatTime(timeLeft)}
                    </div>
                    <div className="spy-game-header__timer-label">
                        {isTimerStopped ? "Таймер зупинено" : "Залишилось часу"}
                    </div>
                </div>

                {!isTimerStopped && (
                    <div className="spy-game-header__vote-controls">
                        <Button
                            size="small"
                            variant="secondary"
                            onClick={onVoteStopTimer}
                            title="Голосувати за зупинку таймера"
                            disabled={hasVoted}
                        >
                            {hasVoted ? "Ви проголосували" : "⏸️ Стоп"}
                        </Button>
                        <div className="spy-game-header__vote-info">
                            Голосів: {getVoteString()}
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