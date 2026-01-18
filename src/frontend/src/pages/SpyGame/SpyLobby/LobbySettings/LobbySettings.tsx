import { useState, useRef } from 'react';
import { Button } from '../../../../components/ui/Button/Button';
import { Modal } from '../../../../components/ui/Modal/Modal';
import type { SpyGameRulesDto, SpyGameWordPacksDto, WordsCategoryDto } from '../../../../models/spy-game';
import './LobbySettings.scss';
import { en } from '../../../../const/localization/en';

const PROJECT_CONSTANTS = {
    SPY_GAME: {
        MAX_PLAYERS_COUNT: 8,
        MAX_GAME_DURATION_MINUTES: 10,
        MIN_GAME_DURATION_MINUTES: 1,
        MAX_CUSTOM_CATEGORIES_COUNT: 10,
        MAX_WORD_IN_CATEGORY_LENGTH: 30,
    }
};

interface LobbySettingsProps {
    rules: SpyGameRulesDto;
    wordPacks: SpyGameWordPacksDto;
    isHost: boolean;
    onUpdateRules: (updates: Partial<SpyGameRulesDto>) => void;
    onUpdateWordPacks: (packs: SpyGameWordPacksDto) => void;
}

export const LobbySettings = ({ rules, wordPacks, isHost, onUpdateRules, onUpdateWordPacks }: LobbySettingsProps) => {
    const [isCatModalOpen, setCatModalOpen] = useState(false);
    const [isViewCatModalOpen, setViewCatModalOpen] = useState(false);
    const [viewingCategory, setViewingCategory] = useState<WordsCategoryDto | null>(null);
    const [editingCatName, setEditingCatName] = useState('');
    const [editingCatWords, setEditingCatWords] = useState<string[]>([]);
    const [editingOriginalName, setEditingOriginalName] = useState<string | null>(null);
    const [newWordInput, setNewWordInput] = useState('');
    const fileInputRef = useRef<HTMLInputElement>(null);

    const t = en.spyGame.settings;

    const modifyNumber = (
        key: keyof SpyGameRulesDto,
        delta: number,
        minLimit: number,
        maxLimit: number
    ) => {
        if (!isHost) return;

        const currentValue = rules[key];
        if (typeof currentValue !== 'number') return;

        let nextValue = currentValue + delta;
        nextValue = Math.max(minLimit, Math.min(maxLimit, nextValue));

        if (key === 'minSpiesCount' && nextValue > rules.maxSpiesCount) {
            nextValue = rules.maxSpiesCount;
        }
        if (key === 'maxSpiesCount' && nextValue < rules.minSpiesCount) {
            nextValue = rules.minSpiesCount;
        }

        if (nextValue !== currentValue) {
            onUpdateRules({ [key]: nextValue });
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

    const openViewCategory = (cat: WordsCategoryDto) => {
        setViewingCategory(cat);
        setViewCatModalOpen(true);
    };

    const handleDeleteCategory = (nameToRemove: string) => {
        if (!isHost || !confirm(t.categoryModal.errors.enterCategoryName)) return;
        const newCats = wordPacks.customCategories.filter(c => c.name !== nameToRemove);
        onUpdateWordPacks({ customCategories: newCats });
    };

    const handleAddWordToBuffer = () => {
        if (!newWordInput.trim()) return;
        const word = newWordInput.trim();

        if (word.length > PROJECT_CONSTANTS.SPY_GAME.MAX_WORD_IN_CATEGORY_LENGTH) {
            alert(t.categoryModal.errors.wordTooLong.replace('{max}', String(PROJECT_CONSTANTS.SPY_GAME.MAX_WORD_IN_CATEGORY_LENGTH)));
            return;
        }

        if (editingCatWords.includes(word)) return;
        setEditingCatWords([...editingCatWords, word]);
        setNewWordInput('');
    };

    const handleRemoveWordFromBuffer = (word: string) => {
        setEditingCatWords(editingCatWords.filter(w => w !== word));
    };

    const handleSaveCategory = () => {
        if (!editingCatName.trim()) return alert(t.categoryModal.errors.enterCategoryName);
        if (editingCatWords.length < 3) return alert(t.categoryModal.errors.minWords);

        if (wordPacks.customCategories.length >= PROJECT_CONSTANTS.SPY_GAME.MAX_CUSTOM_CATEGORIES_COUNT && !editingOriginalName) {
            return alert(t.categoryModal.errors.maxCategories.replace('{count}', String(PROJECT_CONSTANTS.SPY_GAME.MAX_CUSTOM_CATEGORIES_COUNT)));
        }

        let newCategories = [...wordPacks.customCategories];

        if (editingOriginalName) {
            newCategories = newCategories.map(c =>
                c.name === editingOriginalName
                    ? { name: editingCatName, words: editingCatWords }
                    : c
            );
        } else {
            if (newCategories.some(c => c.name.toLowerCase() === editingCatName.toLowerCase())) {
                return alert(t.categoryModal.errors.categoryExists);
            }
            newCategories.push({ name: editingCatName, words: editingCatWords });
        }

        onUpdateWordPacks({ customCategories: newCategories });
        setCatModalOpen(false);
    };

    const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        if (!file.name.endsWith('.txt')) {
            alert(t.fileUpload.selectTxt);
            return;
        }

        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const content = e.target?.result as string;
                const parsedCategories = parseWordsFile(content);

                if (parsedCategories.length === 0) {
                    alert(t.fileUpload.noCategoriesFound);
                    return;
                }

                const totalCategories = wordPacks.customCategories.length + parsedCategories.length;
                if (totalCategories > PROJECT_CONSTANTS.SPY_GAME.MAX_CUSTOM_CATEGORIES_COUNT) {
                    alert(t.fileUpload.limitExceeded.replace('{max}', String(PROJECT_CONSTANTS.SPY_GAME.MAX_CUSTOM_CATEGORIES_COUNT)));
                    return;
                }

                const newCategories = [...wordPacks.customCategories];
                let addedCount = 0;

                for (const cat of parsedCategories) {
                    if (newCategories.some(c => c.name.toLowerCase() === cat.name.toLowerCase())) {
                        continue;
                    }
                    newCategories.push(cat);
                    addedCount++;
                }

                if (addedCount > 0) {
                    onUpdateWordPacks({ customCategories: newCategories });
                    alert(t.fileUpload.categoriesAdded.replace('{count}', String(addedCount)));
                } else {
                    alert(t.fileUpload.allExist);
                }
            } catch (error) {
                alert(t.fileUpload.readError);
                console.error(error);
            }
        };
        reader.readAsText(file);

        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const parseWordsFile = (content: string): WordsCategoryDto[] => {
        const lines = content.split('\n').map(l => l.trim()).filter(Boolean);
        const categories: WordsCategoryDto[] = [];

        for (const line of lines) {
            if (!line.includes(':')) continue;

            const [categoryName, wordsStr] = line.split(':').map(s => s.trim());
            if (!categoryName || !wordsStr) continue;

            const words = wordsStr
                .split(',')
                .map(w => w.trim())
                .filter(w => w.length > 0 && w.length <= PROJECT_CONSTANTS.SPY_GAME.MAX_WORD_IN_CATEGORY_LENGTH);

            if (words.length >= 3) {
                categories.push({ name: categoryName, words });
            }
        }

        return categories;
    };

    return (
        <div className="settings-container">
            <div className="settings-list">
                <div className="setting-item">
                    <span>{t.gameTime}</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('timerMinutes', -1,
                                PROJECT_CONSTANTS.SPY_GAME.MIN_GAME_DURATION_MINUTES,
                                PROJECT_CONSTANTS.SPY_GAME.MAX_GAME_DURATION_MINUTES)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{rules.timerMinutes}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('timerMinutes', 1,
                                PROJECT_CONSTANTS.SPY_GAME.MIN_GAME_DURATION_MINUTES,
                                PROJECT_CONSTANTS.SPY_GAME.MAX_GAME_DURATION_MINUTES)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>{t.maxPlayers}</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxPlayersCount', -1, 3, PROJECT_CONSTANTS.SPY_GAME.MAX_PLAYERS_COUNT)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{rules.maxPlayersCount}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxPlayersCount', 1, 3, PROJECT_CONSTANTS.SPY_GAME.MAX_PLAYERS_COUNT)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>{t.minSpies}</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('minSpiesCount', -1, 0, rules.maxSpiesCount)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{rules.minSpiesCount}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('minSpiesCount', 1, 1, rules.maxSpiesCount)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>{t.maxSpies}</span>
                    <div className="setting-control">
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxSpiesCount', -1, rules.minSpiesCount, 5)}
                            disabled={!isHost}
                        >
                            -
                        </button>
                        <span className="val-display">{rules.maxSpiesCount}</span>
                        <button
                            className="btn-mini"
                            onClick={() => modifyNumber('maxSpiesCount', 1, rules.minSpiesCount, 5)}
                            disabled={!isHost}
                        >
                            +
                        </button>
                    </div>
                </div>

                <div className="setting-item">
                    <span>{t.spiesKnowEachOther}</span>
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={rules.isSpiesKnowEachOther}
                            onChange={(e) => onUpdateRules({ isSpiesKnowEachOther: e.target.checked })}
                            disabled={!isHost}
                        />
                        <span className="slider"></span>
                    </label>
                </div>

                <div className="setting-item">
                    <span>{t.showCategoryToSpy}</span>
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={rules.isShowCategoryToSpy}
                            onChange={(e) => onUpdateRules({ isShowCategoryToSpy: e.target.checked })}
                            disabled={!isHost}
                        />
                        <span className="slider"></span>
                    </label>
                </div>

                <div className="setting-item">
                    <span>{t.spiesPlayAsTeam}</span>
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={rules.isSpiesPlayAsTeam}
                            onChange={(e) => onUpdateRules({ isSpiesPlayAsTeam: e.target.checked })}
                            disabled={!isHost}
                        />
                        <span className="slider"></span>
                    </label>
                </div>

                <div className="categories-section">
                    <div className="categories-header">
                        <span>{t.categories.title}</span>
                    </div>
                    <div className="category-list">
                        {wordPacks.customCategories.map((cat, idx) => (
                            <div key={idx} className="category-item">
                                <div className="cat-info" onClick={() => openViewCategory(cat)} style={{ cursor: 'pointer' }}>
                                    <span className="cat-name">{cat.name}</span>
                                    <span className="cat-count">{t.categories.wordCount.replace('{count}', String(cat.words.length))}</span>
                                </div>
                                {isHost && (
                                    <div className="cat-actions">
                                        <button className="category-edit-btn" onClick={() => openEditCategory(cat)}>
                                            {t.categories.editButton}
                                        </button>
                                        <button className="category-remove-btn" onClick={() => handleDeleteCategory(cat.name)}>
                                            {t.categories.removeButton}
                                        </button>
                                    </div>
                                )}
                            </div>
                        ))}
                        {wordPacks.customCategories.length === 0 && (
                            <div className="empty-categories-msg">{t.categories.noCategories}</div>
                        )}
                    </div>

                    {isHost && (
                        <div className="category-actions-wrapper">
                            <Button size="small" variant="secondary" fullWidth onClick={openAddCategory}>
                                {t.categories.addCategory}
                            </Button>
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept=".txt"
                                onChange={handleFileUpload}
                                style={{ display: 'none' }}
                            />
                            <Button
                                size="small"
                                variant="secondary"
                                fullWidth
                                onClick={() => fileInputRef.current?.click()}
                                className="mt-1"
                            >
                                {t.categories.uploadFromFile}
                            </Button>
                            <div className="file-format-hint">
                                {t.categories.fileFormatHint}
                            </div>
                        </div>
                    )}
                </div>
            </div>

            <Modal
                isOpen={isCatModalOpen}
                onClose={() => setCatModalOpen(false)}
                title={editingOriginalName ? t.categoryModal.titleEdit : t.categoryModal.titleNew}
            >
                <div className="category-modal-content">
                    <div className="form-group">
                        <label>{t.categoryModal.categoryName}</label>
                        <input
                            value={editingCatName}
                            onChange={(e) => setEditingCatName(e.target.value)}
                            placeholder={t.categoryModal.categoryPlaceholder}
                        />
                    </div>
                    <div className="form-group">
                        <label>{t.categoryModal.words} ({editingCatWords.length})</label>
                        <div className="words-input-group">
                            <input
                                value={newWordInput}
                                onChange={(e) => setNewWordInput(e.target.value)}
                                placeholder={t.categoryModal.newWordPlaceholder}
                                maxLength={PROJECT_CONSTANTS.SPY_GAME.MAX_WORD_IN_CATEGORY_LENGTH}
                                onKeyDown={(e) => e.key === 'Enter' && handleAddWordToBuffer()}
                            />
                            <Button size="small" onClick={handleAddWordToBuffer}>{t.categoryModal.addWord}</Button>
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
                        <Button variant="secondary" onClick={() => setCatModalOpen(false)}>{t.categoryModal.cancel}</Button>
                        <Button onClick={handleSaveCategory}>{t.categoryModal.save}</Button>
                    </div>
                </div>
            </Modal>

            <Modal
                isOpen={isViewCatModalOpen}
                onClose={() => setViewCatModalOpen(false)}
                title={viewingCategory?.name || t.viewCategoryModal.title}
            >
                <div className="view-category-content">
                    <div className="words-view-grid">
                        {viewingCategory?.words.map((word, idx) => (
                            <div key={idx} className="word-view-chip">
                                {word}
                            </div>
                        ))}
                    </div>
                    <div className="modal-actions">
                        <Button variant="secondary" fullWidth onClick={() => setViewCatModalOpen(false)}>
                            {t.viewCategoryModal.close}
                        </Button>
                    </div>
                </div>
            </Modal>
        </div>
    );
};