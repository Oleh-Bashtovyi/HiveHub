import { Button } from '../../../../components/ui/Button/Button';
import './SpyGameRoleCard.scss';

interface SpyGameRoleCardProps {
    isSpy: boolean;
    isDead: boolean;
    secretWord: string | null;
    category: string | null;
    onGuessWord?: () => void;
}

export const SpyGameRoleCard = ({ isSpy, isDead, secretWord, category, onGuessWord }: SpyGameRoleCardProps) => {
    return (
        <div className={`spy-role-card ${isSpy ? 'spy-role-card--spy' : 'spy-role-card--civilian'} ${isDead ? 'spy-role-card--dead' : ''}`}>
            {isDead && (
                <div className="spy-role-card__skull-overlay">
                    💀
                </div>
            )}

            <div className="spy-role-card__icon">{isSpy ? '🥷' : '🕵️'}</div>
            <div className="spy-role-card__title">
                {isDead ? "ВИ МЕРТВІ" : (isSpy ? "ВИ ШПИГУН" : "Мирний Житель")}
            </div>

            <div className="spy-role-card__desc">
                {isDead ? (
                    <>Ви програли і більше не можете впливати на гру. Але можете спостерігати за грою в чаті!</>
                ) : isSpy ? (
                    <>Ваша ціль: дізнатися слово з розмов інших або протриматися до кінця і не видати себе.</>
                ) : (
                    <>Ваша ціль: знайти шпигуна серед гравців, задаючи навідні питання.</>
                )}
            </div>

            {!isDead && (
                <>
                    {isSpy ? (
                        <>
                            {category && <div className="spy-role-card__category-badge">Категорія: {category}</div>}
                            {onGuessWord && (
                                <Button
                                    size="small"
                                    variant="secondary"
                                    onClick={onGuessWord}
                                    className="spy-role-card__guess-btn"
                                >
                                    💡 Вгадати слово
                                </Button>
                            )}
                        </>
                    ) : (
                        <>
                            <div className="spy-role-card__secret-word">{secretWord}</div>
                            <div className="spy-role-card__category-text">Категорія: <strong>{category}</strong></div>
                        </>
                    )}
                </>
            )}
        </div>
    );
};