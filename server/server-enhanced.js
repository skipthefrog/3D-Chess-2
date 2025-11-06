/**
 * Enhanced 3D Chess Server
 * Socket.IO server that fully supports Unity NetworkManager integration
 * Handles rooms, players, game state, moves, and real-time synchronization
 */

const express = require('express');
const http = require('http');
const socketIo = require('socket.io');
const cors = require('cors');
const helmet = require('helmet');
const winston = require('winston');
const { v4: uuidv4 } = require('uuid');
const fs = require('fs');
const path = require('path');

// Configuration
const PORT = process.env.PORT || 3000;
const NODE_ENV = process.env.NODE_ENV || 'development';

// Express app setup
const app = express();
const server = http.createServer(app);

// Logging setup
const logger = winston.createLogger({
  level: NODE_ENV === 'development' ? 'debug' : 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.colorize(),
    winston.format.printf(({ timestamp, level, message }) => {
      return `${timestamp} [${level}]: ${message}`;
    })
  ),
  transports: [
    new winston.transports.Console()
  ]
});

// Security middleware with relaxed CSP for 3D chess game
app.use(helmet({
  contentSecurityPolicy: {
    directives: {
      defaultSrc: ["'self'"],
      scriptSrc: [
        "'self'", 
        "'unsafe-inline'", 
        "https://cdnjs.cloudflare.com",
        "https://cdn.jsdelivr.net",
        "https://unpkg.com"
      ],
      styleSrc: ["'self'", "'unsafe-inline'", "https://fonts.googleapis.com"],
      fontSrc: ["'self'", "https://fonts.gstatic.com"],
      imgSrc: ["'self'", "data:", "https:"],
      connectSrc: ["'self'", "ws:", "wss:"],
      objectSrc: ["'none'"],
      mediaSrc: ["'self'"],
      frameSrc: ["'none'"]
    }
  }
}));
app.use(cors());
app.use(express.json());

// Serve static files (web dashboard)
app.use(express.static('public'));

// HTTP API endpoints for debugging and monitoring
app.get('/api/rooms', (req, res) => {
  const rooms = Array.from(gameServer.rooms.entries()).map(([roomCode, room]) => {
    const activePlayers = Array.from(room.players.values()).filter(p => !p.isDisconnected);
    const disconnectedPlayers = Array.from(room.players.values()).filter(p => p.isDisconnected);
    
    return {
      roomCode,
      playerCount: room.players.size,
      activePlayerCount: activePlayers.length,
      disconnectedPlayerCount: disconnectedPlayers.length,
      maxPlayers: room.maxPlayers,
      gamePhase: room.gameState.phase,
      currentPlayer: room.gameState.currentPlayer,
      players: Array.from(room.players.values()).map(p => ({
        name: p.playerName,
        color: p.assignedColor,
        isHost: p.isHost,
        isReady: p.isReady,
        isDisconnected: p.isDisconnected || false,
        disconnectedAt: p.disconnectedAt || null,
        gracePeriodRemaining: p.disconnectedAt ? Math.max(0, 120000 - (Date.now() - p.disconnectedAt)) : null
      })),
      createdAt: room.createdAt,
      isJoinable: activePlayers.length < room.maxPlayers && room.gameState.phase === 'waiting'
    };
  });
  
  res.json({
    totalRooms: rooms.length,
    activeRooms: rooms.filter(r => r.activePlayerCount > 0).length,
    roomsWithDisconnectedPlayers: rooms.filter(r => r.disconnectedPlayerCount > 0).length,
    rooms: rooms
  });
});

app.get('/api/rooms/:roomCode', (req, res) => {
  const roomCode = req.params.roomCode.toUpperCase();
  const room = gameServer.rooms.get(roomCode);
  
  if (!room) {
    return res.status(404).json({ error: 'Room not found' });
  }
  
  res.json({
    roomCode,
    playerCount: room.players.size,
    maxPlayers: room.maxPlayers,
    gamePhase: room.gameState.phase,
    currentPlayer: room.gameState.currentPlayer,
    players: Array.from(room.players.values()).map(p => ({
      name: p.playerName,
      color: p.assignedColor,
      isHost: p.isHost,
      isReady: p.isReady
    })),
    gameState: room.gameState,
    createdAt: room.createdAt,
    isJoinable: room.players.size < room.maxPlayers && room.gameState.phase === 'waiting'
  });
});

app.get('/api/server/status', (req, res) => {
  res.json({
    status: 'running',
    uptime: process.uptime(),
    totalRooms: gameServer.rooms.size,
    totalPlayers: gameServer.players.size,
    activeGames: Array.from(gameServer.rooms.values()).filter(r => r.gameState.phase === 'playing').length,
    timestamp: new Date().toISOString()
  });
});

// Simple web dashboard
app.get('/', (req, res) => {
  res.send(`
<!DOCTYPE html>
<html>
<head>
    <title>3D Chess Server Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; }
        .card { background: white; padding: 20px; margin: 20px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .room { border-left: 4px solid #007bff; margin: 10px 0; padding: 10px; background: #f8f9fa; }
        .room.full { border-left-color: #dc3545; }
        .room.playing { border-left-color: #28a745; }
        .status { color: #666; font-size: 0.9em; }
        button { background: #007bff; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; }
        button:hover { background: #0056b3; }
        .refresh { text-align: right; margin: 10px 0; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🎮 3D Chess Server Dashboard</h1>
        
        <div class="card">
            <h2>Server Status</h2>
            <div id="serverStatus">Loading...</div>
            <div class="refresh">
                <button onclick="refreshAll()">🔄 Refresh</button>
            </div>
        </div>
        
        <div class="card">
            <h2>Active Rooms</h2>
            <div id="roomsList">Loading...</div>
        </div>
        
        <div class="card">
            <h2>Testing Tools</h2>
            <p>🤖 Add AI to any room: <code>node add-ai-to-room.js ROOMCODE</code></p>
            <p>🔍 Check room status: <code>node check-room-status.js ROOMCODE</code></p>
            <p>👁️ Monitor activity: <code>node room-monitor.js</code></p>
        </div>
    </div>

    <script>
        async function fetchServerStatus() {
            try {
                const response = await fetch('/api/server/status');
                const data = await response.json();
                document.getElementById('serverStatus').innerHTML = \`
                    <div class="status">
                        ✅ Status: \${data.status}<br>
                        ⏱️ Uptime: \${Math.floor(data.uptime)}s<br>
                        🏠 Total Rooms: \${data.totalRooms}<br>
                        👥 Total Players: \${data.totalPlayers}<br>
                        🎮 Active Games: \${data.activeGames}<br>
                        🕐 Last Updated: \${new Date(data.timestamp).toLocaleTimeString()}
                    </div>
                \`;
            } catch (error) {
                document.getElementById('serverStatus').innerHTML = '❌ Error loading server status';
            }
        }

        async function fetchRooms() {
            try {
                const response = await fetch('/api/rooms');
                const data = await response.json();
                
                if (data.rooms.length === 0) {
                    document.getElementById('roomsList').innerHTML = '<p>No active rooms. Create one in Unity or Node.js!</p>';
                    return;
                }
                
                const roomsHtml = data.rooms.map(room => \`
                    <div class="room \${room.playerCount >= room.maxPlayers ? 'full' : ''} \${room.gamePhase === 'playing' ? 'playing' : ''}">
                        <h3>🎯 Room: \${room.roomCode}</h3>
                        <div class="status">
                            👥 Players: \${room.playerCount}/\${room.maxPlayers}<br>
                            🎮 Phase: \${room.gamePhase}<br>
                            🎨 Current Turn: \${room.currentPlayer}<br>
                            ✅ Joinable: \${room.isJoinable ? 'Yes' : 'No'}<br>
                            🕐 Created: \${new Date(room.createdAt).toLocaleTimeString()}
                        </div>
                        <div style="margin-top: 10px;">
                            \${room.players.map(p => \`
                                <span style="background: \${p.color === 'White' ? '#f8f9fa' : '#343a40'}; 
                                           color: \${p.color === 'White' ? '#000' : '#fff'}; 
                                           padding: 2px 8px; border-radius: 3px; margin-right: 5px;">
                                    \${p.name} (\${p.color}) \${p.isHost ? '👑' : ''} \${p.isReady ? '✅' : '⏳'}
                                </span>
                            \`).join('')}
                        </div>
                    </div>
                \`).join('');
                
                document.getElementById('roomsList').innerHTML = roomsHtml;
            } catch (error) {
                document.getElementById('roomsList').innerHTML = '❌ Error loading rooms';
            }
        }

        function refreshAll() {
            fetchServerStatus();
            fetchRooms();
        }

        // Initial load and auto-refresh
        refreshAll();
        setInterval(refreshAll, 5000); // Refresh every 5 seconds
    </script>
</body>
</html>
  `);
});

