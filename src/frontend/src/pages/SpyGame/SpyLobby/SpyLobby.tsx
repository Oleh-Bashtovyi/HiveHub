import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/spy-game/SpyGameContext';
import { Button } from '../../../components/ui/Button/Button';
import { SpyGameChat } from '../SpyGame/SpyGameChat/SpyGameChat';
import './SpyLobby.scss';
import { LobbySettings } from "./LobbySettings/LobbySettings.tsx";
import { PlayersPanel } from "./PlayersPanel/PlayersPanel.tsx";
import {RoomStatus} from "../../../models/shared.ts";

type TabType = 'settings' | 'chat';

export const SpyLobby = () => {
    const navigate = useNavigate();
    const {
        isInitializing,
        roomCode,
        me,
        players,
        rules,
        wordPacks,
        roomState,
        messages,
        leaveRoom,
        toggleReady,
        updateRules,
        updateWordPacks,
        startGame,
        kickPlayer,
        changeHost,
        changeName,
        changeAvatar,
        sendMessage
    } = useSpyGame();

    const [activeTab, setActiveTab] = useState<TabType>('settings');
    const [lastSeenMessageCount, setLastSeenMessageCount] = useState(0);
    const hasUnreadMessages = messages.length > lastSeenMessageCount;

    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: unknown) {
            console.error(error);
            const msg = error instanceof Error ? error.message : 'Невідома помилка';
            alert(`Помилка: ${msg}`);
        }
    };

    // Mark messages as read when chat tab is opened
    useEffect(() => {
        if (activeTab === 'chat') {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setLastSeenMessageCount(messages.length);
        }
    }, [activeTab, messages.length]);

    useEffect(() => {
        if (isInitializing) return;

        if (!roomCode) {
            navigate('/spy');
            return;
        }
        if (roomState === RoomStatus.InGame) {
            navigate('/spy/game');
        } else if (roomState === RoomStatus.Ended) {
            navigate('/spy/results');
        }
    }, [roomCode, roomState, navigate, isInitializing]);

    if (!roomCode || !rules || !wordPacks || !me) return <div>Loading Lobby...</div>;

    const copyCode = () => {
        navigator.clipboard.writeText(roomCode);
    };

    const handleLeave = () => {
        if (confirm('Leave room?')) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy');
            });
        }
    };

    const handleStart = () => {
        void safeExecute(async () => await startGame());
    };

    const handleToggleReady = () => {
        void safeExecute(async () => await toggleReady());
    };

    const allReady = players.length >= 3 && players.every(p => p.isReady);

    return (
        <div className="spy-lobby-page theme-spy">
            <div className="lobby-container">
                {/* Header */}
                <div className="lobby-header">
                    <div className="room-code-group">
                        <h2>Кімната</h2>
                        <div className="code-badge" onClick={copyCode} title="Клікніть, щоб скопіювати">
                            {roomCode}
                        </div>
                    </div>
                    <Button variant="danger" onClick={handleLeave} size="small">
                        Вийти
                    </Button>
                </div>

                <div className="lobby-content">
                    {/* LEFT: Players Grid */}
                    <PlayersPanel
                        players={players}
                        me={me}
                        maxPlayersCount={rules.maxPlayersCount}
                        isHost={me.isHost}
                        isReady={me.isReady}
                        allReady={allReady}
                        onToggleReady={handleToggleReady}
                        onStartGame={handleStart}
                        onKickPlayer={(playerId) => safeExecute(async () => await kickPlayer(playerId))}
                        onChangeHost={(playerId) => safeExecute(async () => await changeHost(playerId))}
                        onChangeName={(name) => safeExecute(async () => await changeName(name))}
                        onChangeAvatar={(avatarId) => safeExecute(async () => await changeAvatar(avatarId))}
                    />

                    {/* RIGHT: Sidebar (Tabs: Settings / Chat) */}
                    <div className="section-panel sidebar-panel">
                        <div className="sidebar-tabs">
                            <button
                                className={`tab-btn ${activeTab === 'settings' ? 'active' : ''}`}
                                onClick={() => setActiveTab('settings')}
                            >
                                ⚙️ Налаштування
                            </button>
                            <button
                                className={`tab-btn ${activeTab === 'chat' ? 'active' : ''}`}
                                onClick={() => setActiveTab('chat')}
                            >
                                💬 Чат
                                {hasUnreadMessages && <span className="tab-badge"></span>}
                            </button>
                        </div>

                        <div className="sidebar-content">
                            {activeTab === 'settings' && (
                                <LobbySettings
                                    rules={rules}
                                    wordPacks={wordPacks}
                                    isHost={me.isHost}
                                    onUpdateRules={(updates) =>
                                        safeExecute(async () => await updateRules({ ...rules, ...updates }))
                                    }
                                    onUpdateWordPacks={(packs) =>
                                        safeExecute(async () => await updateWordPacks(packs))
                                    }
                                />
                            )}

                            {activeTab === 'chat' && (
                                <div className="chat-tab-container">
                                    <SpyGameChat
                                        messages={messages}
                                        currentPlayerId={me.id}
                                        onSendMessage={sendMessage}
                                    />
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};