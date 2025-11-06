/**
 * Main Game Controller for 3D Chess
 * Integrates all components: 3D engine, chess logic, multiplayer, and UI
 */

class ChessGame {
    constructor() {
        this.chessLogic = new ChessLogic();
        this.chess3D = null;
        this.multiplayer = new MultiplayerClient();
        this.isInitialized = false;
        this.selectedPiece = null;
        this.validMoves = [];
        this.gamePhase = 'waiting'; // waiting, placement, playing, ended
        this.selectedPieceType = null; // For placement phase
        this.myColor = null;
        this.gameSettings = {
            boardOpacity: 0.8,
            enableShadows: true,
            enableAntialiasing: true,
            enableAnimations: true,
            enableSounds: true,
            soundVolume: 0.7,
            showValidMoves: true,
            showThreats: true,
            confirmMoves: true,
            cameraSpeed: 1.0
        };
        
        this.init();
    }

    async init() {
        try {
            // Show loading overlay
            this.showLoading(true);
            
            // Initialize 3D engine
            await this.initialize3DEngine();
            
            // Setup event listeners
            this.setupEventListeners();
            
            // Setup multiplayer callbacks
            this.setupMultiplayerCallbacks();
            
            // Setup UI
            this.setupUI();
            
            // Setup placement phase handlers
            this.setupPlacementHandlers();
            
            // Auto-join room if room code in URL
            this.handleURLRoomCode();
            
            this.isInitialized = true;
            
            // Hide loading overlay
            this.showLoading(false);
            
            console.log('3D Chess game initialized successfully');
            
        } catch (error) {
            console.error('Failed to initialize game:', error);
            this.showError('Failed to initialize 3D Chess game. Please refresh the page.');
        }
    }

    async initialize3DEngine() {
        return new Promise((resolve, reject) => {
            try {
                this.chess3D = new Chess3DEngine('chessCanvas');
                
                // Setup 3D engine callbacks
                this.chess3D.onPieceSelected = (x, y, z, pieceType, color) => {
                    this.handlePieceSelection(x, y, z, pieceType, color);
                };
                
                this.chess3D.onPieceDeselected = () => {
                    this.handlePieceDeselection();
                };
                
                this.chess3D.onMoveAttempt = (fromX, fromY, fromZ, toX, toY, toZ) => {
                    this.handleMoveAttempt(fromX, fromY, fromZ, toX, toY, toZ);
                };
                
                this.chess3D.onSquareSelected = (x, y, z) => {
                    this.handleSquareSelection(x, y, z);
                };
                
                // Apply initial settings
                this.chess3D.updateSettings(this.gameSettings);
                
                // Don't create board yet - wait for server configuration
                // Board will be created when handleGameConfigReceived is called
                console.log('3D engine initialized, waiting for game configuration...');
                
                resolve();
            } catch (error) {
                reject(error);
            }
        });
    }

