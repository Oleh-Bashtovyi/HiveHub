import { Link } from 'react-router-dom';
import './Home.scss';

export const Home = () => {
    return (
        <div className="hub-container">
            <header className="hub-header">
                <div className="hub-logo">
                    <span style={{ fontSize: '2rem' }}>🐝</span> HiveHub
                </div>
                <div style={{ display: 'flex', gap: '15px', alignItems: 'center' }}>
                    <span style={{ color: '#B0B0B0' }}>Привіт, Геймер</span>
                    <div style={{ width: 40, height: 40, background: '#333', borderRadius: '50%' }}></div>
                </div>
            </header>

            <h1 style={{ marginBottom: '20px' }}>Бібліотека ігор</h1>

            <div className="games-grid">
                {/* Spy Game Card */}
                <Link to="/spy" className="game-card">
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #2c3e50, #000)' }}>
                        🕵️‍♂️
                    </div>
                    <div className="game-info">
                        <div className="game-title">Знайди Шпигуна</div>
                        <div className="game-desc">Психологічна гра. Вичисліть зрадника або обдуріть усіх.</div>
                        <div style={{ marginTop: '15px', display: 'flex', gap: '10px' }}>
                            <span style={{ fontSize: '0.8rem', background: '#333', padding: '4px 8px', borderRadius: '4px' }}>3-8 гравців</span>
                            <span style={{ fontSize: '0.8rem', background: '#333', padding: '4px 8px', borderRadius: '4px' }}>~15 хв</span>
                        </div>
                    </div>
                </Link>

                {/* Coming Soon */}
                <div className="game-card" style={{ opacity: 0.7, cursor: 'default' }}>
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #d35400, #e67e22)' }}>
                        🎨
                    </div>
                    <div className="game-info">
                        <div className="game-title">Crocodile (Незабаром)</div>
                        <div className="game-desc">Малюй та вгадуй слова разом з друзями.</div>
                    </div>
                </div>

                <div className="game-card" style={{ opacity: 0.7, cursor: 'default' }}>
                    <div className="game-cover" style={{ background: 'linear-gradient(135deg, #16a085, #2ecc71)' }}>
                        🃏
                    </div>
                    <div className="game-info">
                        <div className="game-title">Uno Online (Незабаром)</div>
                        <div className="game-desc">Класична карткова гра.</div>
                    </div>
                </div>
            </div>
        </div>
    );
};