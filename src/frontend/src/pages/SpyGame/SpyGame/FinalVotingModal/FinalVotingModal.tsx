import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { AVATAR_MAP } from '../../../../const/avatars';
import type { SpyPlayerDto } from '../../../../models/spy-game';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './FinalVotingModal.scss';
import { en } from '../../../../const/localization/en';

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

    const t = en.spyGame.finalVoting;

    const votedPlayer = myVote ? players.find(p => p.id === myVote) : null;

    return (
        <Modal isOpen={isOpen} onClose={() => {}} title={t.title.replace('{time}', String(timeLeft))}>
            <div className="final-voting">
                <div className="final-voting__header">
                    <div className="final-voting__icon">⏱️</div>
                    <h3 className="final-voting__title">{t.timeUp}</h3>
                    <p className="final-voting__desc">
                        {t.description}
                    </p>
                </div>
                {hasVoted ? (
                    <div className="final-voting__voted">
                        <div className="final-voting__voted-icon">✅</div>
                        <div className="final-voting__voted-text">
                            {votedPlayer ? `${t.youVotedFor}${votedPlayer.name}` : t.youSkipped}
                        </div>
                        <p className="final-voting__voted-info">{t.waitingForOthers}</p>
                    </div>
                ) : (
                    <>
                        <div className="final-voting__skip-section">
                            <Button
                                variant="secondary"
                                fullWidth
                                onClick={() => onVote(null)}
                            >
                                {t.skip}
                            </Button>
                        </div>
                        <div className="final-voting__divider">{t.orChoosePlayer}</div>
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