import './SpyGameRoleCard.scss';

interface SpyGameRoleCardProps {
    isSpy: boolean;
    secretWord: string | null;
    category: string | null;
}

export const SpyGameRoleCard = ({ isSpy, secretWord, category }: SpyGameRoleCardProps) => {
    return (
        <div className={`spy-role-card ${isSpy ? 'spy-role-card--spy' : 'spy-role-card--civilian'}`}>
            <div className="spy-role-card__icon">{isSpy ? '🥷' : '🕵️'}</div>
            <div className="spy-role-card__title">
                {isSpy ? "ВИ ШПИГУН" : "Мирний Житель"}
            </div>

            <div className="spy-role-card__desc">
                {isSpy ? (
                    <>Ваша ціль: дізнатися слово з розмов інших або протриматися до кінця і не видати себе.</>
                ) : (
                    <>Ваша ціль: знайти шпигуна серед гравців, задаючи навідні питання.</>
                )}
            </div>

            {isSpy ? (
                category && <div className="spy-role-card__category-badge">Категорія: {category}</div>
            ) : (
                <>
                    <div className="spy-role-card__secret-word">{secretWord}</div>
                    <div className="spy-role-card__category-text">Категорія: <strong>{category}</strong></div>
                </>
            )}
        </div>
    );
};