// Socket.IO setup with enhanced configuration
const io = socketIo(server, {
  cors: {
    origin: "*",
    methods: ["GET", "POST"],
    credentials: true
  },
  // Relaxed timeout settings to prevent premature disconnects
  pingTimeout: 30000,    // Increased from 10s to 30s
  pingInterval: 15000,   // Increased from 5s to 15s
  connectTimeout: 45000, // Allow longer connection time
  transports: ['websocket', 'polling']
});

// Game state management
class GameServer {
  constructor() {
    this.rooms = new Map();
    this.players = new Map();
    this.gameStates = new Map();
    this.disconnectedPlayers = new Map(); // Track disconnected players with grace period
    this.roomCleanupTimers = new Map(); // Track room cleanup timers
    this.persistenceFile = path.join(__dirname, 'room_state.json');
    this.persistenceInterval = null;
    
    // Load persisted rooms on startup
    this.loadPersistedRooms();
    
    // Start auto-save every 30 seconds
    this.startAutoSave();
    
    logger.info('🎮 3D Chess Game Server initialized with persistence');
  }

  // Persistence methods
  loadPersistedRooms() {
    try {
      if (fs.existsSync(this.persistenceFile)) {
        const data = fs.readFileSync(this.persistenceFile, 'utf8');
        const persistedData = JSON.parse(data);
        
        // Restore rooms (but mark all players as disconnected since server restarted)
        for (const [roomCode, roomData] of Object.entries(persistedData.rooms || {})) {
          const room = {
            ...roomData,
            players: new Map()
          };
          
          // Recreate players map and mark all as disconnected
          for (const [playerId, playerData] of Object.entries(roomData.players || {})) {
            room.players.set(playerId, {
              ...playerData,
              isDisconnected: true,
              disconnectedAt: Date.now(),
              socketId: null
            });
            
            // Schedule cleanup for each player
            this.scheduleRoomCleanup(roomCode, playerId);
          }
          
          this.rooms.set(roomCode, room);
          this.gameStates.set(roomCode, room.gameState);
          
          logger.info(`🔄 Restored room ${roomCode} with ${room.players.size} disconnected players`);
        }
        
        logger.info(`📁 Loaded ${this.rooms.size} persisted rooms from disk`);
      }
    } catch (error) {
      logger.error(`❌ Failed to load persisted rooms: ${error.message}`);
    }
  }

  startAutoSave() {
    // Save room state every 30 seconds
    this.persistenceInterval = setInterval(() => {
      this.saveRoomState();
    }, 30000);
  }

  saveRoomState() {
    try {
      const roomsData = {};
      
      for (const [roomCode, room] of this.rooms.entries()) {
        const playersData = {};
        for (const [playerId, player] of room.players.entries()) {
          playersData[playerId] = {
            ...player,
            socketId: null // Don't persist socket IDs
          };
        }
        
        roomsData[roomCode] = {
          ...room,
          players: playersData
        };
      }
      
      const persistData = {
        rooms: roomsData,
        lastSaved: new Date().toISOString(),
        version: '1.0'
      };
      
      fs.writeFileSync(this.persistenceFile, JSON.stringify(persistData, null, 2));
      logger.debug(`💾 Saved ${Object.keys(roomsData).length} rooms to disk`);
    } catch (error) {
      logger.error(`❌ Failed to save room state: ${error.message}`);
    }
  }

  stopPersistence() {
    if (this.persistenceInterval) {
      clearInterval(this.persistenceInterval);
      this.persistenceInterval = null;
    }
    // Final save on shutdown
    this.saveRoomState();
  }

  // Room management
  createRoom(hostPlayerId, playerName, gameConfig = {}) {
    const roomCode = this.generateRoomCode();
    
    // Enhanced debug logging for configuration merge
    logger.info(`🏠 === CREATE ROOM DEBUG - ${roomCode} ===`);
    logger.info(`🏠 Host: ${playerName} (${hostPlayerId})`);
    logger.info(`🏠 Received gameConfig:`, JSON.stringify(gameConfig, null, 2));
    
    // Apply default game configuration
    const defaultConfig = {
      playerCount: 2,
      boardSize: 'Large8x8x8',
      chaosMode: { enabled: false, turnInterval: 9 },
      timedMode: { enabled: false, timePerPlayer: 10 },
      ai: { enabled: false, difficulty: 'medium' }
    };
    
    logger.info(`🏠 Default config:`, JSON.stringify(defaultConfig, null, 2));
    
    const config = { ...defaultConfig, ...gameConfig };
    
    logger.info(`🏠 Merged config:`, JSON.stringify(config, null, 2));
    logger.info(`🏠 Config playerCount: ${config.playerCount} (type: ${typeof config.playerCount})`);
    logger.info(`🏠 Config boardSize: ${config.boardSize}`);
    
    // Validate board size and player count combination
    if (!this.isValidBoardSizeForPlayerCount(config.boardSize, config.playerCount)) {
      const validSizes = this.getValidBoardSizesForPlayerCount(config.playerCount);
      logger.error(`🏠 ❌ Invalid board size combination: ${config.boardSize} with ${config.playerCount} players`);
      throw new Error(`Invalid board size "${config.boardSize}" for ${config.playerCount} players. Valid sizes: ${validSizes.join(', ')}`);
    }
    
    logger.info(`🏠 ✅ Configuration validated successfully`);
    
    const room = {
      roomCode,
      hostId: hostPlayerId,
      players: new Map(),
      gameState: {
        phase: 'waiting', // waiting, placement, playing, ended
        currentPlayer: 'White',
        boardState: null,
        moveHistory: [],
        totalMoves: 0,
        chaosMode: config.chaosMode,
        timedMode: config.timedMode
      },
      gameConfig: config,
      maxPlayers: config.playerCount,
      createdAt: new Date(),
      autoFillTimer: null,
      lastPlayerJoinTime: new Date()
    };

    logger.info(`🏠 Room object created with maxPlayers: ${room.maxPlayers} (from config.playerCount: ${config.playerCount})`);
    logger.info(`🏠 === END CREATE ROOM DEBUG ===`);

    // Add host as first player
    const hostPlayer = {
      playerId: hostPlayerId,
      playerName,
      assignedColor: 'White',
      isHost: true,
      isReady: false,
      socketId: null
    };

    room.players.set(hostPlayerId, hostPlayer);
    this.rooms.set(roomCode, room);
    this.gameStates.set(roomCode, room.gameState);

    // Start auto-fill timer if needed (for rooms requiring more than current players)
    this.startAutoFillTimer(roomCode);

    logger.info(`🏠 Room created: ${roomCode} by ${playerName} (max: ${room.maxPlayers} players)`);
    return { roomCode, assignedColor: 'White', gameState: room.gameState, gameConfig: room.gameConfig };
  }

