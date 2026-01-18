import { useState } from 'react';
import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './GuessWordModal.scss';
import { en } from '../../../../const/localization/en';

interface GuessWordModalProps {
    isOpen: boolean;
    category: string | null;
    isLastChance: boolean;
    endsAt: string | null;
    onClose: () => void;
    onGuess: (word: string) => void;
}

export const GuessWordModal = ({ isOpen, category, isLastChance, endsAt, onClose, onGuess }: GuessWordModalProps) => {
    const [word, setWord] = useState('');
    const timeLeft = useGameTimer(endsAt);

    const t = en.spyGame.guessWord;

    const handleSubmit = () => {
        if (!word.trim()) return alert(t.enterWord);
        if (!confirm(t.confirmGuess.replace('{word}', word.trim()))) return;
        onGuess(word.trim());
    };

    return (
        <Modal
            isOpen={isOpen}
            onClose={isLastChance ? () => {} : onClose}
            title={isLastChance ? t.lastChanceTitle : t.title}
        >
            <div className="guess-word">
                {endsAt && <div className="guess-word__timer">{t.timer.replace('{time}', String(timeLeft))}</div>}

                <div className={`guess-word__warning ${isLastChance ? 'danger' : ''}`}>
                    <div className="guess-word__warning-icon">{isLastChance ? '🔥' : '⚠️'}</div>
                    <p className="guess-word__warning-text">
                        {isLastChance
                            ? t.warningLastChance
                            : <span dangerouslySetInnerHTML={{ __html: t.warningNormal }} />
                        }
                    </p>
                </div>

                {category && (
                    <div className="guess-word__category">
                        <span className="guess-word__category-label">{t.category}</span>
                        <span className="guess-word__category-value">{category}</span>
                    </div>
                )}

                <div className="guess-word__input-group">
                    <input
                        className="guess-word__input"
                        value={word}
                        onChange={(e) => setWord(e.target.value)}
                        placeholder={t.placeholder}
                        maxLength={50}
                        autoFocus
                        onKeyDown={(e) => e.key === 'Enter' && handleSubmit()}
                    />
                </div>

                <div className="guess-word__buttons">
                    {!isLastChance && (
                        <Button variant="secondary" onClick={onClose}>{t.cancel}</Button>
                    )}
                    <Button variant="primary" onClick={handleSubmit}>
                        {isLastChance ? t.tryLuck : t.guess}
                    </Button>
                </div>
            </div>
        </Modal>
    );
};