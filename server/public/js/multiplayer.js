/**
 * Multiplayer Socket.IO Client for 3D Chess
 * Handles real-time game synchronization, room management, and player communication
 */

class MultiplayerClient {
    constructor() {
        this.socket = null;
        this.playerId = null;
        this.playerName = null;
        this.roomCode = null;
        this.gameState = null;
        this.isHost = false;
        this.isConnected = false;
        
        // Game callbacks
        this.onGameStart = null;
        this.onGameEnd = null;
        this.onMoveReceived = null;
        this.onPlayerJoined = null;
        this.onPlayerLeft = null;
        this.onGameStateUpdate = null;
        this.onChatMessage = null;
        this.onError = null;
        this.onGameConfigReceived = null;
        this.onPiecePlaced = null;
        this.onPlacementPhaseStarted = null;
        
        this.initializeSocket();
    }

    initializeSocket() {
        this.socket = io();
        
        // Connection events
        this.socket.on('connect', () => {
            console.log('Connected to server');
            this.isConnected = true;
            this.updateConnectionStatus('connected');
        });

        this.socket.on('disconnect', () => {
            console.log('Disconnected from server');
            this.isConnected = false;
            this.updateConnectionStatus('disconnected');
        });

        this.socket.on('reconnecting', () => {
            console.log('Reconnecting to server...');
            this.updateConnectionStatus('connecting');
        });

        // Room events
        this.socket.on('roomJoined', (data) => {
            console.log('Joined room:', data);
            this.roomCode = data.roomCode;
            this.playerId = data.playerId;
            this.playerName = data.playerName;
            this.isHost = data.isHost;
            this.gameState = data.gameState;
            this.updateRoomInfo(data);
            
            // Handle game configuration
            if (data.gameConfig && this.onGameConfigReceived) {
                this.onGameConfigReceived(data.gameConfig);
            }
            
            // Check if we're joining a room in placement phase
            if (data.gameState && data.gameState.phase === 'placement' && this.onPlacementPhaseStarted) {
                console.log('Room is already in placement phase, triggering placement phase start');
                this.onPlacementPhaseStarted({
                    roomCode: data.roomCode,
                    phase: 'placement',
                    players: [{ // Simplified player data for placement
                        playerId: data.playerId,
                        playerName: data.playerName,
                        assignedColor: data.assignedColor,
                        isHost: data.isHost
                    }],
                    gameState: data.gameState
                });
            }
            
            if (this.onPlayerJoined) {
                this.onPlayerJoined(data);
            }
        });

        this.socket.on('playerJoined', (data) => {
            console.log('Player joined:', data);
            if (this.onPlayerJoined) {
                this.onPlayerJoined(data);
            }
        });

        this.socket.on('playerLeft', (data) => {
            console.log('Player left:', data);
            if (this.onPlayerLeft) {
                this.onPlayerLeft(data);
            }
        });

        this.socket.on('playersUpdate', (data) => {
            console.log('Players updated:', data);
            this.updatePlayersList(data.players);
        });

        // Game events
        this.socket.on('game_started', (data) => {
            console.log('Game started:', data);
            this.gameState = data.gameState;
            if (this.onGameStart) {
                this.onGameStart(data);
            }
        });

        this.socket.on('placement_phase_started', (data) => {
            console.log('Placement phase started:', data);
            this.gameState = data.gameState;
            if (this.onPlacementPhaseStarted) {
                this.onPlacementPhaseStarted(data);
            }
        });

        this.socket.on('gameStateUpdate', (data) => {
            console.log('Game state updated:', data);
            this.gameState = data.gameState;
            if (this.onGameStateUpdate) {
                this.onGameStateUpdate(data);
            }
        });

        this.socket.on('moveExecuted', (data) => {
            console.log('Move executed:', data);
            if (this.onMoveReceived) {
                this.onMoveReceived(data);
            }
        });

        this.socket.on('gameEnded', (data) => {
            console.log('Game ended:', data);
            if (this.onGameEnd) {
                this.onGameEnd(data);
            }
        });

        // Placement events
        this.socket.on('piece_placed', (data) => {
            console.log('Piece placed by other player:', data);
            if (this.onPiecePlaced) {
                this.onPiecePlaced(data);
            }
        });

        this.socket.on('player_placement_complete', (data) => {
            console.log('Player placement complete:', data);
            // Could add callback for individual player completion
        });

        this.socket.on('game_phase_changed', (data) => {
            console.log('Game phase changed:', data);
            this.gameState = data.gameState;
            if (this.onGameStateUpdate) {
                this.onGameStateUpdate(data);
            }
        });

        // Chat events
        this.socket.on('chatMessage', (data) => {
            console.log('Chat message:', data);
            if (this.onChatMessage) {
                this.onChatMessage(data);
            }
        });

        // Error events
        this.socket.on('error', (error) => {
            console.error('Socket error:', error);
            if (this.onError) {
                this.onError(error);
            }
        });

        this.socket.on('roomError', (error) => {
            console.error('Room error:', error);
            if (this.onError) {
                this.onError(error);
            }
        });

        this.socket.on('gameError', (error) => {
            console.error('Game error:', error);
            if (this.onError) {
                this.onError(error);
            }
        });
    }

