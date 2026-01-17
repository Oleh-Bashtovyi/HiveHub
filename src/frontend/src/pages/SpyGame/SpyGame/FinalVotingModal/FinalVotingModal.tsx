import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { AVATAR_MAP } from '../../../../const/avatars';
import type { SpyPlayerDto } from '../../../../models/spy-game';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './FinalVotingModal.scss';

interface FinalVotingModalProps {
    isOpen: boolean;
    players: SpyPlayerDto[];
    hasVoted: boolean;
    myVote: string | null;
    endsAt: string;
    onVote: (playerId: string | null) => void;
}

export const FinalVotingModal = ({ isOpen, players, hasVoted, myVote, endsAt, onVote }: FinalVotingModalProps) => {
    const timeLeft = useGameTimer(endsAt);

    const votedPlayer = myVote ? players.find(p => p.id === myVote) : null;

    return (
        <Modal isOpen={isOpen} onClose={() => {}} title={`🗳️ Фінальне голосування (${timeLeft}с)`}>
            <div className="final-voting">
                <div className="final-voting__header">
                    <div className="final-voting__icon">⏱️</div>
                    <h3 className="final-voting__title">Час вийшов!</h3>
                    <p className="final-voting__desc">
                        Оберіть гравця, якого ви підозрюєте у шпигунстві, або пропустіть
                    </p>
                </div>
                {hasVoted ? (
                    <div className="final-voting__voted">
                        <div className="final-voting__voted-icon">✅</div>
                        <div className="final-voting__voted-text">
                            {votedPlayer ? `Ви проголосували за: ${votedPlayer.name}` : 'Ви пропустили голосування'}
                        </div>
                        <p className="final-voting__voted-info">Очікуємо інших гравців...</p>
                    </div>
                ) : (
                    <>
                        <div className="final-voting__skip-section">
                            <Button
                                variant="secondary"
                                fullWidth
                                onClick={() => onVote(null)}
                            >
                                ⏭️ ПРОПУСТИТИ (Немає підозр)
                            </Button>
                        </div>
                        <div className="final-voting__divider">або оберіть гравця</div>
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
                    </>
                )}
            </div>
        </Modal>
    );
};