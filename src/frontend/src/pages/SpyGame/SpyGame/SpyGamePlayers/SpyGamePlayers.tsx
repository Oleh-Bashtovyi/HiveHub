import { AVATAR_MAP } from '../../../../const/avatars';
import { Button } from '../../../../components/ui/Button/Button';
import type { SpyPlayerDto } from '../../../../models/spy-game';
import './SpyGamePlayers.scss';

interface SpyGamePlayersProps {
    players: SpyPlayerDto[];
    currentPlayerId: string;
    shouldShowSpies: boolean;
    votesForTimer: number;
    votesRequired: number;
    caughtSpyId: string | null;
    canAccuse: boolean;
    onAccuse: (playerId: string) => void;
}

export const SpyGamePlayers = ({
                                   players,
                                   currentPlayerId,
                                   shouldShowSpies,
                                   votesForTimer,
                                   votesRequired,
                                   caughtSpyId,
                                   canAccuse,
                                   onAccuse
                               }: SpyGamePlayersProps) => {
    return (
        <div className="spy-game-players">
            <h3 className="spy-game-players__title">
                👥 Гравці
                {votesForTimer > 0 && (
                    <span className="spy-game-players__timer-votes">
                        ⏸️ {votesForTimer}/{votesRequired}
                    </span>
                )}
            </h3>
            <div className="spy-game-players__list">
                {players.map(p => {
                    const isMe = p.id === currentPlayerId;
                    const isCaught = p.id === caughtSpyId;
                    const isDead = p.isDead ?? false;
                    const isSpy = p.isSpy ?? null;
                    const showSpyBadge = shouldShowSpies && isSpy && !isMe;
                    const canAccuseThis = canAccuse && !isMe && p.isConnected && !isCaught && !isDead;

                    return (
                        <div
                            key={p.id}
                            className={`spy-game-players__item ${isCaught ? 'spy-game-players__item--caught' : ''} ${isDead ? 'spy-game-players__item--dead' : ''}`}
                            style={{ opacity: p.isConnected ? 1 : 0.5 }}
                        >
                            <div className="spy-game-players__avatar">
                                {isDead && <div className="spy-game-players__skull">💀</div>}
                                {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                            </div>
                            <div className="spy-game-players__info">
                                <div className="spy-game-players__name-row">
                                    <span className="spy-game-players__name">
                                        {p.name} {isMe && '(Ви)'}
                                    </span>
                                    {p.isHost && <span title="Хост">👑</span>}
                                    {showSpyBadge && (
                                        <span title="Союзник-шпигун" className="spy-game-players__spy-badge">🥷</span>
                                    )}
                                    {p.isVotedToStopTimer && votesForTimer > 0 && (
                                        <span title="Голосував за стоп" className="spy-game-players__vote-hand">✋</span>
                                    )}
                                    {isCaught && <span title="Спійманий шпигун" className="spy-game-players__caught-badge">🔒</span>}
                                    {isDead && !isCaught && <span title="Мертвий" className="spy-game-players__dead-badge">💀</span>}
                                </div>
                                {!p.isConnected && <span className="spy-game-players__offline">🔌 Офлайн</span>}

                                {canAccuseThis && (
                                    <Button
                                        size="small"
                                        variant="danger"
                                        onClick={() => onAccuse(p.id)}
                                        className="spy-game-players__accuse-btn"
                                    >
                                        ⚠️ Звинуватити
                                    </Button>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};