import { Modal } from '../../../../components/ui/Modal/Modal';
import { AVATAR_MAP } from '../../../../const/avatars';
import type { SpyPlayerDto } from '../../../../models/spy-game';
import './FinalVotingModal.scss';

interface FinalVotingModalProps {
    isOpen: boolean;
    players: SpyPlayerDto[];
    hasVoted: boolean;
    onVote: (playerId: string) => void;
}

export const FinalVotingModal = ({
                                     isOpen,
                                     players,
                                     hasVoted,
                                     onVote
                                 }: FinalVotingModalProps) => {
    return (
        <Modal isOpen={isOpen} onClose={() => {}} title="🗳️ Фінальне голосування">
            <div className="final-voting">
                <div className="final-voting__header">
                    <div className="final-voting__icon">⏱️</div>
                    <h3 className="final-voting__title">Час вийшов!</h3>
                    <p className="final-voting__desc">
                        Оберіть гравця, якого ви підозрюєте у шпигунстві
                    </p>
                </div>

                {hasVoted ? (
                    <div className="final-voting__voted">
                        <div className="final-voting__voted-icon">✅</div>
                        <div className="final-voting__voted-text">Ви проголосували</div>
                        <p className="final-voting__voted-info">Очікуємо інших гравців...</p>
                    </div>
                ) : (
                    <div className="final-voting__players">
                        {players.map(p => (
                            <button
                                key={p.id}
                                className="final-voting__player"
                                onClick={() => onVote(p.id)}
                                disabled={!p.isConnected}
                            >
                                <div className="final-voting__player-avatar">
                                    {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                                </div>
                                <div className="final-voting__player-name">
                                    {p.name}
                                    {p.isHost && ' 👑'}
                                </div>
                                <div className="final-voting__player-vote">👉</div>
                            </button>
                        ))}
                    </div>
                )}
            </div>
        </Modal>
    );
};