  joinRoom(roomCode, playerId, playerName) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      throw new Error('Room not found');
    }

    if (room.players.size >= room.maxPlayers) {
      throw new Error('Room is full');
    }

    if (room.gameState.phase !== 'waiting') {
      throw new Error('Game already in progress');
    }

    // Assign color based on existing players and max player count
    const colors = this.getPlayerColors(room.maxPlayers);
    const usedColors = new Set(Array.from(room.players.values()).map(p => p.assignedColor));
    const availableColors = colors.filter(color => !usedColors.has(color));
    
    if (availableColors.length === 0) {
      throw new Error('No available colors for new player');
    }
    
    const assignedColor = availableColors[0];
    
    const player = {
      playerId,
      playerName,
      assignedColor,
      isHost: false,
      isReady: false,
      socketId: null
    };

    room.players.set(playerId, player);
    room.lastPlayerJoinTime = new Date();
    
    // Update auto-fill timer based on new room status
    if (room.players.size >= room.maxPlayers) {
      // Room is now full, stop auto-fill timer
      this.stopAutoFillTimer(roomCode);
      logger.info(`🏠 Room ${roomCode} is now full (${room.players.size}/${room.maxPlayers})`);
    } else {
      // Restart auto-fill timer with new 30-second countdown
      this.startAutoFillTimer(roomCode);
    }
    
    logger.info(`🚪 Player ${playerName} joined room ${roomCode} as ${assignedColor} (${room.players.size}/${room.maxPlayers})`);
    return { roomCode, assignedColor, gameState: room.gameState, gameConfig: room.gameConfig };
  }

  leaveRoom(roomCode, playerId, isDisconnect = false) {
    const room = this.rooms.get(roomCode);
    if (!room) return false;

    const player = room.players.get(playerId);
    if (!player) return false;

    // If this is a disconnect (not intentional leave), mark for grace period
    if (isDisconnect) {
      player.disconnectedAt = Date.now();
      player.isDisconnected = true;
      logger.info(`📞 Player ${player.playerName} disconnected from room ${roomCode} - starting grace period`);
      
      // Don't remove from room yet, just mark as disconnected
      // Start grace period cleanup timer
      this.scheduleRoomCleanup(roomCode, playerId);
      return true;
    }

    // Intentional leave - remove immediately
    room.players.delete(playerId);
    this.cancelPlayerReconnect(playerId);
    
    // If host leaves, transfer to another player or close room
    if (room.hostId === playerId && room.players.size > 0) {
      const newHost = Array.from(room.players.values())[0];
      newHost.isHost = true;
      room.hostId = newHost.playerId;
      logger.info(`👑 Host transferred to ${newHost.playerName} in room ${roomCode}`);
    }

    // Close room if empty (only for intentional leaves)
    if (room.players.size === 0) {
      this.cleanupRoom(roomCode);
    }

    logger.info(`👋 Player intentionally left room ${roomCode}`);
    return true;
  }

  // Piece placement method for placement phase
  placePiece(roomCode, playerId, pieceType, color, x, y, z) {
    const room = this.rooms.get(roomCode);
    if (!room) throw new Error('Room not found');

    const gameState = room.gameState;
    if (gameState.phase !== 'placement') {
      throw new Error('Not in placement phase');
    }

    const player = room.players.get(playerId);
    if (!player) throw new Error('Player not found');

    // Validate that the player can place this piece type
    if (!player.assignedColor || player.assignedColor.toLowerCase() !== color.toLowerCase()) {
      throw new Error('Cannot place pieces for other colors');
    }

    // Store the placement (simplified - in a full implementation you'd validate placement rules)
    if (!room.placements) {
      room.placements = new Map();
    }
    
    const placementKey = `${x}_${y}_${z}`;
    room.placements.set(placementKey, {
      pieceType,
      color,
      playerId,
      x, y, z,
      timestamp: Date.now()
    });

    logger.info(`🎯 Piece placed: ${color} ${pieceType} at (${x},${y},${z}) by ${player.playerName} in room ${roomCode}`);
    return { placed: true, placement: { pieceType, color, x, y, z } };
  }

  // Schedule room cleanup with grace period
  scheduleRoomCleanup(roomCode, playerId) {
    const gracePeriod = 120000; // 2 minutes
    const timerId = setTimeout(() => {
      this.handleGracePeriodExpired(roomCode, playerId);
    }, gracePeriod);
    
    this.roomCleanupTimers.set(`${roomCode}_${playerId}`, timerId);
    logger.info(`⏰ Grace period started for ${playerId} in room ${roomCode} (2 minutes)`);
  }

  // Handle when grace period expires
  handleGracePeriodExpired(roomCode, playerId) {
    const room = this.rooms.get(roomCode);
    if (!room) return;

    const player = room.players.get(playerId);
    if (player && player.isDisconnected) {
      logger.info(`⏰ Grace period expired for ${player.playerName} in room ${roomCode}`);
      
      // Remove player permanently
      room.players.delete(playerId);
      this.cancelPlayerReconnect(playerId);
      
      // Check if room should be cleaned up
      const activePlayerCount = Array.from(room.players.values()).filter(p => !p.isDisconnected).length;
      if (activePlayerCount === 0) {
        logger.info(`🗑️ Room ${roomCode} empty after grace period - cleaning up`);
        this.cleanupRoom(roomCode);
      }
    }
  }

  // Cancel player reconnect timer
  cancelPlayerReconnect(playerId) {
    for (const [key, timerId] of this.roomCleanupTimers.entries()) {
      if (key.endsWith(`_${playerId}`)) {
        clearTimeout(timerId);
        this.roomCleanupTimers.delete(key);
        logger.debug(`⏹️ Cancelled grace period for player ${playerId}`);
        break;
      }
    }
  }

  // Clean up room completely
  cleanupRoom(roomCode) {
    // Cancel any pending timers for this room
    for (const [key, timerId] of this.roomCleanupTimers.entries()) {
      if (key.startsWith(`${roomCode}_`)) {
        clearTimeout(timerId);
        this.roomCleanupTimers.delete(key);
      }
    }
    
    // Stop auto-fill timer for this room
    this.stopAutoFillTimer(roomCode);
    
    this.rooms.delete(roomCode);
    this.gameStates.delete(roomCode);
    logger.info(`🗑️ Room ${roomCode} completely cleaned up`);
  }

  // Allow player to reconnect to room
  reconnectToRoom(roomCode, playerId, newSocketId) {
    const room = this.rooms.get(roomCode);
    if (!room) return false;

    const player = room.players.get(playerId);
    if (player && player.isDisconnected) {
      // Restore player
      player.isDisconnected = false;
      player.socketId = newSocketId;
      player.disconnectedAt = null;
      
      // Cancel cleanup timer
      this.cancelPlayerReconnect(playerId);
      
      logger.info(`🔄 Player ${player.playerName} reconnected to room ${roomCode}`);
      return true;
    }
    
    return false;
  }

  // Auto-reconnect players who join with matching IDs
  autoReconnectPlayer(roomCode, playerId, newSocketId) {
    const room = this.rooms.get(roomCode);
    if (!room) return false;

    const player = room.players.get(playerId);
    if (player) {
      // Update socket ID regardless of disconnection status
      player.socketId = newSocketId;
      
      if (player.isDisconnected) {
        player.isDisconnected = false;
        player.disconnectedAt = null;
        this.cancelPlayerReconnect(playerId);
        logger.info(`🔄 Auto-reconnected player ${player.playerName} to room ${roomCode}`);
      } else {
        logger.info(`🔗 Updated socket ID for player ${player.playerName} in room ${roomCode}`);
      }
      
      return true;
    }
    
    return false;
  }

  // Game state management
  startGame(roomCode, hostId) {
    const room = this.rooms.get(roomCode);
    if (!room) throw new Error('Room not found');
    if (room.hostId !== hostId) throw new Error('Only host can start game');
    if (room.players.size < 2) throw new Error('Need at least 2 players');

    room.gameState.phase = 'placement';
    room.gameState.currentPlayer = 'White';
    
    // Prepare player data with colors for placement phase
    const playersData = Array.from(room.players.values()).map(player => ({
      playerId: player.playerId,
      playerName: player.playerName,
      assignedColor: player.assignedColor,
      isHost: player.isHost
    }));
    
    logger.info(`🎮 Game started in room ${roomCode}, transitioning to placement phase`);
    return { 
      gameState: room.gameState, 
      phase: 'placement',
      players: playersData,
      roomCode 
    };
  }

  processMove(roomCode, moveData) {
    const room = this.rooms.get(roomCode);
    if (!room) throw new Error('Room not found');

    const gameState = room.gameState;
    if (gameState.phase !== 'playing' && gameState.phase !== 'placement') {
      throw new Error('Game not in progress');
    }

    // Basic move validation
    if (moveData.playerColor !== gameState.currentPlayer) {
      throw new Error('Not your turn');
    }

    // Add move to history
    moveData.moveNumber = gameState.totalMoves + 1;
    moveData.timestamp = Date.now();
    gameState.moveHistory.push(moveData);
    gameState.totalMoves++;

    // Switch turns (only in playing phase)
    if (gameState.phase === 'playing') {
      gameState.currentPlayer = gameState.currentPlayer === 'White' ? 'Black' : 'White';
    }

    logger.info(`🎯 Move processed in room ${roomCode}: ${moveData.fromX},${moveData.fromY},${moveData.fromZ} → ${moveData.toX},${moveData.toY},${moveData.toZ}`);
    return { success: true, gameState };
  }

  finishPlacement(roomCode, playerId) {
    const room = this.rooms.get(roomCode);
    if (!room) throw new Error('Room not found');

    const gameState = room.gameState;
    if (gameState.phase !== 'placement' && gameState.phase !== 'pawn_placement') {
      throw new Error('Not in placement phase');
    }

    const player = room.players.get(playerId);
    if (!player) throw new Error('Player not found');

    if (gameState.phase === 'placement') {
      // Phase 1: Manual piece placement completion
      player.placementComplete = true;
      
      // Check if all players have completed Phase 1
      const allPlayersCompleted = Array.from(room.players.values()).every(p => p.placementComplete);
      
      if (allPlayersCompleted) {
        // Transition to Phase 2: Automatic pawn placement
        gameState.phase = 'pawn_placement';
        
        // Automatically place pawns for all players
        this.autoPlacePawns(room);
        
        // After pawn placement, transition directly to playing phase
        gameState.phase = 'playing';
        gameState.currentPlayer = 'White'; // Reset to White for start of game
        
        // Reset placement flags for potential next game
        Array.from(room.players.values()).forEach(p => p.placementComplete = false);
        
        logger.info(`🚀 All players completed Phase 1 in room ${roomCode}, pawns auto-placed, transitioning to playing phase`);
        return { phaseChanged: true, newPhase: 'playing', gameState };
      } else {
        logger.info(`📋 Player ${player.playerName} completed Phase 1 placement in room ${roomCode}`);
        return { phaseChanged: false, gameState };
      }
    } else if (gameState.phase === 'pawn_placement') {
      // This shouldn't happen with current flow, but handle for completeness
      logger.warn(`⚠️ finishPlacement called during pawn_placement phase in room ${roomCode}`);
      return { phaseChanged: false, gameState };
    }
  }

  // Automatically place pawns in front of placed pieces (Unity parity)
  autoPlacePawns(room) {
    if (!room.placements) return;

    Array.from(room.players.values()).forEach(player => {
      const color = player.assignedColor;
      if (!color) return;

      // Find all pieces placed by this player (excluding pawns)
      const playerPlacements = Array.from(room.placements.values())
        .filter(p => p.color.toLowerCase() === color.toLowerCase() && p.pieceType !== 'pawn');

      // For each placed piece, place a pawn in front of it
      playerPlacements.forEach(placement => {
        const pawnPosition = this.calculatePawnPosition(placement, color);
        if (pawnPosition) {
          const pawnKey = `${pawnPosition.x}_${pawnPosition.y}_${pawnPosition.z}`;
          room.placements.set(pawnKey, {
            pieceType: 'pawn',
            color: color,
            playerId: player.playerId,
            x: pawnPosition.x,
            y: pawnPosition.y,
            z: pawnPosition.z,
            timestamp: Date.now(),
            autoPlaced: true
          });
        }
      });

      logger.info(`♟️ Auto-placed pawns for ${color} pieces in room ${room.roomCode}`);
    });
  }

  // Calculate where to place a pawn based on piece position and color
  calculatePawnPosition(piecePosition, color) {
    // This is a simplified version - in a full implementation you'd need
    // proper 3D chess logic based on placement zones and board geometry
    const { x, y, z } = piecePosition;
    
    // Basic logic: place pawn one step forward from the piece
    // This would need to be adjusted based on your specific 3D chess rules
    switch (color.toLowerCase()) {
      case 'white':
        return { x: x + 1, y, z }; // Move forward on X axis
      case 'black':
        return { x: x - 1, y, z }; // Move backward on X axis
      case 'green':
        return { x, y, z: z + 1 }; // Move forward on Z axis
      case 'purple':
        return { x, y, z: z - 1 }; // Move backward on Z axis
      case 'yellow':
        return { x, y: y + 1, z }; // Move up on Y axis
      case 'orange':
        return { x, y: y - 1, z }; // Move down on Y axis
      default:
        return null;
    }
  }

  // Auto-fill system for underfilled rooms
  startAutoFillTimer(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return;

    // Only start timer if room needs more players
    if (room.players.size >= room.maxPlayers) {
      logger.debug(`🤖 Room ${roomCode} already full, no auto-fill needed`);
      return;
    }

    // Clear any existing timer
    if (room.autoFillTimer) {
      clearTimeout(room.autoFillTimer);
    }

    const autoFillDelay = 30000; // 30 seconds
    logger.info(`⏰ Starting auto-fill timer for room ${roomCode} (${autoFillDelay/1000}s)`);

    room.autoFillTimer = setTimeout(() => {
      this.attemptAutoFill(roomCode);
    }, autoFillDelay);
  }

  stopAutoFillTimer(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room || !room.autoFillTimer) return;

    clearTimeout(room.autoFillTimer);
    room.autoFillTimer = null;
    logger.debug(`⏰ Auto-fill timer stopped for room ${roomCode}`);
  }

  attemptAutoFill(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return;

    const currentPlayers = room.players.size;
    const maxPlayers = room.maxPlayers;
    const neededPlayers = maxPlayers - currentPlayers;

    if (neededPlayers <= 0) {
      logger.debug(`🤖 Room ${roomCode} is full, no auto-fill needed`);
      return;
    }

    logger.info(`🤖 Attempting to auto-fill room ${roomCode}: need ${neededPlayers} more players`);

    // Add AI players to fill the room
    for (let i = 0; i < neededPlayers; i++) {
      this.addAIPlayer(roomCode);
    }

    // Stop the timer since we've attempted auto-fill
    this.stopAutoFillTimer(roomCode);
  }

  addAIPlayer(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return;

    if (room.players.size >= room.maxPlayers) {
      logger.debug(`🤖 Room ${roomCode} is full, cannot add AI player`);
      return;
    }

    // Generate AI player info
    const aiPlayerId = `ai_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const aiPlayerName = `AI Player ${room.players.size + 1}`;
    
    // Assign color based on player count
    const availableColors = ['White', 'Black', 'Green', 'Purple', 'Yellow', 'Orange'];
    const usedColors = Array.from(room.players.values()).map(p => p.assignedColor);
    const assignedColor = availableColors.find(color => !usedColors.includes(color)) || 'Black';

    const aiPlayer = {
      playerId: aiPlayerId,
      playerName: aiPlayerName,
      assignedColor: assignedColor,
      isHost: false,
      isReady: true, // AI players are always ready
      socketId: null,
      isAI: true,
      aiDifficulty: room.gameConfig?.ai?.difficulty || 'medium'
    };

    room.players.set(aiPlayerId, aiPlayer);
    room.lastPlayerJoinTime = new Date();

    logger.info(`🤖 Added AI player "${aiPlayerName}" (${assignedColor}) to room ${roomCode}`);

    // If room is now full, stop auto-fill timer
    if (room.players.size >= room.maxPlayers) {
      this.stopAutoFillTimer(roomCode);
      logger.info(`🤖 Room ${roomCode} is now full with AI players`);
    }

    return aiPlayer;
  }

  updatePlayerReady(roomCode, playerId, isReady) {
    const room = this.rooms.get(roomCode);
    if (!room) return false;

    const player = room.players.get(playerId);
    if (!player) return false;

    player.isReady = isReady;
    
    // Check if all players are ready and can start game
    const allReady = Array.from(room.players.values()).every(p => p.isReady);
    if (allReady && room.players.size >= 2 && room.gameState.phase === 'waiting') {
      // Auto-start the game and transition to placement phase
      room.gameState.phase = 'placement';
      room.gameState.currentPlayer = 'White';
      
      // Prepare player data with colors for placement phase
      const playersData = Array.from(room.players.values()).map(player => ({
        playerId: player.playerId,
        playerName: player.playerName,
        assignedColor: player.assignedColor,
        isHost: player.isHost
      }));
      
      logger.info(`🚀 All players ready in room ${roomCode}, auto-starting game and transitioning to placement phase`);
      
      return { 
        autoStarted: true,
        gameState: room.gameState, 
        phase: 'placement',
        players: playersData,
        roomCode 
      };
    }

    return { autoStarted: false };
  }

  // Utility methods
  generateRoomCode() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    let code;
    do {
      code = '';
      for (let i = 0; i < 6; i++) {
        code += chars[Math.floor(Math.random() * chars.length)];
      }
    } while (this.rooms.has(code));
    return code;
  }

  getPlayerColors(maxPlayers) {
    switch (maxPlayers) {
      case 2:
        return ['White', 'Black'];
      case 4:
        return ['White', 'Black', 'Green', 'Purple'];
      case 6:
        return ['White', 'Black', 'Green', 'Purple', 'Yellow', 'Orange'];
      default:
        return ['White', 'Black'];
    }
  }

  /**
   * Validate that board size is compatible with player count
   * @param {string} boardSize - Board size (Small4x4x4, Medium6x6x6, Large8x8x8)
   * @param {number} playerCount - Number of players (2, 4, 6)
   * @returns {boolean} True if valid combination
   */
  isValidBoardSizeForPlayerCount(boardSize, playerCount) {
    switch (boardSize) {
      case 'Small4x4x4':
        return playerCount === 2; // 4x4x4 only supports 2 players
      case 'Medium6x6x6':
        return playerCount === 2 || playerCount === 4; // 6x6x6 supports 2 or 4 players
      case 'Large8x8x8':
        return playerCount === 2 || playerCount === 4 || playerCount === 6; // 8x8x8 supports 2, 4, or 6 players
      default:
        return false;
    }
  }

  /**
   * Get valid board sizes for a given player count
   * @param {number} playerCount - Number of players
   * @returns {Array} Array of valid board size strings
   */
  getValidBoardSizesForPlayerCount(playerCount) {
    switch (playerCount) {
      case 2:
        return ['Small4x4x4', 'Medium6x6x6', 'Large8x8x8'];
      case 4:
        return ['Medium6x6x6', 'Large8x8x8'];
      case 6:
        return ['Large8x8x8'];
      default:
        return [];
    }
  }

  /**
   * Get valid player counts for a given board size
   * @param {string} boardSize - Board size
   * @returns {Array} Array of valid player counts
   */
  getValidPlayerCountsForBoardSize(boardSize) {
    switch (boardSize) {
      case 'Small4x4x4':
        return [2];
      case 'Medium6x6x6':
        return [2, 4];
      case 'Large8x8x8':
        return [2, 4, 6];
      default:
        return [];
    }
  }

  getRoomInfo(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return null;

    return {
      roomCode: room.roomCode,
      playerCount: room.players.size,
      maxPlayers: room.maxPlayers,
      gamePhase: room.gameState.phase,
      players: Array.from(room.players.values()),
      isJoinable: room.players.size < room.maxPlayers && room.gameState.phase === 'waiting'
    };
  }

  getServerStats() {
    const players = Array.from(this.players.values());
    const unityPlayers = players.filter(p => p.clientType === 'Unity').length;
    const nodejsPlayers = players.filter(p => p.clientType === 'NodeJS').length;
    const unknownPlayers = players.filter(p => !p.clientType || p.clientType === 'Unknown').length;
    
    return {
      totalRooms: this.rooms.size,
      totalPlayers: this.players.size,
      unityPlayers,
      nodejsPlayers,
      unknownPlayers,
      activeGames: Array.from(this.rooms.values()).filter(r => r.gameState.phase === 'playing').length,
      uptime: process.uptime(),
      roomsWithUnityPlayers: Array.from(this.rooms.values()).filter(r => 
        Array.from(r.players.values()).some(p => {
          const player = this.players.get(p.playerId);
          return player?.clientType === 'Unity';
        })
      ).length
    };
  }
}

// Initialize game server
const gameServer = new GameServer();

// Enhanced authentication middleware with Unity detection
io.use((socket, next) => {
  const handshake = socket.handshake;
  const query = handshake.query || {};
  const headers = handshake.headers || {};
  
  // Log ALL connection attempts for debugging
  logger.info(`🔍 === CONNECTION ATTEMPT ANALYSIS ===`);
  logger.info(`🔍 Socket ID: ${socket.id}`);
  logger.info(`🔍 IP Address: ${handshake.address}`);
  logger.info(`🔍 User Agent: ${headers['user-agent'] || 'Not provided'}`);
  logger.info(`🔍 Query Parameters:`, JSON.stringify(query, null, 2));
  logger.info(`🔍 Headers:`, JSON.stringify(headers, null, 2));
  logger.info(`🔍 Transport: ${socket.conn.transport.name}`);
  logger.info(`🔍 ==========================================`);

  const token = query.token;
  const providedPlayerId = query.playerId;
  const providedPlayerName = query.playerName;
  
  // Check for Unity-specific patterns
  const isUnityClient = headers['user-agent']?.includes('Unity') || 
                       query.client === 'Unity' || 
                       query.platform === 'Unity' ||
                       providedPlayerName?.includes('Unity');
  
  if (isUnityClient) {
    logger.info(`🎮 UNITY CLIENT DETECTED!`);
    logger.info(`🎮 Unity Token: ${token}`);
    logger.info(`🎮 Unity Player ID: ${providedPlayerId}`);
    logger.info(`🎮 Unity Player Name: ${providedPlayerName}`);
  }
  
  // Accept multiple token formats for cross-platform compatibility
  const validTokens = ['UNITY', 'Unity', 'unity', 'UNITYPLAYER', 'WEB_CLIENT', 'MOBILE_CLIENT', 'DASHBOARD', undefined, null, ''];
  const isValidToken = validTokens.includes(token) || NODE_ENV === 'development';
  
  if (isValidToken) {
    socket.playerId = providedPlayerId || uuidv4();
    socket.playerName = providedPlayerName || `Player_${socket.id.substring(0, 6)}`;
    
    // Determine client type based on token and user agent
    if (isUnityClient) {
      socket.clientType = 'Unity';
    } else if (token === 'WEB_CLIENT') {
      socket.clientType = 'Web';
    } else if (token === 'MOBILE_CLIENT') {
      socket.clientType = 'Mobile';
    } else if (token === 'DASHBOARD') {
      socket.clientType = 'Dashboard';
    } else {
      socket.clientType = 'NodeJS'; // Default for backward compatibility
    }
    
    logger.info(`✅ Authentication successful for ${socket.playerName} (${socket.playerId}) - Client: ${socket.clientType}`);
    next();
  } else {
    logger.warn(`🚫 Authentication failed for socket ${socket.id} - Invalid token: ${token}`);
    logger.warn(`🚫 Expected tokens: ${validTokens.join(', ')}`);
    next(new Error('Authentication error'));
  }
});

// Socket.IO connection handling
io.on('connection', (socket) => {
  logger.info(`🔌 Player connected: ${socket.playerName} (${socket.id}) from ${socket.handshake.address} - Client: ${socket.clientType}`);
  
  // Enhanced Unity client tracking
  if (socket.clientType === 'Unity') {
    logger.info(`🎮 UNITY CONNECTION ESTABLISHED!`);
    logger.info(`🎮 Unity Player: ${socket.playerName}`);
    logger.info(`🎮 Unity Player ID: ${socket.playerId}`);
    logger.info(`🎮 Unity Socket ID: ${socket.id}`);
  }
  
  // Store player information with client type
  gameServer.players.set(socket.playerId, {
    playerId: socket.playerId,
    playerName: socket.playerName,
    socketId: socket.id,
    clientType: socket.clientType || 'Unknown',
    currentRoom: null,
    connectedAt: new Date()
  });

  // Check for reconnection attempt
  const existingPlayer = gameServer.players.get(socket.playerId);
  if (existingPlayer && existingPlayer.lastDisconnect) {
    logger.info(`🔄 Reconnection attempt from ${socket.playerName} (previous disconnect: ${existingPlayer.lastDisconnect.reason})`);
    
    // Try to reconnect to previous room
    const previousRoom = existingPlayer.lastDisconnect.room;
    if (previousRoom && gameServer.reconnectToRoom(previousRoom, socket.playerId, socket.id)) {
      existingPlayer.socketId = socket.id;
      existingPlayer.lastDisconnect = null;
      
      socket.join(previousRoom);
      
      // Notify reconnection success
      socket.emit('reconnection_success', {
        roomCode: previousRoom,
        message: 'Successfully reconnected to your previous room',
        gracePeriodUsed: true
      });
      
      // Notify others in room
      socket.to(previousRoom).emit('player_reconnected', {
        playerId: socket.playerId,
        playerName: socket.playerName
      });
      
      logger.info(`✅ ${socket.playerName} successfully reconnected to room ${previousRoom}`);
    } else {
      logger.info(`❌ Failed to reconnect ${socket.playerName} to room ${previousRoom} - room may have been cleaned up`);
      existingPlayer.lastDisconnect = null;
    }
  }

  // Send welcome message
  setTimeout(() => {
    socket.emit('connection', {
      date: Date.now(),
      data: '3D Chess Server Connected',
      message: 'Socket.IO is working!',
      playerId: socket.playerId,
      playerName: socket.playerName,
      reconnected: !!existingPlayer?.lastDisconnect
    });
  }, 500);

  // Room management handlers
  socket.on('create_room', (data, callback) => {
    try {
      const { playerName, gameConfig } = data;
      
      // Update socket player name if provided
      if (playerName) {
        socket.playerName = playerName;
      }
      
      // Enhanced logging for debugging configuration issues
      logger.info(`🎮 === CREATE ROOM DEBUG ===`);
      logger.info(`🎮 Player: "${socket.playerName}"`);
      logger.info(`🎮 Raw data received:`, JSON.stringify(data, null, 2));
      logger.info(`🎮 GameConfig object:`, JSON.stringify(gameConfig, null, 2));
      logger.info(`🎮 GameConfig playerCount:`, gameConfig?.playerCount);
      logger.info(`🎮 GameConfig playerCount type:`, typeof gameConfig?.playerCount);
      logger.info(`🎮 ========================`);
      
      const result = gameServer.createRoom(socket.playerId, socket.playerName, gameConfig);
      const player = gameServer.players.get(socket.playerId);
      player.currentRoom = result.roomCode;
      
      socket.join(result.roomCode);
      
      if (callback) callback({ success: true, ...result });
      
      // Also emit roomJoined event for client compatibility
      socket.emit('roomJoined', {
        roomCode: result.roomCode,
        playerId: socket.playerId,
        playerName: socket.playerName,
        assignedColor: result.assignedColor,
        isHost: true,
        gameState: result.gameState,
        gameConfig: result.gameConfig
      });
      
      logger.info(`✅ Room created successfully: ${result.roomCode} with ${gameConfig?.playerCount || 2} players`);
    } catch (error) {
      logger.error(`❌ Create room error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  socket.on('join_room', (data, callback) => {
    try {
      const { roomCode } = data;
      
      // Enhanced logging for Unity join attempts
      logger.info(`🚪 === JOIN ROOM ATTEMPT ===`);
      logger.info(`🚪 Player: ${socket.playerName} (${socket.clientType})`);
      logger.info(`🚪 Room Code: ${roomCode}`);
      logger.info(`🚪 Player ID: ${socket.playerId}`);
      logger.info(`🚪 Socket ID: ${socket.id}`);
      logger.info(`🚪 Data received:`, JSON.stringify(data, null, 2));
      
      if (socket.clientType === 'Unity') {
        logger.info(`🎮 UNITY ROOM JOIN ATTEMPT!`);
        logger.info(`🎮 Unity trying to join room: ${roomCode}`);
      }
      
      // Check if this player is already in the room (reconnection scenario)
      let result;
      const existingPlayerReconnected = gameServer.autoReconnectPlayer(roomCode, socket.playerId, socket.id);
      
      if (existingPlayerReconnected) {
        // Player reconnected to existing room
        const room = gameServer.rooms.get(roomCode);
        const player = room.players.get(socket.playerId);
        
        result = {
          roomCode: roomCode,
          assignedColor: player.assignedColor,
          gameState: room.gameState,
          gameConfig: room.gameConfig
        };
        
        logger.info(`🔄 Player reconnected to existing room: ${roomCode}`);
      } else {
        // New player joining room
        result = gameServer.joinRoom(roomCode, socket.playerId, socket.playerName);
      }
      
      const player = gameServer.players.get(socket.playerId);
      player.currentRoom = roomCode;
      
      socket.join(roomCode);
      
      // Notify other players in room (only if this is a new join, not reconnection)
      if (!existingPlayerReconnected) {
        socket.to(roomCode).emit('player_joined', {
          playerId: socket.playerId,
          playerName: socket.playerName,
          assignedColor: result.assignedColor,
          clientType: socket.clientType
        });
      }
      
      if (callback) callback({ success: true, ...result });
      
      // Also emit roomJoined event for client compatibility
      socket.emit('roomJoined', {
        roomCode: result.roomCode,
        playerId: socket.playerId,
        playerName: socket.playerName,
        assignedColor: result.assignedColor,
        isHost: existingPlayerReconnected ? player.isHost : false,
        gameState: result.gameState,
        gameConfig: result.gameConfig
      });
      
      logger.info(`✅ Player joined room successfully: ${roomCode} - Client: ${socket.clientType}`);
      
      if (socket.clientType === 'Unity') {
        logger.info(`🎮 UNITY SUCCESSFULLY JOINED ROOM!`);
        logger.info(`🎮 Unity assigned color: ${result.assignedColor}`);
        logger.info(`🎮 Room state:`, result.gameState);
      }
      
    } catch (error) {
      logger.error(`❌ Join room error for ${socket.playerName} (${socket.clientType}): ${error.message}`);
      
      if (socket.clientType === 'Unity') {
        logger.error(`🎮 UNITY JOIN FAILED: ${error.message}`);
      }
      
      if (callback) callback({ success: false, error: error.message });
    }
  });

  socket.on('leave_room', (data, callback) => {
    const player = gameServer.players.get(socket.playerId);
    if (player && player.currentRoom) {
      const roomCode = player.currentRoom;
      
      socket.leave(roomCode);
      gameServer.leaveRoom(roomCode, socket.playerId);
      player.currentRoom = null;
      
      // Notify other players
      socket.to(roomCode).emit('player_left', {
        playerId: socket.playerId,
        playerName: socket.playerName
      });
      
      if (callback) callback({ success: true });
      logger.info(`👋 Player left room: ${roomCode}`);
    }
  });

  // Game management handlers
  socket.on('start_game', (data, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const gameResult = gameServer.startGame(player.currentRoom, socket.playerId);
      
      // Notify all players in room about game start and placement phase
      io.to(player.currentRoom).emit('game_started', { 
        roomCode: player.currentRoom,
        ...gameResult
      });
      
      // Also emit specific placement phase start event
      io.to(player.currentRoom).emit('placement_phase_started', {
        roomCode: player.currentRoom,
        phase: 'placement',
        players: gameResult.players,
        gameState: gameResult.gameState
      });
      
      if (callback) callback({ success: true, roomCode: player.currentRoom, ...gameResult });
      
      logger.info(`🎮 Game started in room: ${player.currentRoom}`);
    } catch (error) {
      logger.error(`❌ Start game error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  // Placement phase handlers
  socket.on('place_piece', (data, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const { pieceType, color, x, y, z } = data;
      const result = gameServer.placePiece(player.currentRoom, socket.playerId, pieceType, color, x, y, z);
      
      // Notify other players about the piece placement
      socket.to(player.currentRoom).emit('piece_placed', {
        playerId: socket.playerId,
        playerName: socket.playerName,
        pieceType,
        color,
        x, y, z
      });
      
      if (callback) callback({ success: true, ...result });
      
      logger.info(`🎯 Piece placed in room ${player.currentRoom}: ${color} ${pieceType} at (${x},${y},${z})`);
    } catch (error) {
      logger.error(`❌ Place piece error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  socket.on('finish_placement', (data, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const result = gameServer.finishPlacement(player.currentRoom, socket.playerId);
      
      // Notify other players about placement completion
      socket.to(player.currentRoom).emit('player_placement_complete', {
        playerId: socket.playerId,
        playerName: socket.playerName
      });
      
      // Handle phase transitions
      if (result.phaseChanged) {
        if (result.newPhase === 'pawn_placement') {
          // Phase 1 complete, moving to Phase 2 (automatic pawn placement)
          io.to(player.currentRoom).emit('game_phase_changed', {
            newPhase: 'pawn_placement',
            gameState: result.gameState
          });
          logger.info(`📍 Room ${player.currentRoom} transitioned to pawn placement phase`);
        } else if (result.newPhase === 'playing') {
          // Phase 2 complete, game starts
          io.to(player.currentRoom).emit('game_phase_changed', {
            newPhase: 'playing',
            gameState: result.gameState
          });
          logger.info(`🚀 Room ${player.currentRoom} transitioned to playing phase`);
        }
      }
      
      if (callback) callback({ success: true, ...result });
      
    } catch (error) {
      logger.error(`❌ Finish placement error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  socket.on('player_ready', (data, callback) => {
    const player = gameServer.players.get(socket.playerId);
    if (player && player.currentRoom) {
      const { isReady } = data;
      gameServer.updatePlayerReady(player.currentRoom, socket.playerId, isReady);
      
      // Notify other players
      socket.to(player.currentRoom).emit('player_ready_updated', {
        playerId: socket.playerId,
        isReady
      });
      
      if (callback) callback({ success: true });
      logger.debug(`🎯 Player ready status updated: ${socket.playerName} - ${isReady}`);
    }
  });

  // Alternative ready handler for web clients
  socket.on('set_ready', (data, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const { isReady } = data;
      const result = gameServer.updatePlayerReady(player.currentRoom, socket.playerId, isReady);
      
      // Notify other players in the room
      socket.to(player.currentRoom).emit('player_ready_updated', {
        playerId: socket.playerId,
        playerName: socket.playerName,
        isReady
      });
      
      // Check if game was auto-started due to all players being ready
      if (result.autoStarted) {
        // Notify all players in room about game start and placement phase
        io.to(player.currentRoom).emit('game_started', { 
          roomCode: player.currentRoom,
          ...result
        });
        
        // Also emit specific placement phase start event
        io.to(player.currentRoom).emit('placement_phase_started', {
          roomCode: player.currentRoom,
          phase: 'placement',
          players: result.players,
          gameState: result.gameState
        });
        
        logger.info(`🚀 Game auto-started in room: ${player.currentRoom} due to all players ready`);
      }
      
      if (callback) callback({ success: true, isReady, autoStarted: result.autoStarted });
      logger.info(`🎯 Player ready status updated: ${socket.playerName} - ${isReady ? 'READY' : 'NOT READY'}`);
    } catch (error) {
      logger.error(`❌ Set ready error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  // Fill AI players handler
  socket.on('fill_ai_players', (data, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const room = gameServer.rooms.get(player.currentRoom);
      if (!room) {
        throw new Error('Room not found');
      }

      // Check if player is host
      if (player.playerId !== room.hostId) {
        throw new Error('Only the host can fill AI players');
      }

      const currentPlayerCount = room.players.size;
      const openSpots = room.maxPlayers - currentPlayerCount;

      if (openSpots <= 0) {
        throw new Error('No open spots available');
      }

      logger.info(`🤖 Host ${socket.playerName} requested AI fill for room ${player.currentRoom} (${openSpots} open spots)`);

      // Use the existing auto-fill logic
      gameServer.attemptAutoFill(player.currentRoom);

      if (callback) callback({ success: true, filledSpots: openSpots });
      logger.info(`✅ AI players added to room ${player.currentRoom}`);

    } catch (error) {
      logger.error(`❌ Fill AI players error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  // Move handling
  socket.on('send_move', (moveData, callback) => {
    try {
      const player = gameServer.players.get(socket.playerId);
      if (!player || !player.currentRoom) {
        throw new Error('Not in a room');
      }

      const result = gameServer.processMove(player.currentRoom, moveData);
      
      // Broadcast move to all players in room
      io.to(player.currentRoom).emit('move_received', moveData);
      io.to(player.currentRoom).emit('game_state_updated', result.gameState);
      
      if (callback) callback({ success: true, gameState: result.gameState });
      
      logger.info(`🎯 Move processed and broadcast in room: ${player.currentRoom}`);
    } catch (error) {
      logger.error(`❌ Move processing error: ${error.message}`);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  // Information requests
  socket.on('get_room_info', (data, callback) => {
    const { roomCode } = data;
    const roomInfo = gameServer.getRoomInfo(roomCode);
    
    if (callback) {
      if (roomInfo) {
        callback({ success: true, roomInfo });
      } else {
        callback({ success: false, error: 'Room not found' });
      }
    }
  });

  socket.on('get_server_stats', (callback) => {
    const stats = gameServer.getServerStats();
    if (callback) callback({ success: true, stats });
  });

  // Legacy Unity test handlers (for compatibility)
  socket.on('hello', (data) => {
    logger.debug(`👋 Hello message from ${socket.playerName}:`, data);
    socket.emit('hello', {
      date: Date.now(),
      data: data,
      serverResponse: 'Hello from 3D Chess Server!',
      playerId: socket.playerId
    });
  });

  socket.on('test_connection', (data) => {
    logger.debug(`🧪 Test connection from ${socket.playerName}:`, data);
    socket.emit('test_response', {
      date: Date.now(),
      success: true,
      message: 'Test successful - Socket.IO is working!',
      receivedData: data,
      playerId: socket.playerId
    });
  });

  socket.on('spin', (data) => {
    logger.debug(`🌀 Spin event from ${socket.playerName}:`, data);
    socket.emit('spin', { date: Date.now(), data: data });
  });

  socket.on('class', (data) => {
    logger.debug(`📚 Class event from ${socket.playerName}:`, data);
    socket.emit('class', { date: Date.now(), data: data });
  });

  // Enhanced heartbeat handling with logging
  socket.on('heartbeat', () => {
    const heartbeatData = { 
      timestamp: Date.now(),
      playerId: socket.playerId,
      playerName: socket.playerName,
      connected: socket.connected
    };
    socket.emit('heartbeat_response', heartbeatData);
    logger.debug(`💓 Heartbeat from ${socket.playerName}: ${socket.connected ? 'healthy' : 'weak'}`);
  });

  // Ping monitoring for connection quality
  socket.on('ping', () => {
    logger.debug(`🏓 Ping from ${socket.playerName} (${socket.id})`);
  });

  socket.on('pong', (latency) => {
    logger.debug(`🏓 Pong from ${socket.playerName}: ${latency}ms latency`);
  });

  // Enhanced disconnect handling with grace period
  socket.on('disconnect', (reason) => {
    const player = gameServer.players.get(socket.playerId);
    logger.info(`🔌 Player disconnected: ${socket.playerName} (${reason}) from ${socket.handshake.address}`);
    
    if (player && player.currentRoom) {
      // Distinguish between different disconnect reasons
      const isNetworkIssue = ['transport close', 'transport error', 'ping timeout', 'client namespace disconnect'].includes(reason);
      const isIntentionalLeave = reason === 'client namespace disconnect' || reason === 'server namespace disconnect';
      
      // Notify other players in room about disconnect
      socket.to(player.currentRoom).emit('player_disconnected', {
        playerId: socket.playerId,
        playerName: socket.playerName,
        reason,
        isNetworkIssue,
        hasGracePeriod: isNetworkIssue
      });
      
      // Handle room leaving with grace period for network issues
      if (isNetworkIssue) {
        logger.info(`📞 Network disconnect detected for ${socket.playerName} - applying grace period`);
        gameServer.leaveRoom(player.currentRoom, socket.playerId, true); // isDisconnect = true
        
        // Don't delete player data yet - keep for reconnection
        player.lastDisconnect = {
          reason,
          timestamp: Date.now(),
          socketId: socket.id,
          room: player.currentRoom
        };
      } else {
        logger.info(`👋 Intentional disconnect for ${socket.playerName} - immediate cleanup`);
        gameServer.leaveRoom(player.currentRoom, socket.playerId, false); // isDisconnect = false
        gameServer.players.delete(socket.playerId);
      }
    } else {
      // No room association - clean up immediately
      gameServer.players.delete(socket.playerId);
    }
  });
});

// Express routes for web interface and monitoring
app.get('/', (req, res) => {
  res.json({
    message: '3D Chess Server',
    status: 'running',
    version: '1.0.0',
    stats: gameServer.getServerStats()
  });
});

app.get('/api/rooms', (req, res) => {
  const rooms = Array.from(gameServer.rooms.values()).map(room => ({
    roomCode: room.roomCode,
    playerCount: room.players.size,
    maxPlayers: room.maxPlayers,
    gamePhase: room.gameState.phase,
    isJoinable: room.players.size < room.maxPlayers && room.gameState.phase === 'waiting'
  }));
  
  res.json({ rooms });
});

app.get('/api/stats', (req, res) => {
  res.json(gameServer.getServerStats());
});

// Error handling
app.use((err, req, res, next) => {
  logger.error('Express error:', err);
  res.status(500).json({ error: 'Internal server error' });
});

// Start server
server.listen(PORT, () => {
  logger.info(`🚀 3D Chess Server listening on port ${PORT}`);
  logger.info(`🌐 Server URL: http://localhost:${PORT}`);
  logger.info(`💡 Ready for Unity Socket.IO connections`);
  logger.info(`🎮 Environment: ${NODE_ENV}`);
});

// Graceful shutdown
const gracefulShutdown = (signal) => {
  logger.info(`🛑 ${signal} received, shutting down gracefully`);
  
  // Stop persistence and save final state
  gameServer.stopPersistence();
  
  // Close server
  server.close(() => {
    logger.info('✅ Server closed');
    process.exit(0);
  });
  
  // Force exit after 10 seconds
  setTimeout(() => {
    logger.error('❌ Forced shutdown after timeout');
    process.exit(1);
  }, 10000);
};

process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));
process.on('SIGINT', () => gracefulShutdown('SIGINT'));

module.exports = { app, server, gameServer };