import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { TargetVoteType } from '../../../../models/spy-game';
import './AccusationVotingModal.scss';

interface AccusationVotingModalProps {
    isOpen: boolean;
    targetName: string;
    hasVoted: boolean;
    onVote: (voteType: TargetVoteType) => void;
}

export const AccusationVotingModal = ({
                                          isOpen,
                                          targetName,
                                          hasVoted,
                                          onVote
                                      }: AccusationVotingModalProps) => {
    return (
        <Modal isOpen={isOpen} onClose={() => {}} title="⚖️ Голосування">
            <div className="accusation-voting">
                <div className="accusation-voting__target">
                    <div className="accusation-voting__icon">👤</div>
                    <div className="accusation-voting__name">{targetName}</div>
                    <div className="accusation-voting__label">звинувачений у шпигунстві</div>
                </div>

                {hasVoted ? (
                    <div className="accusation-voting__voted">
                        <div className="accusation-voting__voted-icon">✅</div>
                        <div className="accusation-voting__voted-text">Ви проголосували</div>
                        <p className="accusation-voting__voted-info">Очікуємо інших гравців...</p>
                    </div>
                ) : (
                    <div className="accusation-voting__buttons">
                        <Button
                            fullWidth
                            variant="primary"
                            onClick={() => onVote(TargetVoteType.Yes)}
                        >
                            ✅ ТАК (Шпигун)
                        </Button>
                        <Button
                            fullWidth
                            variant="danger"
                            onClick={() => onVote(TargetVoteType.No)}
                        >
                            ❌ НІ (Не шпигун)
                        </Button>
                        <Button
                            fullWidth
                            variant="secondary"
                            onClick={() => onVote(TargetVoteType.Skip)}
                        >
                            ⏭️ ПРОПУСТИТИ
                        </Button>
                    </div>
                )}
            </div>
        </Modal>
    );
};