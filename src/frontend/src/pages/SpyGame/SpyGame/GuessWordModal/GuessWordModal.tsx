import { useState } from 'react';
import { Modal } from '../../../../components/ui/Modal/Modal';
import { Button } from '../../../../components/ui/Button/Button';
import './GuessWordModal.scss';

interface GuessWordModalProps {
    isOpen: boolean;
    category: string | null;
    onClose: () => void;
    onGuess: (word: string) => void;
}

export const GuessWordModal = ({
                                   isOpen,
                                   category,
                                   onClose,
                                   onGuess
                               }: GuessWordModalProps) => {
    const [word, setWord] = useState('');

    const handleSubmit = () => {
        if (!word.trim()) {
            alert('Введіть слово!');
            return;
        }
        if (!confirm(`Ви впевнені, що секретне слово: "${word.trim()}"? Неправильна відповідь означає програш!`)) {
            return;
        }
        onGuess(word.trim());
    };

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="💡 Вгадати секретне слово">
            <div className="guess-word">
                <div className="guess-word__warning">
                    <div className="guess-word__warning-icon">⚠️</div>
                    <p className="guess-word__warning-text">
                        <strong>УВАГА!</strong> У вас є лише одна спроба. Правильна відповідь принесе перемогу,
                        неправильна — поразку всім шпигунам!
                    </p>
                </div>

                {category && (
                    <div className="guess-word__category">
                        <span className="guess-word__category-label">Категорія:</span>
                        <span className="guess-word__category-value">{category}</span>
                    </div>
                )}

                <div className="guess-word__input-group">
                    <label className="guess-word__label">Ваше слово:</label>
                    <input
                        className="guess-word__input"
                        type="text"
                        value={word}
                        onChange={(e) => setWord(e.target.value)}
                        placeholder="Введіть слово..."
                        maxLength={50}
                        autoFocus
                        onKeyDown={(e) => e.key === 'Enter' && handleSubmit()}
                    />
                </div>

                <div className="guess-word__buttons">
                    <Button variant="secondary" onClick={onClose}>
                        Скасувати
                    </Button>
                    <Button variant="primary" onClick={handleSubmit}>
                        Вгадати
                    </Button>
                </div>
            </div>
        </Modal>
    );
};