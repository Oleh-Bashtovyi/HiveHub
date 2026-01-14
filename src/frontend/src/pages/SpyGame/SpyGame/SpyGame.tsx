import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext';
import { RoomState } from '../../../models/spy-game';
import {SpyGameHeader} from "./SpyGameHeader/SpyGameHeader.tsx";
import {SpyGameRoleCard} from "./SpyGameRoleCard/SpyGameRoleCard.tsx";
import {SpyGamePlayers} from "./SpyGamePlayers/SpyGamePlayers.tsx";
import {SpyGameCenter} from "./SpyGameCenter/SpyGameCenter.tsx";
import {SpyGameChat} from "./SpyGameChat/SpyGameChat.tsx";
import './SpyGame.scss';

export const SpyGame = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        players,
        me,
        gameState,
        roomState,
        isInitializing,
        sendMessage,
        voteStopTimer,
        revealSpies,
        leaveRoom,
        returnToLobby
    } = useSpyGame();

    const [timeLeft, setTimeLeft] = useState(0);

    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: unknown) {
            console.error(error);
            const msg = error instanceof Error ? error.message : 'Unknown error';
            alert(`Помилка: ${msg}`);
        }
    };

    useEffect(() => {
        if (isInitializing) return;

        if (!roomCode) {
            navigate('/spy');
            return;
        }

        if (roomState === RoomState.Lobby) {
            navigate('/spy/lobby');
            return;
        }

        if (roomState === RoomState.Ended) {
            navigate('/spy/results');
            return;
        }
    }, [roomCode, roomState, navigate, isInitializing]);

    useEffect(() => {
        if (!gameState?.gameEndTime || gameState.isTimerStopped) return;

        const updateTimer = () => {
            const end = new Date(gameState.gameEndTime!).getTime();
            const now = new Date().getTime();
            const diff = Math.floor((end - now) / 1000);
            setTimeLeft(Math.max(0, diff));
        };

        updateTimer();
        const interval = setInterval(updateTimer, 1000);
        return () => clearInterval(interval);
    }, [gameState?.gameEndTime, gameState?.isTimerStopped]);

    if (isInitializing || !gameState || !me || !roomCode) {
        return null;
    }

    const handleVoteStopTimer = () => {
        if (confirm("Зупинити таймер для обговорення?")) {
            void safeExecute(async () => await voteStopTimer());
        }
    };

    const handleLeave = () => {
        if (confirm('Ви впевнені? Це завершить гру для вас.')) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy');
            });
        }
    };

    const handleAbortGame = () => {
        if (confirm('УВАГА: Це примусово завершить гру для всіх і поверне всіх в лобі. Продовжити?')) {
            void safeExecute(async () => await returnToLobby());
        }
    };

    const isSpyRole = !gameState.currentSecretWord;
    const activePlayers = players.filter(p => p.isConnected).length;

    return (
        <div className="spy-game-page">
            <div className="spy-game-container">
                <SpyGameHeader
                    roomCode={roomCode}
                    timeLeft={timeLeft}
                    isTimerStopped={gameState.isTimerStopped}
                    timerVotesCount={gameState.timerVotesCount}
                    activePlayers={activePlayers}
                    hasVoted={me.isVotedToStopTimer}
                    onVoteStopTimer={handleVoteStopTimer}
                />

                <div className="spy-game-layout">
                    <div className="spy-game-layout__column spy-game-layout__column--left">
                        <SpyGameRoleCard
                            isSpy={isSpyRole}
                            secretWord={gameState.currentSecretWord}
                            category={gameState.category}
                        />

                        <SpyGamePlayers
                            players={players}
                            currentPlayerId={me.id}
                            isTimerStopped={gameState.isTimerStopped}
                        />
                    </div>

                    <div className="spy-game-layout__column spy-game-layout__column--center">
                        <SpyGameCenter
                            isTimerStopped={gameState.isTimerStopped}
                            isHost={me.isHost}
                            onRevealSpies={revealSpies}
                            onAbortGame={handleAbortGame}
                            onLeaveGame={handleLeave}
                        />
                    </div>

                    <div className="spy-game-layout__column spy-game-layout__column--right">
                        <SpyGameChat
                            messages={gameState.recentMessages}
                            currentPlayerId={me.id}
                            onSendMessage={sendMessage}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
};
