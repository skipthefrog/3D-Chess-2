/**
 * AI Host Room Creator
 * Creates a fresh room with an AI player as the host
 * Perfect for testing when you need a room that's immediately ready to join
 */

const { TestClient } = require('./test-client.js');

class AIHostRoomCreator extends TestClient {
  constructor(difficulty = 'medium', aiName = null, serverUrl = 'http://localhost:3000') {
    const name = aiName || `AI_Host_${difficulty}_${Date.now()}`;
    super(name, serverUrl);
    
    this.difficulty = difficulty;
    this.roomCode = null;
    this.isHost = true;
    this.myColor = 'White'; // Host always gets White
    this.gameState = null;
    this.isMyTurn = false;
    this.boardSize = 4; // Default to 4x4x4
    this.moveHistory = [];
    this.isGameActive = false;
    this.waitingForPlayer = true;
    this.heartbeatInterval = null;
    this.connectionHealthy = true;
    this.lastHeartbeat = null;
    
    console.log(`🤖 AI Host Creator initialized: ${name} (${difficulty})`);
  }

  async createAndHostRoom() {
    try {
      console.log(`🚀 === AI Host Room Creator ===`);
      console.log(`🤖 AI Name: ${this.name}`);
      console.log(`🧠 Difficulty: ${this.difficulty}`);
      console.log(`🌐 Server: ${this.serverUrl}`);
      console.log('');

      // Step 1: Connect to server
      console.log(`🔌 Step 1: Connecting to server...`);
      await this.connect();
      console.log(`✅ Connected successfully!`);

      // Step 2: Create room (AI becomes host)
      console.log(`🏠 Step 2: Creating room as AI host...`);
      const roomResponse = await this.createRoom();
      this.roomCode = roomResponse.roomCode;
      this.gameState = roomResponse.gameState;
      
      console.log(`✅ Room created successfully!`);
      console.log(`🎯 Room Code: ${this.roomCode}`);
      console.log(`🎨 AI Color: ${this.myColor} (Host)`);
      console.log(`📊 Game Phase: ${this.gameState.phase}`);
      console.log('');

      // Step 3: Setup AI game logic and connection monitoring
      console.log(`🧠 Step 3: Setting up AI game logic...`);
      this.setupAIHostListeners();
      this.startHeartbeat();
      console.log(`✅ AI game logic and connection monitoring activated`);

      // Step 4: Set AI ready automatically
      console.log(`🎮 Step 4: Setting AI ready status...`);
      await this.setReady(true);
      console.log(`✅ AI is ready to play!`);
      console.log('');

      // Success summary
      console.log(`🎉 === SUCCESS! AI-Hosted Room Ready ===`);
      console.log(`🏠 Room Code: ${this.roomCode}`);
      console.log(`🤖 AI Host: ${this.name} (White pieces)`);
      console.log(`🧠 Difficulty: ${this.difficulty}`);
      console.log(`⏳ Status: Waiting for human player to join`);
      console.log('');
      console.log(`💡 Next steps:`);
      console.log(`   1. Join room ${this.roomCode} in Unity`);
      console.log(`   2. Set your player ready`);
      console.log(`   3. Game will start automatically!`);
      console.log(`   4. AI will make moves when it's its turn`);
      console.log('');
      console.log(`🔧 Monitoring:`);
      console.log(`   - Dashboard: http://localhost:3000`);
      console.log(`   - Room API: http://localhost:3000/api/rooms/${this.roomCode}`);
      console.log(`   - Press Ctrl+C to stop AI and close room`);
      console.log('');

      return {
        success: true,
        roomCode: this.roomCode,
        aiName: this.name,
        difficulty: this.difficulty,
        message: `AI-hosted room ${this.roomCode} created successfully`
      };

    } catch (error) {
      console.error(`❌ Failed to create AI-hosted room:`, error.message);
      
      console.log('');
      console.log(`🔧 Troubleshooting:`);
      console.log(`   1. Make sure the server is running:`);
      console.log(`      cd "/Users/skip/3D Chess 2/server"`);
      console.log(`      node server-enhanced.js`);
      console.log('');
      console.log(`   2. Check server status:`);
      console.log(`      curl http://localhost:3000/api/server/status`);
      console.log('');
      console.log(`   3. Check server logs for errors`);
      
      return {
        success: false,
        error: error.message
      };
    }
  }

  startHeartbeat() {
    // Send heartbeat every 10 seconds to maintain connection
    this.heartbeatInterval = setInterval(() => {
      if (this.socket && this.socket.connected) {
        this.socket.emit('heartbeat');
        console.log(`💓 ${this.name}: Heartbeat sent`);
      } else {
        console.log(`❌ ${this.name}: Heartbeat failed - socket not connected`);
        this.connectionHealthy = false;
      }
    }, 10000);

    // Monitor heartbeat responses
    this.socket.on('heartbeat_response', (data) => {
      this.lastHeartbeat = Date.now();
      this.connectionHealthy = true;
      console.log(`💓 ${this.name}: Heartbeat acknowledged (${data.timestamp})`);
    });

    console.log(`💓 ${this.name}: Heartbeat monitoring started (10s interval)`);
  }

