import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './AccusationVotingModal.scss';
import {TargetVoteType} from "../../../../models/shared.ts";
import { en } from '../../../../const/localization/en';

interface AccusationVotingModalProps {
    isOpen: boolean;
    targetName: string;
    isAccused: boolean;
    myVote?: TargetVoteType;
    endsAt: string;
    onVote: (voteType: TargetVoteType) => void;
}

export const AccusationVotingModal = ({
                                          isOpen,
                                          targetName,
                                          isAccused,
                                          myVote,
                                          endsAt,
                                          onVote
                                      }: AccusationVotingModalProps) => {
    const timeLeft = useGameTimer(endsAt);

    const t = en.spyGame.accusationVoting;

    const getVoteLabel = (vote: TargetVoteType) => {
        switch (vote) {
            case TargetVoteType.Yes: return { text: t.votes.yes, icon: '✅', color: '#4caf50' };
            case TargetVoteType.No: return { text: t.votes.no, icon: '❌', color: '#f44336' };
            case TargetVoteType.Skip: return { text: t.votes.skip, icon: '⏭️', color: '#888' };
            default: return { text: 'Unknown', icon: '?', color: '#fff' };
        }
    };

    const voteInfo = myVote ? getVoteLabel(myVote) : null;

    return (
        <Modal isOpen={isOpen} onClose={() => {}} title={t.title}>
            <div className="accusation-voting">
                <div className="accusation-voting__timer">{t.timeLeft.replace('{time}', String(timeLeft))}</div>

                <div className="accusation-voting__target">
                    <div className="accusation-voting__icon">👤</div>
                    <div className="accusation-voting__name">{targetName}</div>
                    <div className="accusation-voting__label">
                        {isAccused ? t.youAccused : t.accusedOfSpying}
                    </div>
                </div>

                {isAccused && (
                    <div className="accusation-voting__status accusation-voting__status--accused">
                        <div className="accusation-voting__status-icon">⚠️</div>
                        <div className="accusation-voting__status-text">{t.youAreAccused}</div>
                        <p className="accusation-voting__status-info">
                            {t.youAreAccusedDesc}
                        </p>
                    </div>
                )}

                {!isAccused && voteInfo && (
                    <div className="accusation-voting__status accusation-voting__status--voted">
                        <div className="accusation-voting__status-icon">{voteInfo.icon}</div>
                        <div className="accusation-voting__status-text" style={{ color: voteInfo.color }}>
                            {t.youVoted}{voteInfo.text}
                        </div>
                        <p className="accusation-voting__status-info">{t.waitingForOthers}</p>
                    </div>
                )}

                {!isAccused && !voteInfo && (
                    <div className="accusation-voting__buttons">
                        <Button fullWidth variant="primary" onClick={() => onVote(TargetVoteType.Yes)}>
                            {t.voteYes}
                        </Button>
                        <Button fullWidth variant="danger" onClick={() => onVote(TargetVoteType.No)}>
                            {t.voteNo}
                        </Button>
                        <Button fullWidth variant="secondary" onClick={() => onVote(TargetVoteType.Skip)}>
                            {t.voteSkip}
                        </Button>
                    </div>
                )}
            </div>
        </Modal>
    );
};