    // Connection methods
    joinRoom(roomCode, playerName) {
        if (!this.isConnected) {
            console.error('Not connected to server');
            return false;
        }

        this.socket.emit('join_room', {
            roomCode: roomCode,
            playerName: playerName
        });

        return true;
    }

    startGame() {
        if (!this.isConnected || !this.roomCode) {
            console.error('Cannot start game: not connected or not in room');
            return false;
        }

        this.socket.emit('start_game', {
            roomCode: this.roomCode
        });

        return true;
    }

    createRoom(playerName, gameOptions = {}) {
        if (!this.isConnected) {
            console.error('Not connected to server');
            return false;
        }

        this.socket.emit('createRoom', {
            playerName: playerName,
            gameOptions: gameOptions
        });

        return true;
    }

    leaveRoom() {
        if (this.roomCode) {
            this.socket.emit('leaveRoom', {
                roomCode: this.roomCode
            });
            
            this.roomCode = null;
            this.playerId = null;
            this.playerName = null;
            this.isHost = false;
            this.gameState = null;
        }
    }

    // Game methods
    setReady(isReady = true) {
        if (!this.roomCode) {
            console.error('Not in a room');
            return false;
        }

        this.socket.emit('setReady', {
            roomCode: this.roomCode,
            isReady: isReady
        });

        return true;
    }

    makeMove(fromX, fromY, fromZ, toX, toY, toZ, promotion = null) {
        if (!this.roomCode || !this.gameState) {
            console.error('Not in an active game');
            return false;
        }

        this.socket.emit('makeMove', {
            roomCode: this.roomCode,
            move: {
                fromX, fromY, fromZ,
                toX, toY, toZ,
                promotion
            }
        });

        return true;
    }

    offerDraw() {
        if (!this.roomCode || !this.gameState) {
            console.error('Not in an active game');
            return false;
        }

        this.socket.emit('offerDraw', {
            roomCode: this.roomCode
        });

        return true;
    }

    acceptDraw() {
        if (!this.roomCode || !this.gameState) {
            console.error('Not in an active game');
            return false;
        }

        this.socket.emit('acceptDraw', {
            roomCode: this.roomCode
        });

        return true;
    }

    resign() {
        if (!this.roomCode || !this.gameState) {
            console.error('Not in an active game');
            return false;
        }

        this.socket.emit('resign', {
            roomCode: this.roomCode
        });

        return true;
    }

    sendPlacement(pieceType, color, x, y, z) {
        if (!this.roomCode) {
            console.error('Not in a room');
            return false;
        }

        this.socket.emit('place_piece', {
            roomCode: this.roomCode,
            pieceType: pieceType,
            color: color,
            x: x,
            y: y,
            z: z
        });

        return true;
    }

    finishPlacement() {
        if (!this.roomCode) {
            console.error('Not in a room');
            return false;
        }

        this.socket.emit('finish_placement', {
            roomCode: this.roomCode
        });

        return true;
    }

    requestNewGame() {
        if (!this.roomCode) {
            console.error('Not in a room');
            return false;
        }

        this.socket.emit('requestNewGame', {
            roomCode: this.roomCode
        });

        return true;
    }

    // Chat methods
    sendChatMessage(message) {
        if (!this.roomCode) {
            console.error('Not in a room');
            return false;
        }

        if (!message || message.trim().length === 0) {
            return false;
        }

        this.socket.emit('chatMessage', {
            roomCode: this.roomCode,
            message: message.trim()
        });

        return true;
    }