  stopHeartbeat() {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
      console.log(`💓 ${this.name}: Heartbeat monitoring stopped`);
    }
  }

  setupAIHostListeners() {
    // Enhanced AI listeners for hosting responsibilities
    
    // Handle when human player joins
    this.socket.on('player_joined', (data) => {
      console.log(`👥 Human player joined: ${data.playerName} (${data.assignedColor})`);
      console.log(`🎮 Room is now full! Waiting for human player to set ready...`);
      this.waitingForPlayer = false;
    });

    // Handle when human player leaves
    this.socket.on('player_left', (data) => {
      console.log(`👋 Human player left: ${data.playerName}`);
      console.log(`⏳ AI waiting for another player to join...`);
      this.waitingForPlayer = true;
    });

    // Handle player ready status updates
    this.socket.on('player_ready_updated', (data) => {
      console.log(`🎯 Player ready status: ${data.playerId} - ${data.isReady}`);
      
      // If human player is ready and we have 2 players, start the game
      if (data.isReady && !this.waitingForPlayer) {
        console.log(`🚀 Both players ready! Starting game...`);
        this.startGame();
      }
    });

    // Handle game start
    this.socket.on('game_started', (data) => {
      console.log(`🎮 Game started! AI vs Human chess match begins!`);
      this.gameState = data.gameState;
      this.isGameActive = true;
      this.checkIfMyTurn();
    });

    // Handle moves from human player
    this.socket.on('move_received', (moveData) => {
      console.log(`🎯 Received move from ${moveData.playerColor}: ${moveData.fromX},${moveData.fromY},${moveData.fromZ} → ${moveData.toX},${moveData.toY},${moveData.toZ}`);
      this.moveHistory.push(moveData);
      this.isMyTurn = this.gameState.currentPlayer === this.myColor;
      
      if (this.isMyTurn) {
        console.log(`🤖 It's AI's turn now...`);
        setTimeout(() => this.makeAIMove(), this.getThinkingTime());
      }
    });

    // Handle game state updates
    this.socket.on('game_state_updated', (gameState) => {
      this.gameState = gameState;
      this.checkIfMyTurn();
    });

    // Handle disconnections gracefully
    this.socket.on('player_disconnected', (data) => {
      if (data.hasGracePeriod) {
        console.log(`📞 Player ${data.playerName} disconnected (${data.reason}) - grace period active`);
        console.log(`⏳ AI waiting for player to reconnect...`);
      } else {
        console.log(`👋 Player ${data.playerName} left permanently`);
        this.waitingForPlayer = true;
      }
    });

    // Handle reconnections
    this.socket.on('player_reconnected', (data) => {
      console.log(`🔄 Player ${data.playerName} reconnected! Game can continue.`);
      this.waitingForPlayer = false;
    });
  }

  checkIfMyTurn() {
    if (this.gameState && this.gameState.currentPlayer === this.myColor && this.isGameActive) {
      this.isMyTurn = true;
      console.log(`🤖 It's AI's turn (${this.myColor})`);
      setTimeout(() => this.makeAIMove(), this.getThinkingTime());
    } else {
      this.isMyTurn = false;
    }
  }

  async startGame() {
    return new Promise((resolve, reject) => {
      console.log(`🎮 AI Host starting the game...`);
      
      this.socket.emit('start_game', {}, (response) => {
        if (response.success) {
          console.log(`✅ Game started successfully!`);
          this.gameState = response.gameState;
          this.isGameActive = true;
          this.checkIfMyTurn();
          resolve(response);
        } else {
          console.error(`❌ Failed to start game:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  makeAIMove() {
    if (!this.isMyTurn || !this.isGameActive) return;

    console.log(`🧠 AI thinking... (${this.difficulty} difficulty)`);
    
    // Generate AI move based on difficulty
    const move = this.generateAIMove();
    
    console.log(`🎯 AI move: ${move.fromX},${move.fromY},${move.fromZ} → ${move.toX},${move.toY},${move.toZ}`);
    
    // Send move to server
    this.socket.emit('send_move', move, (response) => {
      if (response.success) {
        console.log(`✅ AI move accepted`);
        this.moveHistory.push(move);
        this.gameState = response.gameState;
        this.isMyTurn = false;
        console.log(`⏳ Waiting for human player's move...`);
      } else {
        console.error(`❌ AI move rejected:`, response.error);
        // Try alternative move
        setTimeout(() => this.makeAIMove(), 1000);
      }
    });
  }

  generateAIMove() {
    // AI move generation based on difficulty
    // This is a simplified version - in a real implementation, you'd have
    // full 3D chess logic here
    
    const move = {
      fromX: Math.floor(Math.random() * this.boardSize),
      fromY: Math.floor(Math.random() * this.boardSize),
      fromZ: Math.floor(Math.random() * this.boardSize),
      toX: Math.floor(Math.random() * this.boardSize),
      toY: Math.floor(Math.random() * this.boardSize),
      toZ: Math.floor(Math.random() * this.boardSize),
      playerColor: this.myColor,
      piece: 'Pawn', // Simplified
      timestamp: Date.now()
    };

    // Add difficulty-based thinking patterns
    switch (this.difficulty) {
      case 'easy':
        // Random moves
        break;
      case 'medium':
        // Try to move towards center
        move.toX = Math.min(move.toX + 1, this.boardSize - 1);
        move.toY = Math.min(move.toY + 1, this.boardSize - 1);
        break;
      case 'hard':
        // More strategic positioning
        move.toX = Math.floor(this.boardSize / 2);
        move.toY = Math.floor(this.boardSize / 2);
        break;
    }

    return move;
  }

  getThinkingTime() {
    // Return thinking time in milliseconds based on difficulty
    switch (this.difficulty) {
      case 'easy': return 500 + Math.random() * 1000; // 0.5-1.5s
      case 'medium': return 1000 + Math.random() * 2000; // 1-3s
      case 'hard': return 2000 + Math.random() * 3000; // 2-5s
      default: return 1500;
    }
  }

  // Keep the AI running and responsive
  async keepAlive() {
    return new Promise((resolve) => {
      console.log(`🔄 AI Host is running. Press Ctrl+C to stop.`);
      
      const keepAliveInterval = setInterval(() => {
        if (!this.socket?.connected) {
          console.log(`❌ AI Host disconnected. Stopping...`);
          clearInterval(keepAliveInterval);
          resolve(false);
        }
      }, 5000);

      // Handle graceful shutdown
      process.on('SIGINT', () => {
        console.log('\\n🛑 Shutting down AI Host...');
        console.log(`🗑️ Room ${this.roomCode} will be cleaned up`);
        clearInterval(keepAliveInterval);
        this.disconnect();
        setTimeout(() => process.exit(0), 1500);
      });
    });
  }

  disconnect() {
    console.log(`🔌 AI Host disconnecting...`);
    
    // Stop heartbeat monitoring
    this.stopHeartbeat();
    
    // Disconnect socket
    if (this.socket) {
      this.socket.disconnect();
      console.log(`✅ AI Host disconnected`);
    }
    
    // Clean up state
    this.connectionHealthy = false;
    this.isGameActive = false;
  }
}

// Command line interface
async function main() {
  const args = process.argv.slice(2);
  
  // Parse command line options
  const difficulty = args.find(arg => ['easy', 'medium', 'hard'].includes(arg)) || 'medium';
  const aiName = args.find(arg => !['easy', 'medium', 'hard'].includes(arg));
  
  // Show help
  if (args.includes('--help') || args.includes('-h')) {
    showHelp();
    process.exit(0);
  }

  console.log(`🚀 Starting AI Host Room Creator...`);
  
  const aiHost = new AIHostRoomCreator(difficulty, aiName);

  try {
    const result = await aiHost.createAndHostRoom();
    
    if (result.success) {
      // Keep the AI running
      await aiHost.keepAlive();
    } else {
      process.exit(1);
    }
    
  } catch (error) {
    console.error('❌ AI Host creation failed:', error.message);
    process.exit(1);
  }
}

function showHelp() {
  console.log(`
🤖 AI Host Room Creator - 3D Chess Testing Tool

USAGE:
  node create-ai-hosted-room.js [difficulty] [aiName]

ARGUMENTS:
  difficulty   Optional. AI difficulty: easy, medium, hard (default: medium)
  aiName       Optional. Custom name for AI host (default: auto-generated)

EXAMPLES:
  node create-ai-hosted-room.js
  node create-ai-hosted-room.js hard
  node create-ai-hosted-room.js medium ChessBot

WORKFLOW:
  1. AI creates and hosts a new room (gets White pieces)
  2. AI automatically sets ready status
  3. Join the room in Unity (you'll get Black pieces)
  4. Set yourself ready to start the game
  5. AI will make moves automatically when it's its turn

DIFFICULTY LEVELS:
  easy    - Random moves, 0.5-1.5s thinking time
  medium  - Tactical play, center control, 1-3s thinking time
  hard    - Strategic positioning, 2-5s thinking time

FEATURES:
  ✅ AI hosts the room (White pieces)
  ✅ Automatic game management
  ✅ Real-time move generation
  ✅ Grace period support for disconnections
  ✅ Perfect for solo testing

MONITORING:
  🔧 Dashboard: http://localhost:3000
  🔧 Server status: http://localhost:3000/api/server/status
  🔧 Room API: http://localhost:3000/api/rooms/[ROOM_CODE]

This tool creates a fully managed AI opponent ready for immediate play!
`);
}

module.exports = { AIHostRoomCreator };

if (require.main === module) {
  main();
}