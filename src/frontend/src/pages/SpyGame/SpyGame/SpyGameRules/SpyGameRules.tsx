import './SpyGameRules.scss';
import { en } from '../../../../const/localization/en';

export const SpyGameRules = () => {
    const t = en.spyGame.rules;

    return (
        <div className="spy-game-rules">
            <h3 className="spy-game-rules__title">{t.title}</h3>
            <ul className="spy-game-rules__list">
                {t.rules.map((rule, index) => (
                    <li key={index} dangerouslySetInnerHTML={{ __html: rule }} />
                ))}
            </ul>
        </div>
    );
};