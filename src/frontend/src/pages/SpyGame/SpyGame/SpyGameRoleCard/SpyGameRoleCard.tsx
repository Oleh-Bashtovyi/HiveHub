import { Button } from '../../../../components/ui/Button/Button';
import './SpyGameRoleCard.scss';
import { en } from '../../../../const/localization/en';

interface SpyGameRoleCardProps {
    isSpy: boolean;
    isDead: boolean;
    secretWord: string | null;
    category: string | null;
    onGuessWord?: () => void;
}

export const SpyGameRoleCard = ({ isSpy, isDead, secretWord, category, onGuessWord }: SpyGameRoleCardProps) => {
    const t = en.spyGame.roleCard;

    return (
        <div className={`spy-role-card ${isSpy ? 'spy-role-card--spy' : 'spy-role-card--civilian'} ${isDead ? 'spy-role-card--dead' : ''}`}>
            {isDead && (
                <div className="spy-role-card__skull-overlay">
                    💀
                </div>
            )}

            <div className="spy-role-card__icon">{isSpy ? '🥷' : '🕵️'}</div>
            <div className="spy-role-card__title">
                {isDead ? t.youAreDead : (isSpy ? t.youAreSpy : t.civilian)}
            </div>

            <div className="spy-role-card__desc">
                {isDead ? (
                    <>{t.deadDescription}</>
                ) : isSpy ? (
                    <>{t.spyDescription}</>
                ) : (
                    <>{t.civilianDescription}</>
                )}
            </div>

            {!isDead && (
                <>
                    {isSpy ? (
                        <>
                            {category && <div className="spy-role-card__category-badge">{t.category}{category}</div>}
                            {onGuessWord && (
                                <Button
                                    size="small"
                                    variant="secondary"
                                    onClick={onGuessWord}
                                    className="spy-role-card__guess-btn"
                                >
                                    {t.guessWord}
                                </Button>
                            )}
                        </>
                    ) : (
                        <>
                            <div className="spy-role-card__secret-word">{secretWord}</div>
                            <div className="spy-role-card__category-text">{t.category}<strong>{category}</strong></div>
                        </>
                    )}
                </>
            )}
        </div>
    );
};