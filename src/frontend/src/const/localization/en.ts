export const en = {
    common: {
        copyToClipboard: "Click to copy"
    },
    home: {
        logo: "HiveHub",
        greeting: "Hello, Gamer",
        gamesLibrary: "Games Library",
        spyGame: {
            title: "Find the Spy",
            description: "Psychological game. Find the traitor or deceive everyone.",
            players: "3-8 players",
            duration: "~15 min"
        },
        crocodile: {
            title: "Crocodile (Coming Soon)",
            description: "Draw and guess words with friends."
        },
        uno: {
            title: "Uno Online (Coming Soon)",
            description: "Classic card game."
        }
    },
    spyGame: {
        entry: {
            title: "Find the Spy",
            subtitle: "Who among you is the traitor? Figure them out!",
            connectingToServer: "⏳ Connecting to server...",
            createRoom: "Create Room",
            joinGame: "Join Game",
            backToHiveHub: "← Back to HiveHub",
            features: {
                players: "3-8 players",
                duration: "5-8 minutes game",
                noRegistration: "No registration"
            },
            joinModal: {
                title: "Join Game",
                description: "Enter the room code provided by the game host.",
                roomCodeLabel: "Room Code",
                roomCodePlaceholder: "ABC12345",
                joinButton: "Join"
            },
            errors: {
                createRoom: "Room creation error: ",
                joinRoom: "Join error: ",
                unknownError: "Unknown error",
                invalidCode: "Enter a valid room code"
            }
        },
        lobby: {
            room: "Room",
            leave: "Leave",
            leaveConfirm: "Leave room?",
            loadingLobby: "Loading Lobby...",
            tabs: {
                settings: "⚙️ Settings",
                chat: "💬 Chat"
            },
            errors: {
                generic: "Error: "
            }
        },
        settings: {
            gameTime: "Game time (min)",
            maxPlayers: "Max. players",
            minSpies: "Min. spies",
            maxSpies: "Max. spies",
            spiesKnowEachOther: "Spies know each other",
            showCategoryToSpy: "Spies see category",
            spiesPlayAsTeam: "Spies play as team",
            categories: {
                title: "📚 Word Categories",
                noCategories: "No categories",
                addCategory: "+ Add category",
                uploadFromFile: "📁 Upload from file",
                fileFormatHint: "Format: category: word1, word2, word3",
                wordCount: "({count})",
                editButton: "✏️",
                removeButton: "✕"
            },
            categoryModal: {
                titleEdit: "Edit category",
                titleNew: "New category",
                categoryName: "Category name",
                categoryPlaceholder: "For example: Animals",
                words: "Words",
                newWordPlaceholder: "New word...",
                addWord: "+",
                cancel: "Cancel",
                save: "Save",
                errors: {
                    enterCategoryName: "Enter category name",
                    minWords: "Add at least 3 words",
                    maxCategories: "Maximum {count} categories",
                    categoryExists: "Category with this name already exists",
                    wordTooLong: "Word is too long (max. {max} characters)"
                }
            },
            viewCategoryModal: {
                title: "Category",
                close: "Close"
            },
            fileUpload: {
                selectTxt: "Please select a TXT file",
                noCategoriesFound: "No categories found in file",
                limitExceeded: "Category limit exceeded (max. {max})",
                categoriesAdded: "Added {count} categories",
                allExist: "All categories from file already exist",
                readError: "File reading error. Check format."
            }
        },
        game: {
            unknownError: "Unknown error",
            error: "Error: ",
            toast: {
                youCaught: "⚠️ YOU'VE BEEN CAUGHT! Guess the location to win!",
                spyCaught: "🕵️ Spy caught! They're trying to guess the location...",
                votingStarted: "🗳️ Voting started against: ",
                playerWrongGuess: "❌ {name} guessed wrong and is eliminated!"
            },
            lastChanceBanner: {
                youCaught: "YOU'VE BEEN CAUGHT!",
                spyCaught: "SPY CAUGHT: ",
                youCaughtDesc: "You have one last chance: guess the location to win!",
                spyCaughtDesc: "Spy is choosing a location. If they guess — spies win!"
            },
            actions: {
                toLobbyAll: "🛑 To Lobby (All)",
                toLobbyConfirm: "Return everyone to lobby?",
                leaveGame: "🚪 Leave Game",
                leaveConfirm: "Leave room?"
            }
        },
        rules: {
            title: "💡 How to play?",
            rules: [
                "Take turns asking each other questions about the secret word.",
                "Questions should be not too direct, so the spy won't figure it out.",
                "But also not too abstract, so others understand you're \"one of them\".",
                "If you suspect someone — press \"Accuse\" next to their name!",
                "The spy can try to guess the word to win earlier.",
                "Vote to stop the timer to discuss suspicions."
            ]
        },
        roleCard: {
            youAreDead: "YOU ARE DEAD",
            youAreSpy: "YOU ARE SPY",
            civilian: "Civilian",
            deadDescription: "You lost and can no longer affect the game. But you can watch the game in chat!",
            spyDescription: "Your goal: find out the word from others' conversations or last until the end without revealing yourself.",
            civilianDescription: "Your goal: find the spy among players by asking leading questions.",
            category: "Category: ",
            guessWord: "💡 Guess word"
        },
        header: {
            timeLeft: "Time left",
            timerStopped: "Timer stopped",
            room: "ROOM: ",
            pause: "PAUSE",
            stopped: "STOPPED",
            voteStop: "⏸️ Stop",
            youVoted: "You voted",
            votes: "Votes: "
        },
        guessWord: {
            title: "💡 Guess word",
            lastChanceTitle: "🔥 LAST CHANCE",
            timer: "⏱️ {time} sec",
            warningNormal: "ATTENTION! You have only one attempt. Correct answer brings victory, wrong answer — defeat for all spies!",
            warningLastChance: "You've been caught! This is your only chance to win.",
            category: "Category:",
            placeholder: "Enter word...",
            cancel: "Cancel",
            guess: "Guess",
            tryLuck: "Try luck",
            enterWord: "Enter word!",
            confirmGuess: "Secret word: \"{word}\"?"
        },
        finalVoting: {
            title: "🗳️ Final Voting ({time}s)",
            timeUp: "Time's up!",
            description: "Choose the player you suspect of being a spy, or skip",
            youVotedFor: "You voted for: ",
            youSkipped: "You skipped voting",
            waitingForOthers: "Waiting for other players...",
            skip: "⏭️ SKIP (No suspicions)",
            orChoosePlayer: "or choose a player"
        },
        results: {
            title: "Game Over!",
            category: "Category:",
            secretWord: "Secret word:",
            you: "(You)",
            offline: "[Offline]",
            spy: "SPY",
            civilian: "Civilian",
            dead: "💀",
            actions: {
                toLobby: "🛋️ To Lobby",
                playAgain: "🔄 Play Again",
                playAgainConfirm: "Play again?",
                leaveRoom: "🚪 Leave Room",
                leaveConfirm: "Leave?"
            },
            endReasons: {
                roundTimeExpired: "Time's up! Spies were not found.",
                civilianKicked: "Civilian was kicked by mistake!",
                spyGuessedWord: "Spy guessed the secret word!",
                spyWrongGuess: "Spy didn't guess the word!",
                finalVoteFailed: "Final vote failed!",
                allSpiesEliminated: "All spies were eliminated!",
                spyLastChanceFailed: "Spy was caught and didn't guess the word!",
                paranoiaSacrifice: "In Paranoia mode, an innocent was kicked!",
                paranoiaSurvived: "Civilians survived in Paranoia mode!",
                insufficientPlayers: "Not enough players to continue the game."
            },
            teams: {
                civilians: "Civilians won",
                spies: "Spies won"
            }
        },
        players: {
            title: "👥 Players",
            hostBadge: "👑 HOST",
            you: "(You)",
            ready: "✓ Ready",
            notReady: "Not ready",
            waitingForPlayers: "Waiting...",
            connectionLost: "Connection lost",
            offline: "🔌 Offline",
            allySpyTooltip: "Ally-spy",
            votedToStopTooltip: "Voted to stop",
            caughtSpyTooltip: "Caught spy",
            deadTooltip: "Dead",
            accuse: "⚠️ Accuse",
            actions: {
                ready: "✓ I'm ready",
                notReady: "⏸️ Not ready",
                startGame: "🎮 Start Game",
                allPlayersMustBeReady: "All players must be ready"
            },
            profile: {
                title: "My Profile",
                yourName: "Your name",
                namePlaceholder: "Enter name",
                save: "Save",
                selectAvatar: "Select avatar",
                close: "Close",
                errors: {
                    emptyName: "Name cannot be empty"
                }
            },
            kick: "🚫",
            makeHost: "👑"
        },
        chat: {
            title: "💬 Chat",
            noMessages: "No messages yet...",
            messagePlaceholder: "Message...",
            sendButton: "📤"
        },
        accusationVoting: {
            title: "⚖️ Voting",
            timeLeft: "Time left: {time} sec",
            accusedOfSpying: "accused of being a spy",
            youAccused: "you are accused of being a spy!",
            youAreAccused: "YOU ARE ACCUSED!",
            youAreAccusedDesc: "You cannot vote. Defend yourself in chat!",
            youVoted: "You voted: ",
            waitingForOthers: "Waiting for other players...",
            voteYes: "✅ YES (Spy)",
            voteNo: "❌ NO (Not a spy)",
            voteSkip: "⏭️ SKIP",
            votes: {
                yes: "FOR (Spy)",
                no: "AGAINST (Not spy)",
                skip: "ABSTAINED"
            }
        },
    },
};