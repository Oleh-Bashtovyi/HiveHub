import { Link } from 'react-router-dom';
import './Home.scss';
import { en } from '../../const/localization/en';

export const Home = () => {
    const t = en.home;

    return (
        <div className="hub-container">
            <header className="hub-header">
                <div className="hub-logo">
                    <span style={{ fontSize: '2rem' }}>🐝</span> {t.logo}
                </div>
                <div style={{ display: 'flex', gap: '15px', alignItems: 'center' }}>
                    <span style={{ color: '#B0B0B0' }}>{t.greeting}</span>
                    <div style={{ width: 40, height: 40, background: '#333', borderRadius: '50%' }}></div>
                </div>
            </header>

            <h1 style={{ marginBottom: '20px' }}>{t.gamesLibrary}</h1>

            <div className="games-grid">
                <Link to="/spy" className="game-card">
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #2c3e50, #000)' }}>
                        🕵️‍♂️
                    </div>
                    <div className="game-info">
                        <div className="game-title">{t.spyGame.title}</div>
                        <div className="game-desc">{t.spyGame.description}</div>
                        <div style={{ marginTop: '15px', display: 'flex', gap: '10px' }}>
                            <span style={{ fontSize: '0.8rem', background: '#333', padding: '4px 8px', borderRadius: '4px' }}>{t.spyGame.players}</span>
                            <span style={{ fontSize: '0.8rem', background: '#333', padding: '4px 8px', borderRadius: '4px' }}>{t.spyGame.duration}</span>
                        </div>
                    </div>
                </Link>

                <div className="game-card" style={{ opacity: 0.7, cursor: 'default' }}>
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #d35400, #e67e22)' }}>
                        🎨
                    </div>
                    <div className="game-info">
                        <div className="game-title">{t.crocodile.title}</div>
                        <div className="game-desc">{t.crocodile.description}</div>
                    </div>
                </div>

                <div className="game-card" style={{ opacity: 0.7, cursor: 'default' }}>
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #16a085, #2ecc71)' }}>
                        🃏
                    </div>
                    <div className="game-info">
                        <div className="game-title">{t.uno.title}</div>
                        <div className="game-desc">{t.uno.description}</div>
                    </div>
                </div>
            </div>
        </div>
    );
};