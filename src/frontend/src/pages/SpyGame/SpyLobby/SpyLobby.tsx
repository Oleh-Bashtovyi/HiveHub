import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSpyGame } from '../../../context/SpyGameContext';
import { Button } from '../../../components/ui/Button/Button';
import { Modal } from '../../../components/ui/Modal/Modal';
import { type RoomGameSettingsDto, RoomState, type WordsCategoryDto } from '../../../models/spy-game';
import './SpyLobby.scss';
import {AVAILABLE_AVATARS, AVATAR_MAP} from "../../../const/avatars.ts";

export const SpyLobby = () => {
    const navigate = useNavigate();
    const {
        roomCode,
        me,
        players,
        settings,
        roomState,
        leaveRoom,
        toggleReady,
        updateSettings,
        startGame,
        kickPlayer,
        changeHost,
        changeName,
        changeAvatar
    } = useSpyGame();

    // --- State ---
    const [isCatModalOpen, setCatModalOpen] = useState(false);
    const [isProfileModalOpen, setProfileModalOpen] = useState(false);

    // Edit Category State
    const [editingCatName, setEditingCatName] = useState('');
    const [editingCatWords, setEditingCatWords] = useState<string[]>([]);
    const [editingOriginalName, setEditingOriginalName] = useState<string | null>(null);
    const [newWordInput, setNewWordInput] = useState('');

    // Edit Profile State
    const [tempName, setTempName] = useState('');

    useEffect(() => {
        if (!roomCode) {
            navigate('/spy');
            return;
        }
        if (roomState === RoomState.InGame) {
            navigate('/spy/game');
        }
    }, [roomCode, roomState, navigate]);

    // --- Error Handling Wrapper ---
    const safeExecute = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (error: any) {
            console.error(error);
            const msg = error?.message || 'Невідома помилка';
            alert(`Помилка: ${msg}`);
        }
    };

    if (!roomCode || !settings || !me) return <div>Loading Lobby...</div>;

    // --- Actions ---
    const copyCode = () => {
        navigator.clipboard.writeText(roomCode);
    };

    const handleLeave = () => {
        if (confirm('Вийти з кімнати?')) {
            void safeExecute(async () => {
                await leaveRoom();
                navigate('/spy');
            });
        }
    };

    const handleStart = () => {
        void safeExecute(async () => await startGame());
    };

    const handleToggleReady = () => {
        void safeExecute(async () => await toggleReady());
    };

    // --- Settings Helpers ---
    const updateSetting = (key: keyof RoomGameSettingsDto, value: unknown) => {
        if (!me.isHost) return;
        const newSettings = { ...settings, [key]: value };
        safeExecute(async () => await updateSettings(newSettings));
    };

    const modifyNumber = (key: 'timerMinutes' | 'spiesCount', delta: number, min: number, max: number) => {
        if (!me.isHost) return;
        const current = settings[key];
        const next = Math.max(min, Math.min(max, current + delta));
        if (next !== current) {
            updateSetting(key, next);
        }
    };

    // --- Profile Management ---
    const openProfileModal = () => {
        setTempName(me.name);
        setProfileModalOpen(true);
    };

    const handleSaveName = () => {
        if (!tempName.trim()) return alert("Ім'я не може бути порожнім");
        if (tempName === me.name) return;

        void safeExecute(async () => {
            await changeName(tempName.trim());
        });
    };

    const handleSelectAvatar = (avatarId: string) => {
        if (avatarId === me.avatarId) return;

        void safeExecute(async () => {
            await changeAvatar(avatarId);
        });
    };

    // --- Category Logic ---
    const openAddCategory = () => {
        setEditingOriginalName(null);
        setEditingCatName('');
        setEditingCatWords([]);
        setCatModalOpen(true);
    };

    const openEditCategory = (cat: WordsCategoryDto) => {
        setEditingOriginalName(cat.name);
        setEditingCatName(cat.name);
        setEditingCatWords([...cat.words]);
        setCatModalOpen(true);
    };

    const handleDeleteCategory = (nameToRemove: string) => {
        if (!me.isHost || !confirm(`Видалити категорію "${nameToRemove}"?`)) return;
        const newCats = settings.wordsCategories.filter(c => c.name !== nameToRemove);
        updateSetting('wordsCategories', newCats);
    };

    const handleAddWordToBuffer = () => {
        if (!newWordInput.trim()) return;
        if (editingCatWords.includes(newWordInput.trim())) return;
        setEditingCatWords([...editingCatWords, newWordInput.trim()]);
        setNewWordInput('');
    };

    const handleRemoveWordFromBuffer = (word: string) => {
        setEditingCatWords(editingCatWords.filter(w => w !== word));
    };

    const handleSaveCategory = () => {
        if (!editingCatName.trim()) return alert("Назва категорії не може бути порожньою");
        if (editingCatWords.length < 3) return alert("Додайте мінімум 3 слова");

        let newCategories = [...settings.wordsCategories];

        if (editingOriginalName) {
            newCategories = newCategories.map(c =>
                c.name === editingOriginalName
                    ? { name: editingCatName, words: editingCatWords }
                    : c
            );
        } else {
            if (newCategories.some(c => c.name.toLowerCase() === editingCatName.toLowerCase())) {
                return alert("Категорія з такою назвою вже існує");
            }
            newCategories.push({ name: editingCatName, words: editingCatWords });
        }

        updateSetting('wordsCategories', newCategories);
        setCatModalOpen(false);
    };

    const allReady = players.length >= 3 && players.every(p => p.isReady);

    return (
        <div className="spy-lobby-page theme-spy">
            <div className="lobby-container">
                {/* Header */}
                <div className="lobby-header">
                    <div className="room-code-group">
                        <h2>Кімната</h2>
                        <div className="code-badge" onClick={copyCode} title="Клікніть, щоб скопіювати">
                            {roomCode}
                        </div>
                    </div>
                    <Button variant="danger" onClick={handleLeave} size="small">
                        Вийти
                    </Button>
                </div>

                <div className="lobby-content">
                    {/* Players Grid */}
                    <div className="section-panel">
                        <div className="section-title">
                            👥 Гравці ({players.length})
                        </div>
                        <div className="player-grid">
                            {players.map(p => (
                                <div key={p.id} className={`player-card ${p.isReady ? 'ready' : ''} ${p.isHost ? 'host' : ''}`}>
                                    {p.isHost && <div className="host-badge">👑 ХОСТ</div>}

                                    {/* Edit button only for ME */}
                                    {p.id === me.id && (
                                        <button className="edit-profile-btn" onClick={openProfileModal} title="Редагувати профіль">
                                            ✏️
                                        </button>
                                    )}

                                    <div className="player-avatar">
                                        {/* Відображаємо емодзі з мапи або дефолтний */}
                                        {AVATAR_MAP[p.avatarId] || AVATAR_MAP['default']}
                                    </div>
                                    <div className="player-name">
                                        {p.name} {p.id === me.id && '(Ви)'}
                                    </div>

                                    {p.isReady ? (
                                        <span className="ready-badge">✓ Готовий</span>
                                    ) : (
                                        <span className="not-ready-text">Не готовий</span>
                                    )}

                                    {/* Host Actions (Kick/Promote) - Safe execution */}
                                    {me.isHost && p.id !== me.id && (
                                        <div className="player-actions">
                                            <button
                                                className="icon-btn"
                                                title="Вигнати"
                                                onClick={() => safeExecute(async () => await kickPlayer(p.id))}
                                            >🚫</button>
                                            <button
                                                className="icon-btn"
                                                title="Передати права"
                                                onClick={() => safeExecute(async () => await changeHost(p.id))}
                                            >👑</button>
                                        </div>
                                    )}
                                </div>
                            ))}

                            {/* Empty slots filler */}
                            {Array.from({ length: Math.max(0, 8 - players.length) }).map((_, i) => (
                                <div key={`empty-${i}`} className="player-card empty-slot">
                                    <div className="player-avatar avatar-placeholder">❓</div>
                                    <div className="player-name">Очікування...</div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Settings Panel */}
                    <div className="section-panel">
                        <div className="section-title">⚙️ Налаштування</div>

                        <div className="settings-list">
                            <div className="setting-item">
                                <span>Час гри (хв)</span>
                                <div className="setting-control">
                                    <button className="btn-mini" onClick={() => modifyNumber('timerMinutes', -1, 1, 30)} disabled={!me.isHost}>-</button>
                                    <span className="val-display">{settings.timerMinutes}</span>
                                    <button className="btn-mini" onClick={() => modifyNumber('timerMinutes', 1, 1, 30)} disabled={!me.isHost}>+</button>
                                </div>
                            </div>

                            <div className="setting-item">
                                <span>Кількість шпигунів</span>
                                <div className="setting-control">
                                    <button className="btn-mini" onClick={() => modifyNumber('spiesCount', -1, 1, 3)} disabled={!me.isHost}>-</button>
                                    <span className="val-display">{settings.spiesCount}</span>
                                    <button className="btn-mini" onClick={() => modifyNumber('spiesCount', 1, 1, 3)} disabled={!me.isHost}>+</button>
                                </div>
                            </div>

                            <div className="setting-item">
                                <span>Шпигуни знають один одного</span>
                                <label className="switch">
                                    <input
                                        type="checkbox"
                                        checked={settings.spiesKnowEachOther}
                                        onChange={(e) => updateSetting('spiesKnowEachOther', e.target.checked)}
                                        disabled={!me.isHost || settings.spiesCount < 2}
                                    />
                                    <span className="slider"></span>
                                </label>
                            </div>

                            <div className="setting-item">
                                <span>Показувати категорію шпигунам</span>
                                <label className="switch">
                                    <input
                                        type="checkbox"
                                        checked={settings.showCategoryToSpy}
                                        onChange={(e) => updateSetting('showCategoryToSpy', e.target.checked)}
                                        disabled={!me.isHost}
                                    />
                                    <span className="slider"></span>
                                </label>
                            </div>

                            {/* Category Management */}
                            <div className="categories-section">
                                <div className="categories-header">
                                    <span>📚 Категорії слів</span>
                                </div>
                                <div className="category-list">
                                    {settings.wordsCategories.map((cat, idx) => (
                                        <div key={idx} className="category-item">
                                            <div>
                                                <span className="cat-name">{cat.name}</span>
                                                <span className="cat-count">({cat.words.length})</span>
                                            </div>
                                            {me.isHost && (
                                                <div className="cat-actions">
                                                    <button className="category-edit-btn" onClick={() => openEditCategory(cat)}>✏️</button>
                                                    <button className="category-remove-btn" onClick={() => handleDeleteCategory(cat.name)}>✕</button>
                                                </div>
                                            )}
                                        </div>
                                    ))}
                                    {settings.wordsCategories.length === 0 && (
                                        <div className="empty-categories-msg">Немає категорій</div>
                                    )}
                                </div>

                                {me.isHost && (
                                    <div className="add-category-btn-wrapper">
                                        <Button size="small" variant="secondary" fullWidth onClick={openAddCategory}>
                                            + Додати категорію
                                        </Button>
                                    </div>
                                )}
                            </div>
                        </div>

                        <div className="lobby-footer">
                            <Button
                                fullWidth
                                variant={me.isReady ? "danger" : "secondary"}
                                onClick={handleToggleReady}
                            >
                                {me.isReady ? "⏸️ Не готовий" : "✓ Я готовий"}
                            </Button>

                            {me.isHost && (
                                <Button
                                    fullWidth
                                    disabled={!allReady}
                                    onClick={handleStart}
                                >
                                    🎮 Почати гру
                                </Button>
                            )}
                            {me.isHost && !allReady && (
                                <div className="lobby-footer-msg">
                                    Всі гравці (мін. 3) мають бути готові
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {/* --- Profile Edit Modal --- */}
                <Modal
                    isOpen={isProfileModalOpen}
                    onClose={() => setProfileModalOpen(false)}
                    title="Мій Профіль"
                >
                    <div className="profile-modal-content">
                        {/* Name Section */}
                        <div className="form-group">
                            <label>Ваше ім'я</label>
                            <div className="name-edit-row">
                                <div className="input-wrapper">
                                    <input
                                        value={tempName}
                                        onChange={(e) => setTempName(e.target.value)}
                                        placeholder="Введіть ім'я"
                                        maxLength={15}
                                    />
                                </div>
                                <Button size="small" onClick={handleSaveName}>
                                    Зберегти
                                </Button>
                            </div>
                        </div>

                        {/* Avatar Section */}
                        <div className="avatar-selection-section">
                            <h4>Оберіть аватар</h4>
                            <div className="avatar-grid-select">
                                {AVAILABLE_AVATARS.map(avatarKey => (
                                    <div
                                        key={avatarKey}
                                        className={`avatar-option ${me.avatarId === avatarKey ? 'selected' : ''}`}
                                        onClick={() => handleSelectAvatar(avatarKey)}
                                    >
                                        {AVATAR_MAP[avatarKey]}
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="modal-actions">
                            <Button variant="secondary" fullWidth onClick={() => setProfileModalOpen(false)}>
                                Закрити
                            </Button>
                        </div>
                    </div>
                </Modal>

                {/* --- Edit Category Modal --- */}
                <Modal
                    isOpen={isCatModalOpen}
                    onClose={() => setCatModalOpen(false)}
                    title={editingOriginalName ? "Редагувати категорію" : "Нова категорія"}
                >
                    <div className="category-modal-content">
                        <div className="form-group">
                            <label>Назва категорії</label>
                            <input
                                value={editingCatName}
                                onChange={(e) => setEditingCatName(e.target.value)}
                                placeholder="Наприклад: Тварини"
                            />
                        </div>

                        <div className="form-group">
                            <label>Слова ({editingCatWords.length})</label>
                            <div className="words-input-group">
                                <input
                                    value={newWordInput}
                                    onChange={(e) => setNewWordInput(e.target.value)}
                                    placeholder="Нове слово..."
                                    onKeyDown={(e) => e.key === 'Enter' && handleAddWordToBuffer()}
                                />
                                <Button size="small" onClick={handleAddWordToBuffer}>+</Button>
                            </div>

                            <div className="words-manager">
                                <div className="word-chips">
                                    {editingCatWords.map((word, idx) => (
                                        <div key={idx} className="word-chip">
                                            {word}
                                            <button onClick={() => handleRemoveWordFromBuffer(word)}>×</button>
                                        </div>
                                    ))}
                                    {editingCatWords.length === 0 && (
                                        <span style={{color: '#666', fontSize: 13, padding: 5}}>Список порожній</span>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="modal-actions">
                            <Button variant="secondary" onClick={() => setCatModalOpen(false)}>Скасувати</Button>
                            <Button onClick={handleSaveCategory}>Зберегти</Button>
                        </div>
                    </div>
                </Modal>
            </div>
        </div>
    );
};