    setupEventListeners() {
        // Game controls
        document.getElementById('readyBtn')?.addEventListener('click', () => {
            this.toggleReady();
        });

        document.getElementById('resignBtn')?.addEventListener('click', () => {
            this.resign();
        });

        document.getElementById('drawBtn')?.addEventListener('click', () => {
            this.offerDraw();
        });

        // Move controls
        document.getElementById('confirmMoveBtn')?.addEventListener('click', () => {
            this.confirmMove();
        });

        document.getElementById('cancelMoveBtn')?.addEventListener('click', () => {
            this.cancelMove();
        });

        // Camera controls
        document.getElementById('topViewBtn')?.addEventListener('click', () => {
            this.chess3D.setCameraView('top');
        });

        document.getElementById('sideViewBtn')?.addEventListener('click', () => {
            this.chess3D.setCameraView('side');
        });

        document.getElementById('perspectiveBtn')?.addEventListener('click', () => {
            this.chess3D.setCameraView('perspective');
        });

        document.getElementById('resetViewBtn')?.addEventListener('click', () => {
            this.chess3D.setCameraView('reset');
        });

        // Settings controls
        document.getElementById('cameraSpeed')?.addEventListener('input', (e) => {
            this.gameSettings.cameraSpeed = parseFloat(e.target.value);
            this.chess3D.updateSettings({ cameraSpeed: this.gameSettings.cameraSpeed });
        });

        document.getElementById('boardOpacity')?.addEventListener('input', (e) => {
            this.gameSettings.boardOpacity = parseFloat(e.target.value);
            this.chess3D.updateSettings({ boardOpacity: this.gameSettings.boardOpacity });
        });

        // Chat
        document.getElementById('sendBtn')?.addEventListener('click', () => {
            this.sendChatMessage();
        });

        document.getElementById('chatInput')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendChatMessage();
            }
        });

        // Settings modal
        document.getElementById('settingsBtn')?.addEventListener('click', () => {
            this.showModal('settingsModal');
        });

        document.getElementById('closeSettings')?.addEventListener('click', () => {
            this.hideModal('settingsModal');
        });

        // Settings checkboxes and ranges
        this.setupSettingsControls();

        // Window resize
        window.addEventListener('resize', () => {
            if (this.chess3D) {
                this.chess3D.onWindowResize();
            }
        });
    }

    setupSettingsControls() {
        const settingsMap = {
            'enableShadows': 'enableShadows',
            'enableAntialiasing': 'enableAntialiasing', 
            'enableAnimations': 'enableAnimations',
            'enableSounds': 'enableSounds',
            'showValidMoves': 'showValidMoves',
            'showThreats': 'showThreats',
            'confirmMoves': 'confirmMoves'
        };

        Object.entries(settingsMap).forEach(([elementId, settingKey]) => {
            const element = document.getElementById(elementId);
            if (element) {
                element.checked = this.gameSettings[settingKey];
                element.addEventListener('change', (e) => {
                    this.gameSettings[settingKey] = e.target.checked;
                    if (this.chess3D && ['enableShadows', 'enableAntialiasing', 'enableAnimations'].includes(settingKey)) {
                        this.chess3D.updateSettings({ [settingKey]: e.target.checked });
                    }
                });
            }
        });

        // Sound volume
        const soundVolume = document.getElementById('soundVolume');
        if (soundVolume) {
            soundVolume.value = this.gameSettings.soundVolume;
            soundVolume.addEventListener('input', (e) => {
                this.gameSettings.soundVolume = parseFloat(e.target.value);
            });
        }
    }

    setupMultiplayerCallbacks() {
        this.multiplayer.onGameStart = (data) => {
            this.handleGameStart(data);
        };

        this.multiplayer.onGameEnd = (data) => {
            this.handleGameEnd(data);
        };

        this.multiplayer.onMoveReceived = (data) => {
            this.handleMoveReceived(data);
        };

        this.multiplayer.onPlayerJoined = (data) => {
            this.handlePlayerJoined(data);
        };

        this.multiplayer.onPlayerLeft = (data) => {
            this.handlePlayerLeft(data);
        };

        this.multiplayer.onGameStateUpdate = (data) => {
            this.handleGameStateUpdate(data);
        };

        this.multiplayer.onChatMessage = (data) => {
            this.handleChatMessage(data);
        };

        this.multiplayer.onError = (error) => {
            this.handleError(error);
        };

        this.multiplayer.onGameConfigReceived = (gameConfig) => {
            this.handleGameConfigReceived(gameConfig);
        };

        this.multiplayer.onPiecePlaced = (data) => {
            this.handlePiecePlaced(data);
        };

        this.multiplayer.onPlacementPhaseStarted = (data) => {
            this.handlePlacementPhaseStarted(data);
        };
    }
    
    setupPlacementHandlers() {
        // Piece type selection
        const pieceTypes = document.querySelectorAll('.piece-type');
        pieceTypes.forEach(pieceType => {
            pieceType.addEventListener('click', () => {
                if (pieceType.classList.contains('disabled')) return;
                
                // Remove selection from other pieces
                pieceTypes.forEach(pt => pt.classList.remove('selected'));
                
                // Select this piece type
                pieceType.classList.add('selected');
                this.selectedPieceType = pieceType.getAttribute('data-piece');
                
                console.log('Selected piece type:', this.selectedPieceType);
            });
        });
        
        // Finish placement button
        const finishBtn = document.getElementById('finishPlacementBtn');
        if (finishBtn) {
            finishBtn.addEventListener('click', () => {
                this.finishPlacement();
            });
        }
        
        // Clear placement button
        const clearBtn = document.getElementById('clearPlacementBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                this.clearPlacement();
            });
        }
    }

    setupUI() {
        // Initialize move history
        this.updateMoveHistory([]);
        
        // Initialize captured pieces
        this.updateCapturedPieces({ white: [], black: [] });
        
        // Update game status - will be updated once we know the actual game state
        this.updateGameStatus('Connecting...');
    }

    handleURLRoomCode() {
        const urlParams = new URLSearchParams(window.location.search);
        const roomCode = urlParams.get('room');
        
        if (roomCode) {
            const playerName = this.getPlayerName();
            if (playerName) {
                this.multiplayer.joinRoom(roomCode, playerName);
            }
        }
    }

    getPlayerName() {
        // Try to get player name from multiple sources
        
        // 1. First check URL parameters (highest priority)
        const urlParams = new URLSearchParams(window.location.search);
        const urlPlayerName = urlParams.get('playerName');
        if (urlPlayerName && urlPlayerName.trim().length >= 2) {
            return urlPlayerName.trim();
        }
        
        // 2. Check localStorage (medium priority)
        const savedPlayerName = localStorage.getItem('chess3d_playerName');
        if (savedPlayerName && savedPlayerName.trim().length >= 2) {
            return savedPlayerName.trim();
        }
        
        // 3. Prompt user (lowest priority)
        const promptedName = prompt('Enter your name to join the game:');
        if (promptedName && promptedName.trim().length >= 2) {
            const trimmedName = promptedName.trim();
            // Save to localStorage for future use
            localStorage.setItem('chess3d_playerName', trimmedName);
            return trimmedName;
        }
        
        return null;
    }

    // Game Logic Methods
    handlePieceSelection(x, y, z, pieceType, color) {
        // Check if it's the player's turn and piece
        if (!this.multiplayer.isMyTurn()) {
            this.showNotification('It\'s not your turn!', 'warning');
            return;
        }

        if (color !== this.multiplayer.getMyColor()) {
            this.showNotification('You can only move your own pieces!', 'warning');
            return;
        }

        this.selectedPiece = { x, y, z, type: pieceType, color };
        
        // Get valid moves for selected piece
        this.validMoves = this.getValidMovesForPiece(x, y, z);
        
        // Highlight valid moves if enabled
        if (this.gameSettings.showValidMoves) {
            this.chess3D.highlightValidMoves(this.validMoves);
        }
        
        // Update move input display
        this.updateMoveInput(`Selected ${color} ${pieceType} at (${x},${y},${z})`);
        
        // Play sound if enabled
        if (this.gameSettings.enableSounds) {
            this.playSound('select');
        }
    }

    handlePieceDeselection() {
        this.selectedPiece = null;
        this.validMoves = [];
        this.chess3D.clearHighlights();
        this.updateMoveInput('Select a piece to move');
    }

    handleMoveAttempt(fromX, fromY, fromZ, toX, toY, toZ) {
        if (!this.selectedPiece) return;
        
        // Check if move is valid
        const isValid = this.validMoves.some(move => 
            move.toX === toX && move.toY === toY && move.toZ === toZ
        );
        
        if (!isValid) {
            this.showNotification('Invalid move!', 'error');
            if (this.gameSettings.enableSounds) {
                this.playSound('invalid');
            }
            return;
        }

        // Check if move confirmation is enabled
        if (this.gameSettings.confirmMoves) {
            this.pendingMove = { fromX, fromY, fromZ, toX, toY, toZ };
            this.updateMoveInput(
                `Move ${this.selectedPiece.type} from (${fromX},${fromY},${fromZ}) to (${toX},${toY},${toZ})?`,
                true
            );
        } else {
            this.executeMove(fromX, fromY, fromZ, toX, toY, toZ);
        }
    }

    handleSquareSelection(x, y, z) {
        // Handle placement phase clicks
        if (this.gamePhase === 'placement') {
            this.handlePlacementClick(x, y, z);
        } else {
            // For playing phase, this could be used for move hints or other features
            console.log(`Empty square selected: (${x},${y},${z})`);
        }
    }

    confirmMove() {
        if (this.pendingMove) {
            const { fromX, fromY, fromZ, toX, toY, toZ } = this.pendingMove;
            this.executeMove(fromX, fromY, fromZ, toX, toY, toZ);
            this.pendingMove = null;
        }
    }

    cancelMove() {
        this.pendingMove = null;
        this.handlePieceDeselection();
    }

    executeMove(fromX, fromY, fromZ, toX, toY, toZ, promotion = null) {
        // Check for pawn promotion
        if (this.selectedPiece.type === 'pawn' && this.isPawnPromotionMove(toY, this.selectedPiece.color)) {
            this.showPromotionModal((promotionPiece) => {
                this.sendMoveToServer(fromX, fromY, fromZ, toX, toY, toZ, promotionPiece);
            });
            return;
        }

        this.sendMoveToServer(fromX, fromY, fromZ, toX, toY, toZ, promotion);
    }

    sendMoveToServer(fromX, fromY, fromZ, toX, toY, toZ, promotion = null) {
        if (this.multiplayer.makeMove(fromX, fromY, fromZ, toX, toY, toZ, promotion)) {
            // Optimistically update local board
            this.chess3D.movePiece(fromX, fromY, fromZ, toX, toY, toZ, true);
            
            // Clear selection
            this.handlePieceDeselection();
            
            // Play move sound
            if (this.gameSettings.enableSounds) {
                this.playSound('move');
            }
        }
    }

    getValidMovesForPiece(x, y, z) {
        const allMoves = this.chessLogic.getAllValidMoves(this.multiplayer.getMyColor());
        return allMoves.filter(move => 
            move.fromX === x && move.fromY === y && move.fromZ === z
        );
    }

    isPawnPromotionMove(toY, color) {
        return (color === 'white' && toY === 7) || (color === 'black' && toY === 0);
    }

    // Multiplayer Event Handlers
    handleGameStart(data) {
        console.log('Game started:', data);
        
        // Update multiplayer game state first
        this.multiplayer.gameState = data.gameState;
        
        // Check if we're starting in placement phase
        if (data.gameState && data.gameState.phase === 'placement') {
            console.log('Game started in placement phase');
            this.gamePhase = 'placement';
            
            // Get my assigned color
            const myColor = this.getMyAssignedColor();
            if (myColor) {
                this.enterPlacementPhase(myColor);
            }
        } else {
            // Normal game start (already in playing phase)
            this.chessLogic.loadGameState(data.gameState);
            this.updateBoardDisplay();
            this.gamePhase = 'playing';
        }
        
        // Force status update after setting game phase
        console.log(`Game start complete: gamePhase=${this.gamePhase}, isInGame=${this.multiplayer.isInGame()}`);
        this.updateGameStatus(this.getGameStatusText());
        
        // Enable game controls
        document.getElementById('resignBtn').disabled = false;
        document.getElementById('drawBtn').disabled = false;
        
        if (this.gameSettings.enableSounds) {
            this.playSound('start');
        }
    }

    handleGameEnd(data) {
        console.log('Game ended:', data);
        this.updateGameStatus(`Game Over: ${data.result}`);
        
        // Disable game controls
        document.getElementById('resignBtn').disabled = true;
        document.getElementById('drawBtn').disabled = true;
        
        // Show game end modal
        this.showGameEndModal(data);
        
        if (this.gameSettings.enableSounds) {
            this.playSound('end');
        }
    }

    handleMoveReceived(data) {
        console.log('Move received:', data);
        
        // Update chess logic
        const result = this.chessLogic.makeMove(
            data.move.fromX, data.move.fromY, data.move.fromZ,
            data.move.toX, data.move.toY, data.move.toZ,
            data.move.promotion
        );
        
        if (result.success) {
            // Update 3D board
            this.chess3D.movePiece(
                data.move.fromX, data.move.fromY, data.move.fromZ,
                data.move.toX, data.move.toY, data.move.toZ,
                true
            );
            
            // Update UI
            this.updateMoveHistory(this.chessLogic.moveHistory);
            this.updateCapturedPieces(this.chessLogic.capturedPieces);
            this.updateGameStatus(this.getGameStatusText());
            
            // Handle special moves
            if (result.capturedPiece) {
                if (this.gameSettings.enableSounds) {
                    this.playSound('capture');
                }
            } else if (this.gameSettings.enableSounds) {
                this.playSound('move');
            }
            
            // Check for check
            if (result.isCheck) {
                this.highlightCheck(this.chessLogic.currentPlayer);
                if (this.gameSettings.enableSounds) {
                    this.playSound('check');
                }
            }
        }
    }

    handlePlayerJoined(data) {
        console.log('Player joined:', data);
        this.addChatMessage('system', `${data.playerName} joined the game`);
    }

    handlePlayerLeft(data) {
        console.log('Player left:', data);
        this.addChatMessage('system', `${data.playerName} left the game`);
    }

    handleGameStateUpdate(data) {
        console.log('Game state updated:', data);
        
        // Handle phase transitions
        if (data.newPhase) {
            if (data.newPhase === 'pawn_placement') {
                this.updateGameStatus('Automatically placing pawns...');
                // Show notification about automatic pawn placement
                this.showNotification('Phase 1 complete! Automatically placing pawns...', 'success', 3000);
            } else if (data.newPhase === 'playing') {
                this.gamePhase = 'playing';
                this.updateGameStatus('Game started! Pawns have been automatically placed.');
                
                // Hide placement panel
                const placementPanel = document.getElementById('placementPanel');
                if (placementPanel) {
                    placementPanel.style.display = 'none';
                }
                
                // Show notification about game start
                this.showNotification('Game started! All pieces and pawns are now on the board.', 'success', 4000);
            }
        }
        
        // Check if we need to enter placement phase (for reconnections)
        if (data.gameState && data.gameState.phase === 'placement' && this.gamePhase !== 'placement') {
            console.log('Reconnecting to ongoing placement phase');
            
            // Try to get my color from multiplayer data
            const myColor = this.getMyAssignedColor();
            if (myColor) {
                this.enterPlacementPhase(myColor);
                this.showNotification('Reconnected to placement phase. Continue placing your pieces.', 'info', 3000);
            } else {
                console.warn('Could not determine assigned color for placement phase');
            }
        }
        
        this.chessLogic.loadGameState(data.gameState);
        this.updateBoardDisplay();
        this.updateGameStatus(this.getGameStatusText());
    }

    handleChatMessage(data) {
        this.addChatMessage(data.playerName, data.message, data.playerId === this.multiplayer.playerId);
    }

    handleError(error) {
        console.error('Game error:', error);
        this.showNotification(error.message || 'An error occurred', 'error');
    }

    handleGameConfigReceived(gameConfig) {
        console.log('Game config received:', gameConfig);
        
        // Check if we should be in placement phase based on multiplayer game state
        if (this.multiplayer.gameState && this.multiplayer.gameState.phase === 'placement') {
            console.log('Detected placement phase from multiplayer state, setting game phase');
            this.gamePhase = 'placement';
        }
        
        // Apply board size configuration
        if (gameConfig.boardSize) {
            this.setBoardSize(gameConfig.boardSize);
            // Initialize board display after creating the board
            this.updateBoardDisplay();
        }
        
        // Apply player count configuration
        if (gameConfig.playerCount) {
            this.setPlayerCount(gameConfig.playerCount);
        }
        
        // If we're joining a game already in placement phase, enter placement phase
        if (this.gamePhase === 'placement') {
            // Get my assigned color
            const myColor = this.getMyAssignedColor();
            if (myColor) {
                console.log(`Joining game in placement phase as ${myColor}`);
                this.enterPlacementPhase(myColor);
                
                // Force status update after entering placement phase
                setTimeout(() => {
                    this.updateGameStatus(this.getGameStatusText());
                }, 100);
            } else {
                console.warn('Could not determine assigned color for placement phase');
            }
        }
        
        // Update status now that we have proper game state
        this.updateGameStatus(this.getGameStatusText());
        
        console.log(`Applied game configuration - Board: ${gameConfig.boardSize}, Players: ${gameConfig.playerCount}, Phase: ${this.gamePhase}`);
    }

    handlePiecePlaced(data) {
        const { pieceType, color, x, y, z, playerName } = data;
        console.log(`Piece placed by ${playerName}:`, { pieceType, color, x, y, z });
        
        // Create the piece in 3D view for other players to see
        if (this.chess3D) {
            this.chess3D.createPiece(pieceType, color, x, y, z);
        }
        
        // Show notification
        this.showNotification(`${playerName} placed a ${color} ${pieceType}`, 'info', 2000);
    }

    handlePlacementPhaseStarted(data) {
        console.log('Placement phase started:', data);
        
        // Update multiplayer game state first
        this.multiplayer.gameState = data.gameState;
        
        // Find my player data to get assigned color
        const myPlayer = data.players.find(p => p.playerId === this.multiplayer.playerId);
        if (!myPlayer) {
            console.error('Could not find my player data in placement phase start');
            return;
        }
        
        const myColor = myPlayer.assignedColor;
        console.log(`Starting placement phase as ${myColor}`);
        
        // Set game phase and update status
        this.gamePhase = 'placement';
        
        // Enter placement phase with assigned color
        this.enterPlacementPhase(myColor);
        
        // Force status update
        console.log(`Placement phase started: gamePhase=${this.gamePhase}, isInGame=${this.multiplayer.isInGame()}`);
        this.updateGameStatus(this.getGameStatusText());
        
        // Show notification
        this.showNotification(`Game started! Place your ${myColor} pieces on the board.`, 'success', 4000);
    }

    // UI Update Methods
    updateBoardDisplay() {
        if (!this.chess3D) return;

        // Clear existing pieces
        this.chess3D.clearPieces();
        
        // Check multiple sources for placement phase status
        const isPlacementPhase = this.gamePhase === 'placement' || 
                                (this.multiplayer.gameState && this.multiplayer.gameState.phase === 'placement') ||
                                (this.chessLogic.gamePhase === 'placement');
        
        console.log(`updateBoardDisplay: gamePhase=${this.gamePhase}, multiplayer.phase=${this.multiplayer.gameState?.phase}, logic.phase=${this.chessLogic.gamePhase}, isPlacement=${isPlacementPhase}`);
        
        // Only show pieces on board if not in placement phase
        if (!isPlacementPhase) {
            // Get board dimensions dynamically
            const size = this.chessLogic.boardSize ? 
                BoardPosition.getBoardDimensions(this.chessLogic.boardSize) : 8;
            
            // Add pieces from current board state
            for (let x = 0; x < size; x++) {
                for (let y = 0; y < size; y++) {
                    for (let z = 0; z < size; z++) {
                        const piece = this.chessLogic.getPiece(x, y, z);
                        if (piece) {
                            this.chess3D.createPiece(piece.type, piece.color, x, y, z);
                        }
                    }
                }
            }
            console.log(`Board display updated with pieces for phase: ${this.gamePhase}`);
        } else {
            console.log(`Board display cleared for placement phase`);
        }
    }

    updateMoveHistory(moves) {
        const movesList = document.getElementById('movesList');
        if (!movesList) return;

        if (moves.length === 0) {
            movesList.innerHTML = '<div class="no-moves">No moves yet</div>';
            return;
        }

        movesList.innerHTML = moves.map((move, index) => `
            <div class="move-item">
                <span class="move-number">${index + 1}.</span>
                <span class="move-notation">${move.notation}</span>
            </div>
        `).join('');
        
        // Scroll to bottom
        movesList.scrollTop = movesList.scrollHeight;
    }

    updateCapturedPieces(captured) {
        const whitePieces = document.querySelector('#capturedWhite .pieces-list');
        const blackPieces = document.querySelector('#capturedBlack .pieces-list');
        
        if (whitePieces) {
            whitePieces.innerHTML = captured.white.map(piece => 
                `<div class="captured-piece">${this.getPieceSymbol(piece.type, 'white')}</div>`
            ).join('');
        }
        
        if (blackPieces) {
            blackPieces.innerHTML = captured.black.map(piece => 
                `<div class="captured-piece">${this.getPieceSymbol(piece.type, 'black')}</div>`
            ).join('');
        }
    }

    updateGameStatus(status) {
        const statusElement = document.querySelector('.status-text');
        if (statusElement) {
            statusElement.textContent = status;
        }
    }

    updateMoveInput(text, showActions = false) {
        const moveText = document.querySelector('.move-text');
        const confirmBtn = document.getElementById('confirmMoveBtn');
        
        if (moveText) {
            moveText.textContent = text;
        }
        
        if (confirmBtn) {
            confirmBtn.disabled = !showActions;
        }
    }

    getGameStatusText() {
        const isInGame = this.multiplayer.isInGame();
        const multiplayerPhase = this.multiplayer.gameState?.phase;
        console.log(`getGameStatusText: isInGame=${isInGame}, gamePhase=${this.gamePhase}, multiplayerPhase=${multiplayerPhase}, myColor=${this.myColor}`);
        
        if (!isInGame) {
            return 'Waiting for game to start...';
        }

        // Handle placement phase specifically
        if (this.gamePhase === 'placement' || multiplayerPhase === 'placement') {
            return `Place your ${this.myColor || 'assigned'} pieces on the board`;
        }

        const currentPlayer = this.chessLogic.currentPlayer;
        const isMyTurn = this.multiplayer.isMyTurn();
        
        let status = `${currentPlayer.charAt(0).toUpperCase() + currentPlayer.slice(1)}'s turn`;
        
        if (isMyTurn) {
            status += ' (Your turn)';
        }
        
        if (this.chessLogic.isInCheck(currentPlayer)) {
            status += ' - In Check!';
        }
        
        return status;
    }

    // Modal Methods
    showModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('show');
        }
    }

    hideModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('show');
        }
    }

    showPromotionModal(callback) {
        const modal = document.getElementById('promotionModal');
        if (!modal) return;

        // Setup promotion buttons
        const promotionBtns = modal.querySelectorAll('.promotion-btn');
        promotionBtns.forEach(btn => {
            btn.onclick = () => {
                const piece = btn.dataset.piece;
                callback(piece);
                this.hideModal('promotionModal');
            };
        });

        this.showModal('promotionModal');
    }

    showGameEndModal(data) {
        const modal = document.getElementById('gameEndModal');
        const title = document.getElementById('gameEndTitle');
        const result = document.getElementById('gameResult');
        
        if (modal && title && result) {
            title.textContent = 'Game Over';
            result.innerHTML = `
                <h3>${data.result}</h3>
                <p>Reason: ${data.reason}</p>
            `;
            
            this.showModal('gameEndModal');
        }
    }

    // Chat Methods
    sendChatMessage() {
        const input = document.getElementById('chatInput');
        if (!input) return;

        const message = input.value.trim();
        if (message) {
            this.multiplayer.sendChatMessage(message);
            input.value = '';
        }
    }

    addChatMessage(author, message, isOwn = false) {
        const chatMessages = document.getElementById('chatMessages');
        if (!chatMessages) return;

        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${isOwn ? 'own' : ''} ${author === 'system' ? 'system' : ''}`;
        
        if (author !== 'system') {
            messageDiv.innerHTML = `
                <span class="message-author">${author}:</span>
                <span class="message-text">${message}</span>
            `;
        } else {
            messageDiv.innerHTML = `<span class="message-text">${message}</span>`;
        }

        chatMessages.appendChild(messageDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // Game Actions
    toggleReady() {
        const readyBtn = document.getElementById('readyBtn');
        const isReady = readyBtn.textContent === 'Ready';
        
        this.multiplayer.setReady(!isReady);
        readyBtn.textContent = isReady ? 'Not Ready' : 'Ready';
        readyBtn.className = `control-btn ${isReady ? 'secondary' : 'primary'}`;
    }

    resign() {
        if (confirm('Are you sure you want to resign?')) {
            this.multiplayer.resign();
        }
    }

    offerDraw() {
        if (confirm('Offer a draw to your opponent?')) {
            this.multiplayer.offerDraw();
        }
    }
    
    // Placement Phase Methods
    enterPlacementPhase(color) {
        console.log('Entering placement phase as', color);
        this.gamePhase = 'placement';
        this.myColor = color;
        
        // Initialize board for placement phase
        this.chessLogic.initializeBoard('placement');
        
        // Show placement panel
        const placementPanel = document.getElementById('placementPanel');
        if (placementPanel) {
            placementPanel.style.display = 'block';
        }
        
        // Update game status
        this.updateGameStatus(`Place your ${color} pieces on the board`);
        
        // Update piece counts and hide pawns for Phase 1
        this.updatePieceCountDisplay();
        this.hidePawnsForPhase1();
        
        // Clear the board display
        this.updateBoardDisplay();
    }
    
    setBoardSize(boardSize) {
        console.log(`setBoardSize called with: ${boardSize}`);
        
        // Validate board size
        const validSizes = ['Small4x4x4', 'Medium6x6x6', 'Large8x8x8'];
        if (!validSizes.includes(boardSize)) {
            console.error(`Invalid board size: ${boardSize}, defaulting to Large8x8x8`);
            boardSize = 'Large8x8x8';
        }
        
        this.chessLogic.boardSize = boardSize;
        console.log(`Chess logic board size set to: ${this.chessLogic.boardSize}`);
        
        // Recreate the 3D board with new size
        if (this.chess3D) {
            console.log(`Calling chess3D.createBoard with: ${boardSize}`);
            this.chess3D.createBoard(boardSize);
        } else {
            console.warn('chess3D not initialized when setting board size');
        }
        
        // Reinitialize logic board - preserve placement phase
        const currentPhase = this.gamePhase || 'waiting';
        if (currentPhase === 'placement') {
            // During placement phase, keep board empty
            this.chessLogic.initializeBoard('placement');
        } else {
            // For other phases, use current phase or default to waiting
            this.chessLogic.initializeBoard(currentPhase === 'waiting' ? 'placement' : currentPhase);
        }
        
        // Update display
        this.updateBoardDisplay();
        
        const dimensions = BoardPosition.getBoardDimensions(boardSize);
        console.log(`Board size changed to: ${boardSize} (${dimensions}x${dimensions}x${dimensions}), phase: ${currentPhase}`);
    }
    
    setPlayerCount(playerCount) {
        this.chessLogic.playerCount = playerCount;
        console.log(`Player count set to: ${playerCount}`);
    }
    
    updatePieceCountDisplay() {
        if (!this.myColor) {
            console.warn('updatePieceCountDisplay: myColor not set');
            return;
        }
        
        // Normalize color to lowercase for piece pool lookup
        const normalizedColor = this.myColor.toLowerCase();
        console.log(`updatePieceCountDisplay: myColor=${this.myColor}, normalized=${normalizedColor}`);
        
        const piecePool = this.chessLogic.getPiecePool(normalizedColor);
        
        if (!piecePool) {
            console.error(`updatePieceCountDisplay: No piece pool found for color ${normalizedColor}`);
            return;
        }
        
        console.log(`Piece pool for ${normalizedColor}:`, piecePool);
        
        Object.keys(piecePool).forEach(pieceType => {
            const countElement = document.getElementById(`${pieceType}-count`);
            const pieceElement = document.querySelector(`[data-piece="${pieceType}"]`);
            
            if (countElement) {
                countElement.textContent = piecePool[pieceType];
                console.log(`Updated ${pieceType} count to ${piecePool[pieceType]}`);
            } else {
                console.warn(`Count element not found for piece type: ${pieceType}`);
            }
            
            if (pieceElement) {
                if (piecePool[pieceType] === 0) {
                    pieceElement.classList.add('disabled');
                } else {
                    pieceElement.classList.remove('disabled');
                }
            } else {
                console.warn(`Piece element not found for piece type: ${pieceType}`);
            }
        });
        
        // Check if placement is complete
        const allPlaced = Object.values(piecePool).every(count => count === 0);
        const finishBtn = document.getElementById('finishPlacementBtn');
        if (finishBtn) {
            finishBtn.disabled = !allPlaced;
            console.log(`Finish button disabled: ${finishBtn.disabled} (all placed: ${allPlaced})`);
        }
    }

    hidePawnsForPhase1() {
        // Hide pawn button during Phase 1 placement
        const pawnElement = document.querySelector(`[data-piece="pawn"]`);
        if (pawnElement) {
            pawnElement.style.display = 'none';
        }
        
        // Update instructions to reflect Phase 1
        const instructions = document.querySelector('.placement-instructions p');
        if (instructions) {
            instructions.textContent = 'Phase 1: Place your starting pieces (no pawns). Click a piece type below, then click on the board to place it.';
        }
    }
    
    handlePlacementClick(x, y, z) {
        if (this.gamePhase !== 'placement' || !this.selectedPieceType || !this.myColor) {
            console.warn('handlePlacementClick: Invalid state', {
                gamePhase: this.gamePhase,
                selectedPieceType: this.selectedPieceType,
                myColor: this.myColor
            });
            return false;
        }
        
        // Normalize color for chess logic
        const normalizedColor = this.myColor.toLowerCase();
        console.log(`handlePlacementClick: Placing ${this.selectedPieceType} for ${this.myColor} (normalized: ${normalizedColor}) at (${x},${y},${z})`);
        
        const result = this.chessLogic.placePiece(this.selectedPieceType, normalizedColor, x, y, z);
        
        if (result.valid && result.placed) {
            // Create the piece in 3D
            this.chess3D.createPiece(this.selectedPieceType, this.myColor, x, y, z);
            
            // Update piece count display
            this.updatePieceCountDisplay();
            
            // Send placement to server
            this.multiplayer.sendPlacement(this.selectedPieceType, this.myColor, x, y, z);
            
            console.log(`Placed ${this.selectedPieceType} at (${x}, ${y}, ${z})`);
            return true;
        } else {
            console.warn('Cannot place piece:', result.reason);
            this.showPlacementError(result.reason);
            return false;
        }
    }
    
    showPlacementError(message) {
        // Could be enhanced with better UI feedback
        alert(message);
    }
    
    finishPlacement() {
        if (!this.chessLogic.isPlacementPhaseComplete()) {
            alert('Please place all pieces before finishing placement');
            return;
        }
        
        this.multiplayer.finishPlacement();
        
        // Hide placement panel
        const placementPanel = document.getElementById('placementPanel');
        if (placementPanel) {
            placementPanel.style.display = 'none';
        }
        
        this.updateGameStatus('Waiting for opponent to finish placement...');
    }
    
    clearPlacement() {
        if (confirm('Clear all placed pieces?')) {
            // Clear the logic board
            this.chessLogic.initializeBoard('placement');
            
            // Clear the 3D display
            this.chess3D.clearPieces();
            
            // Update piece counts
            this.updatePieceCountDisplay();
            
            // Deselect piece type
            this.selectedPieceType = null;
            document.querySelectorAll('.piece-type').forEach(pt => pt.classList.remove('selected'));
        }
    }

    // Utility Methods
    showLoading(show) {
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) {
            loadingOverlay.classList.toggle('hidden', !show);
        }
    }

    showError(message) {
        this.showNotification(message, 'error', 5000);
    }

    showNotification(message, type = 'info', duration = 3000) {
        if (this.multiplayer) {
            this.multiplayer.showNotification(message, type, duration);
        }
    }

    getPieceSymbol(type, color) {
        const symbols = {
            white: { king: '♔', queen: '♕', rook: '♖', bishop: '♗', knight: '♘', pawn: '♙' },
            black: { king: '♚', queen: '♛', rook: '♜', bishop: '♝', knight: '♞', pawn: '♟' }
        };
        return symbols[color][type] || '?';
    }

    highlightCheck(color) {
        const kingPos = this.chessLogic.kingPositions[color];
        if (kingPos && this.chess3D) {
            this.chess3D.highlightSquare(kingPos.x, kingPos.y, kingPos.z, 'check');
        }
    }

    playSound(type) {
        // Placeholder for sound implementation
        console.log(`Playing sound: ${type}`);
    }

    getMyAssignedColor() {
        // Try to get color from various sources
        if (this.myColor) return this.myColor;
        
        // Check if we have room data with player assignments
        if (this.multiplayer.gameState && this.multiplayer.gameState.players) {
            const myPlayerId = this.multiplayer.playerId;
            const myPlayer = this.multiplayer.gameState.players.find(p => p.playerId === myPlayerId);
            if (myPlayer && myPlayer.assignedColor) {
                return myPlayer.assignedColor;
            }
        }
        
        // Fallback to multiplayer's color method
        const color = this.multiplayer.getMyColor();
        if (color) return color;
        
        // Last resort: assume host is white, non-host is black
        return this.multiplayer.isHost ? 'White' : 'Black';
    }
}

// Initialize game when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.chessGame = new ChessGame();
});

// Export for debugging
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ChessGame;
}