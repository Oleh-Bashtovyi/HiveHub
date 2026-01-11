import { useMemo } from 'react';
import { Outlet } from 'react-router-dom';
import { useSpyGame } from '../../context/SpyGameContext';
import './SpyLoader.scss';

const GlobalLoader = () => {
    const { isInitializing, isConnecting, isReconnecting } = useSpyGame();

    const loadingMessage = useMemo(() => {
        if (isConnecting) {
            return (
                <>
                    Встановлюємо <span className="spy-loader-highlight">захищене з'єднання</span>...
                </>
            );
        }
        if (isReconnecting) {
            return (
                <>
                    Відновлюємо <span className="spy-loader-highlight">сесію гри</span>...
                </>
            );
        }
        return "Завантаження модулів...";
    }, [isConnecting, isReconnecting]);

    if (isInitializing) {
        return (
            <div className="spy-loader-container">
                <div className="spy-radar-spinner" />

                <div className="spy-status-text">
                    {loadingMessage}
                </div>
            </div>
        );
    }

    return <Outlet />;
};

export default GlobalLoader;