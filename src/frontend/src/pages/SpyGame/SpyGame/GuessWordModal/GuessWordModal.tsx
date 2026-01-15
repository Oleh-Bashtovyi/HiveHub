import { useState } from 'react';
import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import { useGameTimer } from '../../../../hooks/useGameTimer';
import './GuessWordModal.scss';

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

    const handleSubmit = () => {
        if (!word.trim()) return alert('Введіть слово!');
        if (!confirm(`Секретне слово: "${word.trim()}"?`)) return;
        onGuess(word.trim());
    };

    return (
        <Modal
            isOpen={isOpen}
            onClose={isLastChance ? () => {} : onClose}
            title={isLastChance ? "🔥 ОСТАННІЙ ШАНС" : "💡 Вгадати слово"}
        >
            <div className="guess-word">
                {endsAt && <div className="guess-word__timer">⏱️ {timeLeft} сек</div>}

                <div className={`guess-word__warning ${isLastChance ? 'danger' : ''}`}>
                    <div className="guess-word__warning-icon">{isLastChance ? '🔥' : '⚠️'}</div>
                    <p className="guess-word__warning-text">
                        {isLastChance
                            ? "Вас спіймали! Це ваш єдиний шанс виграти."
                            :  <span><strong>УВАГА!</strong> У вас є лише одна спроба. Правильна відповідь принесе перемогу,
                            неправильна — поразку всім шпигунам!</span>
                        }
                    </p>
                </div>

                {category && (
                    <div className="guess-word__category">
                        <span className="guess-word__category-label">Категорія:</span>
                        <span className="guess-word__category-value">{category}</span>
                    </div>
                )}

                <div className="guess-word__input-group">
                    <input
                        className="guess-word__input"
                        value={word}
                        onChange={(e) => setWord(e.target.value)}
                        placeholder="Введіть слово..."
                        maxLength={50}
                        autoFocus
                        onKeyDown={(e) => e.key === 'Enter' && handleSubmit()}
                    />
                </div>

                <div className="guess-word__buttons">
                    {!isLastChance && (
                        <Button variant="secondary" onClick={onClose}>Скасувати</Button>
                    )}
                    <Button variant="primary" onClick={handleSubmit}>
                        {isLastChance ? "Спробувати долю" : "Вгадати"}
                    </Button>
                </div>
            </div>
        </Modal>
    );
};