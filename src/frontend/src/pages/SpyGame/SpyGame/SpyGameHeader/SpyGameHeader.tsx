import { useEffect, useState } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import { TimerStatus } from '../../../../models/spy-game';
import './SpyGameHeader.scss';

interface SpyGameHeaderProps {
    roomCode: string;
    remainingSeconds: number;
    timerStatus: TimerStatus;
    playersVoted: number;
    votesRequired: number;
    hasVoted: boolean;
    onVoteStopTimer: () => void;
}

export const SpyGameHeader = ({
                                  roomCode,
                                  remainingSeconds,
                                  timerStatus,
                                  playersVoted,
                                  votesRequired,
                                  hasVoted,
                                  onVoteStopTimer
                              }: SpyGameHeaderProps) => {
    const [localSeconds, setLocalSeconds] = useState(remainingSeconds);

    // Sync with backend time whenever it pushes an update
    useEffect(() => {
        setLocalSeconds(remainingSeconds);
    }, [remainingSeconds]);

    // Local countdown
    useEffect(() => {
        if (timerStatus !== TimerStatus.Running) return;

        const interval = setInterval(() => {
            setLocalSeconds((prev: number) => Math.max(0, prev - 1));
        }, 1000);

        return () => clearInterval(interval);
    }, [timerStatus]);

    const formatTime = (sec: number) => {
        const val = Math.floor(sec);
        const m = Math.floor(val / 60);
        const s = val % 60;
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    };

    const isRunning = timerStatus === TimerStatus.Running;
    const isPaused = timerStatus === TimerStatus.Paused;
    const isStopped = timerStatus === TimerStatus.Stopped;
    const isExpired = timerStatus === 'Expired'; // Check against string literal if needed or enum

    const showWarning = localSeconds < 60 && isRunning;

    let statusText = formatTime(localSeconds);
    if (isPaused) statusText = "ПАУЗА";
    if (isStopped) statusText = "ЗУПИНЕНО";
    if (isExpired) statusText = "00:00";

    return (
        <div className="spy-game-header">
            <div className="spy-game-header__timer-section">
                <div className="spy-game-header__timer-wrapper">
                    <div className={`spy-game-header__timer-display ${showWarning ? 'warning' : ''} ${!isRunning ? 'paused' : ''}`}>
                        {statusText}
                    </div>
                    <div className="spy-game-header__timer-label">
                        {isStopped || isPaused ? "Таймер зупинено" : "Залишилось часу"}
                    </div>
                </div>

                {isRunning && (
                    <div className="spy-game-header__vote-controls">
                        <Button size="small" variant="secondary" onClick={onVoteStopTimer} disabled={hasVoted}>
                            {hasVoted ? "Ви проголосували" : "⏸️ Стоп"}
                        </Button>
                        <div className="spy-game-header__vote-info">
                            Голосів: {playersVoted} / {votesRequired}
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