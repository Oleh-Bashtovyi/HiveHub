import { useState, useRef, useEffect } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import './SpyGameChat.scss';
import type {ChatMessageDto} from "../../../../models/shared.ts";

interface SpyGameChatProps {
    messages: ChatMessageDto[];
    currentPlayerId: string;
    onSendMessage: (message: string) => Promise<void>;
}

export const SpyGameChat = ({ messages, currentPlayerId, onSendMessage }: SpyGameChatProps) => {
    const [msgText, setMsgText] = useState('');
    const messagesContainerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (messagesContainerRef.current) {
            messagesContainerRef.current.scrollTop = messagesContainerRef.current.scrollHeight;
        }
    }, [messages]);

    const handleSend = async () => {
        if (!msgText.trim()) return;
        try {
            await onSendMessage(msgText);
            setMsgText('');
        } catch (error) {
            console.error('Failed to send message:', error);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            void handleSend();
        }
    };

    return (
        <div className="spy-game-chat">
            <h3 className="spy-game-chat__title">💬 Чат</h3>

            <div className="spy-game-chat__messages" ref={messagesContainerRef}>
                {messages.length === 0 && (
                    <div className="spy-game-chat__empty">Повідомлень ще немає...</div>
                )}
                {messages.map((msg, idx) => (
                    <div
                        key={idx}
                        className={`spy-game-chat__message ${msg.playerId === currentPlayerId ? 'spy-game-chat__message--mine' : ''}`}
                    >
                        <div className="spy-game-chat__message-header">
                            <span className="spy-game-chat__message-author">{msg.playerName}</span>
                            <span className="spy-game-chat__message-time">
                                {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </span>
                        </div>
                        <div className="spy-game-chat__message-text">{msg.message}</div>
                    </div>
                ))}
            </div>

            <div className="spy-game-chat__input">
                <input
                    className="spy-game-chat__input-field"
                    value={msgText}
                    onChange={e => setMsgText(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Повідомлення..."
                    maxLength={200}
                />
                <Button size="small" onClick={handleSend}>📤</Button>
            </div>
        </div>
    );
};
