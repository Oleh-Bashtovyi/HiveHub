import { AVATAR_MAP } from '../../../../const/avatars';
import './SpyGamePlayers.scss';
import type { PlayerDto } from "../../../../models/spy-game.ts";

interface SpyGamePlayersProps {
    players: PlayerDto[];
    currentPlayerId: string;
    isTimerStopped: boolean;
}

export const SpyGamePlayers = ({ players, currentPlayerId, isTimerStopped }: SpyGamePlayersProps) => {
    return (
        <div className="spy-game-players">
            <h3 className="spy-game-players__title">Гравці</h3>
            <div className="spy-game-players__list">
                {players.map(p => (
                    <div
                        key={p.id}
                        className="spy-game-players__item"
                        style={{ opacity: p.isConnected ? 1 : 0.5 }}
                    >
                        <div className="spy-game-players__avatar">
                            {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                        </div>
                        <div className="spy-game-players__info">
                            <div className="spy-game-players__name-row">
                                <span className="spy-game-players__name">
                                    {p.name} {p.id === currentPlayerId && '(Ви)'}
                                </span>
                                {p.isHost && <span title="Хост">👑</span>}
                                {!isTimerStopped && p.isVotedToStopTimer && (
                                    <span title="Голосував за стоп" className="spy-game-players__vote-hand">✋</span>
                                )}
                                {p.isSpy && <span title="Шпигун">🥷</span>}
                            </div>
                            {!p.isConnected && <span className="spy-game-players__offline">🔌 Офлайн</span>}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};