    // Utility methods
    updateConnectionStatus(status) {
        const statusElement = document.getElementById('connectionStatus');
        const statusText = document.getElementById('statusText');
        
        if (statusElement && statusText) {
            statusElement.className = `connection-status ${status}`;
            
            switch (status) {
                case 'connected':
                    statusText.textContent = 'Connected';
                    break;
                case 'disconnected':
                    statusText.textContent = 'Disconnected';
                    break;
                case 'connecting':
                    statusText.textContent = 'Connecting...';
                    break;
                default:
                    statusText.textContent = 'Unknown';
            }
        }
    }

    updateRoomInfo(data) {
        const roomCodeElement = document.getElementById('roomCode');
        if (roomCodeElement) {
            roomCodeElement.textContent = data.roomCode;
        }

        // Update share button functionality
        const shareBtn = document.getElementById('shareBtn');
        if (shareBtn) {
            shareBtn.onclick = () => this.shareRoom();
        }
    }

    updatePlayersList(players) {
        const playerCards = document.getElementById('playerCards');
        if (!playerCards) return;

        playerCards.innerHTML = '';

        Object.values(players).forEach(player => {
            const playerCard = document.createElement('div');
            playerCard.className = `player-card ${player.assignedColor || 'unassigned'}`;
            
            if (this.gameState && this.gameState.currentPlayer === player.assignedColor) {
                playerCard.classList.add('current-turn');
            }

            playerCard.innerHTML = `
                <div class="player-name">${player.playerName}</div>
                <div class="player-status">
                    ${player.assignedColor ? `${player.assignedColor} pieces` : 'Spectator'}
                    ${player.isReady ? ' • Ready' : ' • Not ready'}
                    ${player.isHost ? ' • Host' : ''}
                </div>
            `;

            playerCards.appendChild(playerCard);
        });
    }

    shareRoom() {
        if (!this.roomCode) return;

        const gameUrl = `${window.location.origin}/game.html?room=${this.roomCode}`;
        
        if (navigator.share) {
            navigator.share({
                title: '3D Chess Game',
                text: `Join my 3D Chess game! Room code: ${this.roomCode}`,
                url: gameUrl
            }).catch(console.error);
        } else if (navigator.clipboard) {
            navigator.clipboard.writeText(gameUrl).then(() => {
                this.showNotification('Room link copied to clipboard!');
            }).catch(console.error);
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = gameUrl;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            this.showNotification('Room link copied to clipboard!');
        }
    }

    showNotification(message, type = 'info', duration = 3000) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: rgba(0, 0, 0, 0.9);
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            z-index: 1000;
            backdrop-filter: blur(10px);
            border-left: 4px solid ${type === 'error' ? '#f44336' : type === 'success' ? '#4CAF50' : '#2196F3'};
            animation: slideIn 0.3s ease;
        `;
        notification.textContent = message;

        // Add slide-in animation
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
        `;
        document.head.appendChild(style);

        document.body.appendChild(notification);

        // Remove notification after duration
        setTimeout(() => {
            notification.style.animation = 'slideIn 0.3s ease reverse';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
                if (style.parentNode) {
                    style.parentNode.removeChild(style);
                }
            }, 300);
        }, duration);
    }

    // Getters
    isInGame() {
        return this.gameState && (this.gameState.phase === 'playing' || this.gameState.phase === 'placement');
    }

    isMyTurn() {
        if (!this.gameState || !this.isInGame()) return false;
        
        // Find my assigned color
        const myColor = this.getMyColor();
        return myColor === this.gameState.currentPlayer;
    }

    getMyColor() {
        if (!this.roomCode || !this.playerId) return null;
        
        // This would need to be tracked from room data
        // For now, assume first player is white, second is black
        return this.isHost ? 'white' : 'black';
    }

    getOpponentColor() {
        const myColor = this.getMyColor();
        return myColor === 'white' ? 'black' : 'white';
    }

    // Cleanup
    disconnect() {
        if (this.socket) {
            this.leaveRoom();
            this.socket.disconnect();
            this.socket = null;
        }
        
        this.isConnected = false;
        this.playerId = null;
        this.playerName = null;
        this.roomCode = null;
        this.gameState = null;
        this.isHost = false;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MultiplayerClient;
}