import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../../components/ui/Button/Button';
import { SpyGameEndReason, SpyGameTeam } from '../../../models/spy-game';
import './SpyResults.scss';
import { useSpyGame } from "../../../context/spy-game/SpyGameContext.tsx";
import { SpyGameChat } from "../SpyGame/SpyGameChat/SpyGameChat.tsx";
import { RoomStatus } from "../../../models/shared.ts";
import { en } from '../../../const/localization/en';

const END_REASON_TEXT: Record<SpyGameEndReason, string> = {
    [SpyGameEndReason.RoundTimeExpired]: en.spyGame.results.endReasons.roundTimeExpired,
    [SpyGameEndReason.CivilianKicked]: en.spyGame.results.endReasons.civilianKicked,
    [SpyGameEndReason.SpyGuessedWord]: en.spyGame.results.endReasons.spyGuessedWord,
    [SpyGameEndReason.SpyWrongGuess]: en.spyGame.results.endReasons.spyWrongGuess,
    [SpyGameEndReason.FinalVoteFailed]: en.spyGame.results.endReasons.finalVoteFailed,
    [SpyGameEndReason.AllSpiesEliminated]: en.spyGame.results.endReasons.allSpiesEliminated,
    [SpyGameEndReason.SpyLastChanceFailed]: en.spyGame.results.endReasons.spyLastChanceFailed,
    [SpyGameEndReason.ParanoiaSacrifice]: en.spyGame.results.endReasons.paranoiaSacrifice,
    [SpyGameEndReason.ParanoiaSurvived]: en.spyGame.results.endReasons.paranoiaSurvived,
    [SpyGameEndReason.InsufficientPlayers]: en.spyGame.results.endReasons.insufficientPlayers,
};

const TEAM_TEXT: Record<SpyGameTeam, string> = {
    [SpyGameTeam.Civilians]: en.spyGame.results.teams.civilians,
    [SpyGameTeam.Spies]: en.spyGame.results.teams.spies,
};

export const SpyResults = () => {
    const navigate = useNavigate();
    const {
        isInitializing, roomCode, players, spiesReveal, returnToLobby,
        leaveRoom, roomState, startGame, me, messages, sendMessage,
        winnerTeam, gameEndReason, gameEndMessage, gameState,
    } = useSpyGame();

    const t = en.spyGame.results;

    const safeExecute = async (action: () => Promise<void>) => {
        try { await action(); } catch (error) { console.error(error); }
    };

    useEffect(() => {
        if (isInitializing) return;
        if (!roomCode) { navigate('/spy'); return; }
        if (roomState === RoomStatus.Lobby) navigate('/spy/lobby');
        else if (roomState === RoomStatus.InGame) navigate('/spy/game');
    }, [roomCode, roomState, navigate, isInitializing]);

    const secretWord = gameState?.currentSecretWord;
    const category = gameState?.currentCategory;

    return (
        <div className="spy-results">
            <div className="spy-results__container">
                <div className="spy-results__panel">
                    <div className="spy-card">
                        <div className="spy-card__header">
                            <div className="spy-header">
                                <div className="icon-wrapper">
                                    {winnerTeam === SpyGameTeam.Spies ? '🥷' : '🕵️'}
                                </div>
                                <h1>{t.title}</h1>
                                {winnerTeam && <p className={winnerTeam == SpyGameTeam.Civilians
                                    ? "winner-text winner-text-civil"
                                    : "winner-text winner-text-spies"}>{TEAM_TEXT[winnerTeam]}</p>}
                                {gameEndReason && (
                                    <p className="reason-text">
                                        {END_REASON_TEXT[gameEndReason] || gameEndMessage}
                                    </p>
                                )}
                            </div>

                            {(secretWord || category) && (
                                <div className="secret-info">
                                    {category && (
                                        <div className="secret-item">
                                            <span className="secret-label">{t.category}</span>
                                            <span className="secret-value">{category}</span>
                                        </div>
                                    )}
                                    {secretWord && (
                                        <div className="secret-item">
                                            <span className="secret-label">{t.secretWord}</span>
                                            <span className="secret-value secret-word">{secretWord}</span>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        <div className="results-list">
                            {spiesReveal.map(reveal => {
                                const player = players.find(p => p.id === reveal.playerId);
                                const isOnline = player?.isConnected ?? false;
                                const isMe = me?.id === reveal.playerId;

                                const itemClasses = [
                                    'result-item',
                                    reveal.isSpy ? 'is-spy' : 'is-civilian',
                                    !isOnline ? 'is-offline' : '',
                                    reveal.isDead ? 'is-dead' : ''
                                ].filter(Boolean).join(' ');

                                return (
                                    <div key={reveal.playerId} className={itemClasses}>
                                        <div className="player-info">
                                            <div className="role-icon">
                                                {reveal.isDead ? t.dead : reveal.isSpy ? '🥷' : '🕵️'}
                                            </div>
                                            <div className="player-name">
                                                {reveal.playerName} {isMe && t.you}
                                                {!isOnline && ` ${t.offline}`}
                                            </div>
                                        </div>
                                        <div className="role-label">
                                            {reveal.isSpy ? t.spy : t.civilian}
                                        </div>
                                    </div>
                                );
                            })}
                        </div>

                        <div className="spy-actions">
                            {me?.isHost && (
                                <>
                                    <Button fullWidth onClick={() => safeExecute(async () => { await returnToLobby(); navigate('/spy/lobby'); })} variant="secondary">
                                        {t.actions.toLobby}
                                    </Button>
                                    <Button fullWidth onClick={() => { if(confirm(t.actions.playAgainConfirm)) safeExecute(startGame); }}>
                                        {t.actions.playAgain}
                                    </Button>
                                </>
                            )}
                            <Button fullWidth variant="secondary" onClick={() => { if(confirm(t.actions.leaveConfirm)) safeExecute(async () => { await leaveRoom(); navigate('/spy'); }); }}>
                                {t.actions.leaveRoom}
                            </Button>
                        </div>
                    </div>
                </div>

                <div className="spy-results__chat">
                    <SpyGameChat
                        messages={messages}
                        currentPlayerId={me?.id || ''}
                        onSendMessage={sendMessage}
                    />
                </div>
            </div>
        </div>
    );
};