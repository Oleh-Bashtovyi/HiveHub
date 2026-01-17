import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './AccusationVotingModal.scss';
import {TargetVoteType} from "../../../../models/shared.ts";

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

    // Хелпер для відображення тексту голосу
    const getVoteLabel = (vote: TargetVoteType) => {
        switch (vote) {
            case TargetVoteType.Yes: return { text: 'ЗА (Шпигун)', icon: '✅', color: '#4caf50' };
            case TargetVoteType.No: return { text: 'ПРОТИ (Не шпигун)', icon: '❌', color: '#f44336' };
            case TargetVoteType.Skip: return { text: 'УТРИМАВСЯ', icon: '⏭️', color: '#888' };
            default: return { text: 'Unknown', icon: '?', color: '#fff' };
        }
    };

    const voteInfo = myVote ? getVoteLabel(myVote) : null;

    return (
        <Modal isOpen={isOpen} onClose={() => {}} title={`⚖️ Голосування`}>
            <div className="accusation-voting">
                <div className="accusation-voting__timer">Залишилось часу: {timeLeft} сек</div>

                <div className="accusation-voting__target">
                    <div className="accusation-voting__icon">👤</div>
                    <div className="accusation-voting__name">{targetName}</div>
                    <div className="accusation-voting__label">
                        {isAccused ? 'ви звинувачуєтесь у шпигунстві!' : 'звинувачений у шпигунстві'}
                    </div>
                </div>

                {/* СЦЕНАРІЙ 1: Мене звинувачують */}
                {isAccused && (
                    <div className="accusation-voting__status accusation-voting__status--accused">
                        <div className="accusation-voting__status-icon">⚠️</div>
                        <div className="accusation-voting__status-text">ВАС ЗВИНУВАЧУЮТЬ!</div>
                        <p className="accusation-voting__status-info">
                            Ви не можете голосувати. Виправдовуйтесь у чаті!
                        </p>
                    </div>
                )}

                {/* СЦЕНАРІЙ 2: Я вже проголосував */}
                {!isAccused && voteInfo && (
                    <div className="accusation-voting__status accusation-voting__status--voted">
                        <div className="accusation-voting__status-icon">{voteInfo.icon}</div>
                        <div className="accusation-voting__status-text" style={{ color: voteInfo.color }}>
                            Ви проголосували: {voteInfo.text}
                        </div>
                        <p className="accusation-voting__status-info">Очікуємо інших гравців...</p>
                    </div>
                )}

                {/* СЦЕНАРІЙ 3: Я маю голосувати */}
                {!isAccused && !voteInfo && (
                    <div className="accusation-voting__buttons">
                        <Button fullWidth variant="primary" onClick={() => onVote(TargetVoteType.Yes)}>
                            ✅ ТАК (Шпигун)
                        </Button>
                        <Button fullWidth variant="danger" onClick={() => onVote(TargetVoteType.No)}>
                            ❌ НІ (Не шпигун)
                        </Button>
                        <Button fullWidth variant="secondary" onClick={() => onVote(TargetVoteType.Skip)}>
                            ⏭️ ПРОПУСТИТИ
                        </Button>
                    </div>
                )}
            </div>
        </Modal>
    );
};