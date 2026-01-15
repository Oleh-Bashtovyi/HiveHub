import { useState } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import { Modal } from '../../../../components/ui/Modal/Modal';
import type { SpyRoomGameSettingsDto, WordsCategoryDto } from '../../../../models/spy-game';

interface LobbySettingsProps {
    settings: SpyRoomGameSettingsDto;
    isHost: boolean;
    onUpdateSettings: (updates: Partial<SpyRoomGameSettingsDto>) => void;
}

export const LobbySettings = ({ settings, isHost, onUpdateSettings }: LobbySettingsProps) => {
    const [isCatModalOpen, setCatModalOpen] = useState(false);
    const [editingCatName, setEditingCatName] = useState('');
    const [editingCatWords, setEditingCatWords] = useState<string[]>([]);
    const [editingOriginalName, setEditingOriginalName] = useState<string | null>(null);
    const [newWordInput, setNewWordInput] = useState('');

    const modifyNumber = (
        key: keyof SpyRoomGameSettingsDto,
        delta: number,
        minLimit: number,
        maxLimit: number
    ) => {
        if (!isHost) return;

        const currentValue = settings[key];
        if (typeof currentValue !== 'number') return;

        let nextValue = currentValue + delta;
        nextValue = Math.max(minLimit, Math.min(maxLimit, nextValue));

        if (key === 'minSpiesCount' && nextValue > settings.maxSpiesCount) {
            nextValue = settings.maxSpiesCount;
        }
        if (key === 'maxSpiesCount' && nextValue < settings.minSpiesCount) {
            nextValue = settings.minSpiesCount;
        }

        if (nextValue !== currentValue) {
            onUpdateSettings({ [key]: nextValue });
        }
    };

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
        if (!isHost || !confirm(`Delete category "${nameToRemove}"?`)) return;
        const newCats = settings.customCategories.filter(c => c.name !== nameToRemove);
        onUpdateSettings({ customCategories: newCats });
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
        if (!editingCatName.trim()) return alert("Category name required");
        if (editingCatWords.length < 3) return alert("Add at least 3 words");

        let newCategories = [...settings.customCategories];

        if (editingOriginalName) {
            newCategories = newCategories.map(c =>
                c.name === editingOriginalName
                    ? { name: editingCatName, words: editingCatWords }
                    : c
            );
        } else {
            if (newCategories.some(c => c.name.toLowerCase() === editingCatName.toLowerCase())) {
                return alert("Category already exists");
            }
            newCategories.push({ name: editingCatName, words: editingCatWords });
        }

        onUpdateSettings({ customCategories: newCategories });
        setCatModalOpen(false);
    };

    return (
        <div className="settings-container">
            <div className="settings-list">
                <div className="setting-item">
                    <span>Час гри (хв)</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('timerMinutes', -1, 1, 30)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{settings.timerMinutes}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('timerMinutes', 1, 1, 30)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>Мін. шпигунів</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('minSpiesCount', -1, 0, 7)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{settings.minSpiesCount}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('minSpiesCount', 1, 1, 8)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>Макс. шпигунів</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxSpiesCount', -1, 1, 5)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{settings.maxSpiesCount}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxSpiesCount', 1, 1, 5)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>Шпигуни знають один одного</span>
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={settings.spiesKnowEachOther}
                            onChange={(e) => onUpdateSettings({ spiesKnowEachOther: e.target.checked })}
                            disabled={!isHost}
                        />
                        <span className="slider"></span>
                    </label>
                </div>

                <div className="setting-item">
                    <span>Категорія для шпигунів</span>
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={settings.showCategoryToSpy}
                            onChange={(e) => onUpdateSettings({ showCategoryToSpy: e.target.checked })}
                            disabled={!isHost}
                        />
                        <span className="slider"></span>
                    </label>
                </div>

                <div className="categories-section">
                    <div className="categories-header">
                        <span>📚 Категорії слів</span>
                    </div>
                    <div className="category-list">
                        {settings.customCategories.map((cat, idx) => (
                            <div key={idx} className="category-item">
                                <div>
                                    <span className="cat-name">{cat.name}</span>
                                    <span className="cat-count">({cat.words.length})</span>
                                </div>
                                {isHost && (
                                    <div className="cat-actions">
                                        <button className="category-edit-btn" onClick={() => openEditCategory(cat)}>
                                            ✏️
                                        </button>
                                        <button className="category-remove-btn" onClick={() => handleDeleteCategory(cat.name)}>
                                            ✕
                                        </button>
                                    </div>
                                )}
                            </div>
                        ))}
                        {settings.customCategories.length === 0 && (
                            <div className="empty-categories-msg">Немає категорій</div>
                        )}
                    </div>

                    {isHost && (
                        <div className="add-category-btn-wrapper">
                            <Button size="small" variant="secondary" fullWidth onClick={openAddCategory}>
                                + Додати категорію
                            </Button>
                        </div>
                    )}
                </div>
            </div>

            {/* Edit Category Modal */}
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
                                        {word} <button onClick={() => handleRemoveWordFromBuffer(word)}>×</button>
                                    </div>
                                ))}
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
    );
};