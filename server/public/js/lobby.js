/**
 * 3D Chess Lobby Management
 * Handles room creation, joining, and player management
 */

class ChessLobby {
    constructor() {
        this.socket = null;
        this.isConnected = false;
        this.currentRoom = null;
        this.playerName = '';
        this.playerId = null;
        this.isSearchingPlayers = false;
        this.searchTimer = null;
        this.searchTimeRemaining = 0;
        
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.connectToServer();
        this.loadSavedPlayerName();
        this.refreshRooms();
        
        // Initialize board size options for default player count
        this.updateBoardSizeOptions(document.getElementById('playerCount').value);
        
        // Auto-refresh rooms every 10 seconds
        setInterval(() => this.refreshRooms(), 10000);
    }

    setupEventListeners() {
        // Player name input
        const playerNameInput = document.getElementById('playerName');
        if (!playerNameInput) {
            console.error('❌ playerName input element not found!');
            return;
        }
        
        playerNameInput.addEventListener('input', (e) => {
            this.playerName = e.target.value.trim();
            this.savePlayerName();
            this.updateButtonStates();
        });

        // Game configuration options
        this.setupGameConfigHandlers();

        // Room creation
        const createRoomBtn = document.getElementById('createRoomBtn');
        if (createRoomBtn) {
            createRoomBtn.addEventListener('click', () => {
                this.createRoom();
            });
        } else {
            console.error('❌ createRoomBtn element not found!');
        }

        // Room joining
        const joinRoomBtn = document.getElementById('joinRoomBtn');
        if (joinRoomBtn) {
            joinRoomBtn.addEventListener('click', () => {
                this.joinRoom();
            });
        } else {
            console.error('❌ joinRoomBtn element not found!');
        }

        // Room code input - auto-format and join on Enter
        const roomCodeInput = document.getElementById('roomCode');
        roomCodeInput.addEventListener('input', (e) => {
            e.target.value = e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
            this.updateButtonStates();
        });
        
        roomCodeInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && e.target.value.length === 6) {
                this.joinRoom();
            }
        });

        // Refresh rooms
        document.getElementById('refreshRoomsBtn').addEventListener('click', () => {
            this.refreshRooms();
        });

        // Modal controls
        document.getElementById('closeModal').addEventListener('click', () => {
            this.hideModal();
        });

        document.getElementById('copyRoomCode').addEventListener('click', () => {
            this.copyToClipboard(document.getElementById('displayRoomCode').textContent);
        });

        document.getElementById('copyShareLink').addEventListener('click', () => {
            this.copyToClipboard(document.getElementById('shareLink').value);
        });

        // Modal button event listeners (these elements exist but may not be visible initially)
        const startGameBtn = document.getElementById('startGameBtn');
        const readyBtn = document.getElementById('readyBtn');
        const leaveRoomBtn = document.getElementById('leaveRoomBtn');
        const matchPlayersBtn = document.getElementById('matchPlayersBtn');
        
        if (startGameBtn) {
            startGameBtn.addEventListener('click', () => {
                this.startGame();
            });
        } else {
            console.error('❌ startGameBtn element not found');
        }

        if (readyBtn) {
            readyBtn.addEventListener('click', () => {
                this.toggleReady();
            });
        } else {
            console.error('❌ readyBtn element not found');
        }

        if (leaveRoomBtn) {
            leaveRoomBtn.addEventListener('click', () => {
                this.leaveRoom();
            });
        } else {
            console.error('❌ leaveRoomBtn element not found');
        }

        if (matchPlayersBtn) {
            matchPlayersBtn.addEventListener('click', () => {
                this.matchPlayers();
            });
        } else {
            console.error('❌ matchPlayersBtn element not found');
        }

        // Click outside modal to close
        document.getElementById('roomModal').addEventListener('click', (e) => {
            if (e.target.id === 'roomModal') {
                this.hideModal();
            }
        });
    }

    setupGameConfigHandlers() {
        // Player count and board size correlation
        const playerCount = document.getElementById('playerCount');
        const boardSize = document.getElementById('boardSize');
        
        playerCount.addEventListener('change', (e) => {
            this.updateBoardSizeOptions(e.target.value);
        });
        
        // Chaos mode toggle
        const chaosMode = document.getElementById('chaosMode');
        const chaosSettings = document.getElementById('chaosSettings');
        const chaosTurnInterval = document.getElementById('chaosTurnInterval');
        const chaosIntervalValue = document.getElementById('chaosIntervalValue');
        
        chaosMode.addEventListener('change', (e) => {
            chaosSettings.style.display = e.target.checked ? 'block' : 'none';
        });
        
        chaosTurnInterval.addEventListener('input', (e) => {
            chaosIntervalValue.textContent = e.target.value;
        });
        
        // Timed mode toggle
        const timedMode = document.getElementById('timedMode');
        const timedSettings = document.getElementById('timedSettings');
        const timePerPlayer = document.getElementById('timePerPlayer');
        const timePerPlayerValue = document.getElementById('timePerPlayerValue');
        
        timedMode.addEventListener('change', (e) => {
            timedSettings.style.display = e.target.checked ? 'block' : 'none';
        });
        
        timePerPlayer.addEventListener('input', (e) => {
            timePerPlayerValue.textContent = e.target.value;
        });
        
        // AI options
        const includeAI = document.getElementById('includeAI');
        const aiSettings = document.getElementById('aiSettings');
        
        includeAI.addEventListener('change', (e) => {
            aiSettings.style.display = e.target.checked ? 'block' : 'none';
        });
    }
    
    updateBoardSizeOptions(playerCount) {
        const boardSize = document.getElementById('boardSize');
        const options = boardSize.options;
        const currentValue = boardSize.value;
        let needsNewSelection = false;
        
        // Board size validation rules:
        // 4x4x4: 2 players only
        // 6x6x6: 2 or 4 players
        // 8x8x8: 2, 4, or 6 players
        
        for (let i = 0; i < options.length; i++) {
            const option = options[i];
            let isValid = false;
            
            switch (option.value) {
                case 'Small4x4x4':
                    isValid = playerCount === '2';
                    option.textContent = 'Small (4×4×4) - 2 players only';
                    break;
                case 'Medium6x6x6':
                    isValid = playerCount === '2' || playerCount === '4';
                    option.textContent = 'Medium (6×6×6) - 2 or 4 players';
                    break;
                case 'Large8x8x8':
                    isValid = playerCount === '2' || playerCount === '4' || playerCount === '6';
                    option.textContent = 'Large (8×8×8) - 2, 4, or 6 players';
                    break;
            }
            
            option.disabled = !isValid;
            
            // If current selection becomes invalid, mark for change
            if (option.selected && !isValid) {
                needsNewSelection = true;
            }
        }
        
        // Auto-select a valid option if current selection is invalid
        if (needsNewSelection) {
            if (playerCount === '2') {
                boardSize.value = 'Large8x8x8'; // Default to largest available
            } else if (playerCount === '4') {
                boardSize.value = 'Large8x8x8'; // Default to largest available
            } else if (playerCount === '6') {
                boardSize.value = 'Large8x8x8'; // Only option available
            }
        }
        
        console.log(`Board size options updated for ${playerCount} players. Selected: ${boardSize.value}`);
    }

    connectToServer() {
        this.showLoading('Connecting to server...');
        
        console.log('🔌 Attempting to connect to Socket.IO server...');
        console.log('🔌 Server URL:', window.location.origin);
        console.log('🔌 Socket.IO available:', typeof io !== 'undefined');
        
        if (typeof io === 'undefined') {
            console.error('❌ Socket.IO library not loaded!');
            this.showError('Socket.IO library failed to load. Please refresh the page.');
            return;
        }
        
        this.socket = io({
            query: {
                token: 'WEB_CLIENT',
                playerId: 'web_' + Date.now(),
                playerName: 'WebPlayer'
            },
            timeout: 10000,
            forceNew: true,
            reconnection: true,
            reconnectionDelay: 1000,
            reconnectionAttempts: 5,
            maxReconnectionAttempts: 5
        });
        
        console.log('🔌 Socket.IO instance created:', this.socket);

        this.socket.on('connect', () => {
            console.log('✅ Successfully connected to 3D Chess Server');
            console.log('🔌 Socket ID:', this.socket.id);
            this.isConnected = true;
            this.updateConnectionStatus();
            this.updateButtonStates();
            this.hideLoading();
        });

        this.socket.on('disconnect', (reason) => {
            console.log('❌ Disconnected from server. Reason:', reason);
            this.isConnected = false;
            this.updateConnectionStatus();
            this.updateButtonStates();
            this.currentRoom = null;
        });

        this.socket.on('connect_error', (error) => {
            console.error('❌ Connection error:', error);
            console.error('❌ Error details:', {
                type: error.type,
                description: error.description,
                context: error.context,
                message: error.message
            });
            this.isConnected = false;
            this.updateConnectionStatus();
            this.updateButtonStates();
            this.hideLoading();
            this.showError('Failed to connect to server. Retrying...');
        });

        this.socket.on('reconnect', (attemptNumber) => {
            console.log('Reconnected to server after', attemptNumber, 'attempts');
            this.isConnected = true;
            this.updateConnectionStatus();
            this.updateButtonStates();
            this.hideLoading();
        });

        this.socket.on('reconnect_attempt', (attemptNumber) => {
            console.log('Reconnection attempt:', attemptNumber);
            this.showLoading(`Reconnecting... (attempt ${attemptNumber})`);
        });

        this.socket.on('reconnect_failed', () => {
            console.error('Failed to reconnect after maximum attempts');
            this.isConnected = false;
            this.updateConnectionStatus();
            this.hideLoading();
            this.showError('Unable to connect to server. Please refresh the page.');
        });

        // Room events (now handled via callbacks in createRoom/joinRoom methods)

        this.socket.on('room_join_failed', (data) => {
            console.error('Failed to join room:', data.error);
            this.hideLoading();
            this.showError(`Failed to join room: ${data.error}`);
        });

        this.socket.on('room_updated', (data) => {
            console.log('Room updated:', data);
            if (this.currentRoom === data.roomCode) {
                this.updateRoomModal(data);
            }
        });

        this.socket.on('player_joined', (data) => {
            console.log('Player joined:', data);
            if (this.currentRoom === data.roomCode) {
                this.refreshRoomStatus();
            }
        });

        this.socket.on('player_left', (data) => {
            console.log('Player left:', data);
            if (this.currentRoom === data.roomCode) {
                this.refreshRoomStatus();
            }
        });

        this.socket.on('game_started', (data) => {
            console.log('Game started:', data);
            this.redirectToGame(data.roomCode);
        });

        this.socket.on('player_ready_updated', (data) => {
            console.log('Player ready status updated:', data);
            if (this.currentRoom) {
                this.refreshRoomStatus();
            }
        });
    }

    updateConnectionStatus() {
        const statusElement = document.getElementById('connectionStatus');
        const statusText = document.getElementById('statusText');

        if (this.isConnected) {
            statusElement.className = 'connection-status connected';
            statusText.textContent = '🟢 Connected';
        } else {
            statusElement.className = 'connection-status disconnected';
            statusText.textContent = '🔴 Disconnected';
        }
    }

    updateButtonStates() {
        const createBtn = document.getElementById('createRoomBtn');
        const joinBtn = document.getElementById('joinRoomBtn');
        const roomCode = document.getElementById('roomCode').value;

        const hasValidName = this.playerName.length >= 2;
        
        // Debug logging
        console.log('🔧 Updating button states:', {
            hasValidName,
            isConnected: this.isConnected,
            playerName: this.playerName,
            roomCodeLength: roomCode.length
        });
        
        // Update Create Room button with helpful tooltip
        createBtn.disabled = !hasValidName || !this.isConnected;
        if (!this.isConnected) {
            createBtn.title = 'Please wait for connection to server...';
        } else if (!hasValidName) {
            createBtn.title = 'Please enter your name (at least 2 characters)';
        } else {
            createBtn.title = 'Create a new game room';
        }
        
        // Update Join Room button with helpful tooltip
        joinBtn.disabled = !hasValidName || !this.isConnected || roomCode.length !== 6;
        if (!this.isConnected) {
            joinBtn.title = 'Please wait for connection to server...';
        } else if (!hasValidName) {
            joinBtn.title = 'Please enter your name (at least 2 characters)';
        } else if (roomCode.length !== 6) {
            joinBtn.title = 'Please enter a 6-letter room code';
        } else {
            joinBtn.title = 'Join the specified room';
        }
        
        console.log('🔧 Button states set:', {
            createDisabled: createBtn.disabled,
            joinDisabled: joinBtn.disabled,
            createTitle: createBtn.title,
            joinTitle: joinBtn.title
        });
    }

    createRoom() {
        if (!this.validatePlayerName()) return;

        this.showLoading('Creating room...');
        
        // Collect all game configuration data
        const gameConfig = this.collectGameConfiguration();

        console.log('🎯 Sending create_room with data:', {
            playerName: this.playerName,
            gameConfig: gameConfig
        });

        this.socket.emit('create_room', {
            playerName: this.playerName,
            gameConfig: gameConfig
        }, (response) => {
            this.hideLoading();
            
            if (response.success) {
                console.log('Room created:', response);
                this.currentRoom = response.roomCode;
                this.playerId = this.socket.playerId || response.playerId;
                this.showRoomModal(response.roomCode);
            } else {
                console.error('Failed to create room:', response.error);
                this.showError(response.error || 'Failed to create room');
            }
        });
    }

    joinRoom() {
        if (!this.validatePlayerName()) return;

        const roomCode = document.getElementById('roomCode').value.toUpperCase();
        if (roomCode.length !== 6) {
            this.showError('Please enter a valid 6-letter room code');
            return;
        }

        this.showLoading('Joining room...');

        this.socket.emit('join_room', {
            roomCode: roomCode,
            playerName: this.playerName
        }, (response) => {
            this.hideLoading();
            
            if (response.success) {
                console.log('Room joined:', response);
                this.currentRoom = response.roomCode;
                this.playerId = this.socket.playerId || response.playerId;
                this.showRoomModal(response.roomCode);
            } else {
                console.error('Failed to join room:', response.error);
                this.showError(response.error || 'Failed to join room');
            }
        });
    }
    
    collectGameConfiguration() {
        const config = {
            playerCount: parseInt(document.getElementById('playerCount').value),
            boardSize: document.getElementById('boardSize').value,
            chaosMode: {
                enabled: document.getElementById('chaosMode').checked,
                turnInterval: parseInt(document.getElementById('chaosTurnInterval').value)
            },
            timedMode: {
                enabled: document.getElementById('timedMode').checked,
                timePerPlayer: parseInt(document.getElementById('timePerPlayer').value)
            },
            ai: {
                enabled: document.getElementById('includeAI').checked,
                difficulty: document.getElementById('aiDifficulty').value
            }
        };
        
        console.log('🎯 Web client collected configuration:', JSON.stringify(config, null, 2));
        console.log('🎯 PlayerCount value:', config.playerCount, 'Type:', typeof config.playerCount);
        
        return config;
    }

    validatePlayerName() {
        if (this.playerName.length < 2) {
            this.showError('Please enter a name (at least 2 characters)');
            document.getElementById('playerName').focus();
            return false;
        }
        return true;
    }

    async refreshRooms() {
        try {
            const response = await fetch('/api/rooms');
            const data = await response.json();
            this.displayRooms(data.rooms || []);
        } catch (error) {
            console.error('Error fetching rooms:', error);
        }
    }

    displayRooms(rooms) {
        const roomsList = document.getElementById('roomsList');
        
        if (!rooms || rooms.length === 0) {
            roomsList.innerHTML = '<div class="no-rooms">No active rooms found</div>';
            return;
        }

        const roomsHTML = rooms
            .filter(room => room.isJoinable)
            .map(room => this.createRoomElement(room))
            .join('');

        roomsList.innerHTML = roomsHTML || '<div class="no-rooms">No joinable rooms found</div>';

        // Add click listeners to room items
        document.querySelectorAll('.room-item').forEach(item => {
            item.addEventListener('click', () => {
                const roomCode = item.dataset.roomCode;
                document.getElementById('roomCode').value = roomCode;
                this.updateButtonStates();
            });
        });
    }

    createRoomElement(room) {
        const statusClass = `status-${room.gamePhase}`;
        const statusText = room.gamePhase.charAt(0).toUpperCase() + room.gamePhase.slice(1);
        const openSpots = room.maxPlayers - room.playerCount;
        const hasOpenSpots = openSpots > 0 && room.isJoinable;
        const roomItemClass = hasOpenSpots ? 'room-item joinable' : 'room-item';
        
        let detailsText = `${room.playerCount}/${room.maxPlayers} players`;
        if (hasOpenSpots) {
            detailsText += ` • ${openSpots} open spot${openSpots > 1 ? 's' : ''} available`;
        } else if (room.isJoinable) {
            detailsText += ' • Click to select';
        } else {
            detailsText += ' • Full/In Progress';
        }
        
        return `
            <div class="${roomItemClass}" data-room-code="${room.roomCode}">
                <div class="room-header">
                    <span class="room-code">${room.roomCode}</span>
                    <span class="room-status ${statusClass}">${statusText}</span>
                    ${hasOpenSpots ? '<span class="open-spots-badge">Open</span>' : ''}
                </div>
                <div class="room-details">
                    ${detailsText}
                </div>
            </div>
        `;
    }

    showRoomModal(roomCode) {
        this.currentRoom = roomCode;
        document.getElementById('displayRoomCode').textContent = roomCode;
        document.getElementById('shareLink').value = `${window.location.origin}/lobby.html?join=${roomCode}`;
        document.getElementById('roomModal').classList.add('show');
        this.refreshRoomStatus();
    }

    hideModal() {
        document.getElementById('roomModal').classList.remove('show');
    }

    async refreshRoomStatus() {
        if (!this.currentRoom) return;

        try {
            const response = await fetch(`/api/rooms/${this.currentRoom}`);
            const data = await response.json();
            this.updateRoomModal(data);
        } catch (error) {
            console.error('Error fetching room status:', error);
        }
    }

    updateRoomModal(roomData) {
        const playersList = document.getElementById('playersList');
        const startBtn = document.getElementById('startGameBtn');
        const matchPlayersBtn = document.getElementById('matchPlayersBtn');
        const playerCountDisplay = document.getElementById('playerCountDisplay');
        const openSpotsIndicator = document.getElementById('openSpotsIndicator');
        
        if (!roomData || !roomData.players) {
            playersList.innerHTML = '<div class="no-players">No players found</div>';
            return;
        }

        const currentPlayerCount = roomData.players.length;
        const maxPlayers = roomData.maxPlayers || 2;
        const openSpots = maxPlayers - currentPlayerCount;
        const isHost = roomData.players.some(p => p.playerId === this.playerId && p.isHost);

        // Update player count display
        if (playerCountDisplay) {
            playerCountDisplay.textContent = `${currentPlayerCount}/${maxPlayers}`;
        }

        // Update open spots indicator
        if (openSpotsIndicator) {
            if (openSpots > 0) {
                openSpotsIndicator.textContent = `• ${openSpots} open spot${openSpots > 1 ? 's' : ''} available`;
                openSpotsIndicator.style.display = 'inline';
                openSpotsIndicator.style.color = '#4CAF50';
            } else {
                openSpotsIndicator.textContent = '• Room full';
                openSpotsIndicator.style.display = 'inline';
                openSpotsIndicator.style.color = '#666';
            }
        }

        // Show/hide match players button based on host status and open spots
        if (matchPlayersBtn) {
            if (isHost && openSpots > 0 && !this.isSearchingPlayers) {
                matchPlayersBtn.style.display = 'inline-block';
                matchPlayersBtn.disabled = false;
            } else {
                matchPlayersBtn.style.display = 'none';
            }
        }

        const playersHTML = roomData.players.map(player => {
            const isCurrentPlayer = player.playerId === this.playerId;
            const statusClass = player.isReady ? 'status-ready' : 'status-not-ready';
            const statusText = player.isReady ? '✅ Ready' : '⏳ Not Ready';
            
            return `
                <div class="player-item">
                    <div class="player-name">
                        ${player.name || player.playerName}
                        ${isCurrentPlayer ? ' (You)' : ''}
                        ${player.isHost ? '<span class="status-host">HOST</span>' : ''}
                    </div>
                    <div class="player-status ${statusClass}">
                        ${statusText}
                        <span class="player-color">${player.color || player.assignedColor}</span>
                    </div>
                </div>
            `;
        }).join('');

        playersList.innerHTML = playersHTML;

        // Update start button
        const allReady = roomData.players.every(p => p.isReady);
        const hasMinPlayers = roomData.players.length >= 2;
        
        startBtn.disabled = !allReady || !hasMinPlayers || !isHost;
        startBtn.textContent = isHost ? 'Start Game' : 'Waiting for Host';
    }

    startGame() {
        if (!this.currentRoom) return;

        console.log('🎮 Starting game for room:', this.currentRoom);
        this.socket.emit('start_game', {
            roomCode: this.currentRoom
        }, (response) => {
            if (response && response.success) {
                console.log('✅ Game start successful:', response);
                // The server will emit game_started event to all players
            } else {
                console.error('❌ Game start failed:', response?.error || 'Unknown error');
                this.showError(response?.error || 'Failed to start game');
            }
        });
    }

    toggleReady() {
        if (!this.currentRoom) return;

        // Get current ready state from button
        const readyBtn = document.getElementById('readyBtn');
        const isCurrentlyReady = readyBtn.textContent === 'Not Ready';
        
        this.socket.emit('set_ready', {
            roomCode: this.currentRoom,
            isReady: !isCurrentlyReady
        }, (response) => {
            if (response.success) {
                // Update button state
                readyBtn.textContent = response.isReady ? 'Not Ready' : 'Ready';
                readyBtn.className = response.isReady ? 'primary-btn' : 'secondary-btn';
                
                // Refresh room status to update all players
                this.refreshRoomStatus();
            } else {
                console.error('Failed to set ready state:', response.error);
                this.showError(response.error || 'Failed to set ready state');
            }
        });
    }

    leaveRoom() {
        if (!this.currentRoom) return;

        this.socket.emit('leave_room', {
            roomCode: this.currentRoom
        });

        this.currentRoom = null;
        this.hideModal();
        this.refreshRooms();
    }

    async matchPlayers() {
        if (!this.currentRoom || this.isSearchingPlayers) return;

        console.log('🔍 Starting player search for room:', this.currentRoom);
        
        this.isSearchingPlayers = true;
        this.searchTimeRemaining = 30; // 30 seconds like Unity
        
        // Show search status
        this.showPlayerSearchStatus();
        
        // Start search timer countdown
        this.startSearchTimer();
        
        // Try to find human players first
        try {
            await this.searchForHumanPlayers();
        } catch (error) {
            console.error('Error during player search:', error);
            this.showError('Failed to search for players. Please try again.');
        }
        
        // If we still have open spots after 30 seconds, fall back to AI
        setTimeout(() => {
            if (this.isSearchingPlayers) {
                this.fillRemainingWithAI();
            }
        }, 30000);
    }

    async searchForHumanPlayers() {
        // Get available rooms with compatible settings
        try {
            const response = await fetch('/api/rooms');
            const data = await response.json();
            
            // Look for players in waiting rooms or rooms with open spots
            const compatibleRooms = data.rooms.filter(room => 
                room.roomCode !== this.currentRoom && 
                room.isJoinable && 
                room.activePlayerCount > 0
            );
            
            console.log(`Found ${compatibleRooms.length} rooms with potential players`);
            
            // For now, we'll rely on the existing auto-refresh and visibility
            // In a full implementation, we might invite specific players
            
        } catch (error) {
            console.error('Error searching for human players:', error);
        }
    }

    fillRemainingWithAI() {
        if (!this.isSearchingPlayers) return;
        
        console.log('⏰ Search timeout reached, filling remaining spots with AI');
        
        // Emit request to server to add AI players
        this.socket.emit('fill_ai_players', {
            roomCode: this.currentRoom
        }, (response) => {
            this.hidePlayerSearchStatus();
            
            if (response && response.success) {
                console.log('✅ AI players added successfully');
                this.showNotification('Added AI players to fill remaining spots', 'success');
                
                // Refresh room status to show new AI players
                this.refreshRoomStatus();
            } else {
                console.error('Failed to add AI players:', response?.error);
                this.showError(response?.error || 'Failed to add AI players');
            }
        });
    }

    startSearchTimer() {
        if (this.searchTimer) {
            clearInterval(this.searchTimer);
        }
        
        this.updateSearchTimer();
        
        this.searchTimer = setInterval(() => {
            this.searchTimeRemaining--;
            this.updateSearchTimer();
            
            if (this.searchTimeRemaining <= 0) {
                clearInterval(this.searchTimer);
                this.searchTimer = null;
            }
        }, 1000);
    }

    updateSearchTimer() {
        const timerElement = document.getElementById('searchTimer');
        const progressFill = document.getElementById('progressFill');
        
        if (timerElement) {
            timerElement.textContent = `${this.searchTimeRemaining}s remaining`;
        }
        
        if (progressFill) {
            const progress = ((30 - this.searchTimeRemaining) / 30) * 100;
            progressFill.style.width = `${progress}%`;
        }
    }

    showPlayerSearchStatus() {
        const searchStatus = document.getElementById('playerSearchStatus');
        const matchPlayersBtn = document.getElementById('matchPlayersBtn');
        
        if (searchStatus) {
            searchStatus.style.display = 'block';
        }
        
        if (matchPlayersBtn) {
            matchPlayersBtn.disabled = true;
            matchPlayersBtn.textContent = '🔍 Searching...';
        }
    }

    hidePlayerSearchStatus() {
        this.isSearchingPlayers = false;
        
        const searchStatus = document.getElementById('playerSearchStatus');
        const matchPlayersBtn = document.getElementById('matchPlayersBtn');
        
        if (searchStatus) {
            searchStatus.style.display = 'none';
        }
        
        if (matchPlayersBtn) {
            matchPlayersBtn.disabled = false;
            matchPlayersBtn.textContent = '🔍 Match Players';
        }
        
        if (this.searchTimer) {
            clearInterval(this.searchTimer);
            this.searchTimer = null;
        }
    }

    redirectToGame(roomCode) {
        // Include player name in URL for seamless experience
        const encodedPlayerName = encodeURIComponent(this.playerName);
        window.location.href = `/game.html?room=${roomCode}&playerName=${encodedPlayerName}`;
    }

    showLoading(message) {
        document.getElementById('loadingText').textContent = message;
        document.getElementById('loadingOverlay').classList.add('show');
    }

    hideLoading() {
        document.getElementById('loadingOverlay').classList.remove('show');
    }

    showError(message) {
        alert(message); // Simple error display - could be enhanced with custom modal
    }

    copyToClipboard(text) {
        navigator.clipboard.writeText(text).then(() => {
            // Simple feedback - could be enhanced with toast notification
            const originalText = event.target.textContent;
            event.target.textContent = '✅';
            setTimeout(() => {
                event.target.textContent = originalText;
            }, 1000);
        }).catch(err => {
            console.error('Failed to copy:', err);
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
        });
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

    savePlayerName() {
        localStorage.setItem('chess3d_playerName', this.playerName);
    }

    loadSavedPlayerName() {
        const saved = localStorage.getItem('chess3d_playerName');
        if (saved) {
            this.playerName = saved;
            document.getElementById('playerName').value = saved;
            this.updateButtonStates();
        }
    }

    // Handle URL parameters for direct room joining
    handleURLParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const joinRoom = urlParams.get('join');
        
        if (joinRoom) {
            document.getElementById('roomCode').value = joinRoom.toUpperCase();
            this.updateButtonStates();
            
            // Auto-focus the player name field if empty
            if (!this.playerName) {
                document.getElementById('playerName').focus();
            }
        }
    }
}

// Initialize lobby when page loads
document.addEventListener('DOMContentLoaded', () => {
    const lobby = new ChessLobby();
    lobby.handleURLParams();
    
    // Set initial button states
    lobby.updateButtonStates();
});