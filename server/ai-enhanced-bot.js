/**
 * Enhanced AI Bot for 3D Chess Server
 * Uses the new ChessBrain for sophisticated AI decision-making
 * Supports personality-driven behavior and advanced multi-player strategies
 */

const { TestClient } = require('./test-client.js');
const ChessBrain = require('./ai-logic/chess-brain.js');

class EnhancedAIBot extends TestClient {
  constructor(roomCode, name = 'Enhanced_AI', difficulty = 'medium', personality = 'balanced', serverUrl = 'http://localhost:3000') {
    super(name, serverUrl);
    this.roomCode = roomCode;
    this.difficulty = difficulty;
    this.personality = personality;
    this.myColor = null;
    this.gameState = null;
    this.isMyTurn = false;
    this.boardSize = 4; // Default to 4x4x4, will update if needed
    this.moveHistory = [];
    this.isGameActive = false;
    
    // Initialize AI brain
    this.brain = new ChessBrain(difficulty, personality, true);
    
    // Performance tracking
    this.moveCount = 0;
    this.totalThinkingTime = 0;
    this.moveTimeouts = [];
    
    console.log(`🧠 Enhanced AI Bot created for room ${roomCode}`);
    console.log(`   Difficulty: ${difficulty}, Personality: ${personality}`);
    console.log(`   AI Brain: ${this.brain.maxSearchDepth} depth, ${this.brain.maxThinkingTime}s time limit`);
  }

  async start() {
    try {
      console.log(`🚀 Enhanced AI Bot starting up...`);
      console.log(`🎯 Target room: ${this.roomCode}`);
      console.log(`🧠 AI Configuration: ${this.difficulty}/${this.personality}`);
      console.log(`👤 Bot name: ${this.name}`);
      console.log('');
      
      // Connect to server with detailed logging
      console.log(`🔌 Connecting to server...`);
      await this.connect();
      console.log(`✅ Connected successfully!`);
      
      // Setup enhanced game event listeners
      this.setupEnhancedGameListeners();
      console.log(`📡 Enhanced event listeners configured`);
      
      // Join the specified room with detailed error handling
      console.log(`🚪 Attempting to join room ${this.roomCode}...`);
      try {
        const joinResponse = await this.joinRoom(this.roomCode);
        this.myColor = joinResponse.assignedColor;
        
        console.log(`✅ Successfully joined room ${this.roomCode}!`);
        console.log(`🎨 Assigned color: ${this.myColor}`);
        console.log(`📊 Game state:`, joinResponse.gameState);
        
        // Update game state
        this.gameState = joinResponse.gameState;
        this.boardSize = joinResponse.gameState?.boardSize || 4;
        this.isGameActive = true;
        
        console.log(`🎮 Game active: ${this.isGameActive}, Board size: ${this.boardSize}x${this.boardSize}x${this.boardSize}`);
        
        // Check if it's immediately our turn (shouldn't happen normally)
        if (joinResponse.gameState?.currentPlayer === this.myColor) {
          console.log(`⚡ It's immediately my turn after joining - scheduling move`);
          setTimeout(() => this.makeMove(), 1000);
        }
        
      } catch (joinError) {
        console.error(`❌ Failed to join room ${this.roomCode}:`, joinError.message);
        return;
      }
      
      console.log(`🎉 Enhanced AI Bot setup complete!`);
      console.log(`⏳ Waiting for game events...`);
      
    } catch (error) {
      console.error(`💥 Enhanced AI Bot startup failed:`, error);
    }
  }

  setupEnhancedGameListeners() {
    // Enhanced move event handling
    this.socket.on('move', (data) => {
      console.log(`📢 Move received:`, data);
      
      if (data.success) {
        // Update our game state with the successful move
        this.updateGameState(data);
        
        // Check if it's now our turn
        if (data.nextPlayer === this.myColor) {
          console.log(`🎯 It's now my turn after ${data.player}'s move`);
          this.isMyTurn = true;
          
          // Schedule our move with enhanced timing
          const thinkingDelay = this.calculateThinkingDelay();
          console.log(`⏰ Scheduling move in ${thinkingDelay}ms`);
          setTimeout(() => this.makeMove(), thinkingDelay);
        } else {
          this.isMyTurn = false;
          console.log(`⏳ Waiting for ${data.nextPlayer}'s move`);
        }
      } else {
        console.log(`❌ Move failed for ${data.player}: ${data.error}`);
      }
    });

    // Game state updates
    this.socket.on('gameState', (data) => {
      console.log(`📊 Game state update:`, data);
      this.gameState = data;
      
      if (data.currentPlayer === this.myColor && !this.isMyTurn) {
        console.log(`🎯 Game state indicates it's my turn`);
        this.isMyTurn = true;
        setTimeout(() => this.makeMove(), this.calculateThinkingDelay());
      }
    });

    // Game phase changes
    this.socket.on('gamePhase', (data) => {
      console.log(`📋 Game phase changed to: ${data.phase}`);
      
      if (data.phase === 'placement') {
        console.log(`🎪 Entering placement phase`);
      } else if (data.phase === 'playing') {
        console.log(`⚔️ Game started! Current player: ${data.currentPlayer}`);
        
        if (data.currentPlayer === this.myColor) {
          console.log(`🚀 I start the game!`);
          this.isMyTurn = true;
          setTimeout(() => this.makeMove(), this.calculateThinkingDelay());
        }
      }
    });

    // Enhanced placement handling
    this.socket.on('placementComplete', (data) => {
      console.log(`🎪 Placement completed by ${data.player}`);
      
      if (data.allPlayersReady) {
        console.log(`🎉 All players ready - game should start soon`);
      }
    });

    // Player join/leave events
    this.socket.on('playerJoined', (data) => {
      console.log(`👋 Player joined: ${data.playerName} as ${data.color}`);
    });

    this.socket.on('playerLeft', (data) => {
      console.log(`🚪 Player left: ${data.playerName}`);
    });

    // Game end events
    this.socket.on('gameEnd', (data) => {
      console.log(`🏁 Game ended!`, data);
      this.isGameActive = false;
      this.printFinalStatistics();
    });

    // Error handling
    this.socket.on('error', (error) => {
      console.error(`⚠️ Socket error:`, error);
    });
  }

  async makeMove() {
    if (!this.isMyTurn || !this.isGameActive) {
      console.log(`🚫 Not my turn or game not active (turn: ${this.isMyTurn}, active: ${this.isGameActive})`);
      return;
    }

    const moveStartTime = Date.now();
    console.log(`🤖 ${this.myColor} AI thinking...`);

    try {
      // Get the best move from our AI brain
      const move = await this.brain.getBestMove(this.gameState, this.myColor, {
        boardSize: this.boardSize,
        gamePhase: 'playing'
      });

      if (!move) {
        console.error(`❌ No valid move found for ${this.myColor}!`);
        return;
      }

      const thinkingTime = (Date.now() - moveStartTime) / 1000;
      console.log(`💭 AI decision made in ${thinkingTime.toFixed(2)}s: ${JSON.stringify(move)}`);
      
      // Update statistics
      this.moveCount++;
      this.totalThinkingTime += thinkingTime;
      
      // Execute the move
      this.isMyTurn = false; // Prevent multiple simultaneous moves
      await this.sendMove(move);
      
    } catch (error) {
      console.error(`💥 Error making move:`, error);
      
      // Try to make a random move as fallback
      try {
        const fallbackMove = await this.getFallbackMove();
        if (fallbackMove) {
          console.log(`🎲 Using fallback move: ${JSON.stringify(fallbackMove)}`);
          await this.sendMove(fallbackMove);
        }
      } catch (fallbackError) {
        console.error(`💥 Fallback move also failed:`, fallbackError);
        this.isMyTurn = true; // Allow retry
      }
    }
  }

  async sendMove(move) {
    return new Promise((resolve, reject) => {
      console.log(`📤 Sending move: ${JSON.stringify(move)}`);
      
      const timeout = setTimeout(() => {
        reject(new Error('Move timeout'));
      }, 10000); // 10 second timeout
      
      this.socket.emit('move', move, (response) => {
        clearTimeout(timeout);
        
        if (response.success) {
          console.log(`✅ Move accepted: ${JSON.stringify(move)}`);
          this.moveHistory.push({
            move: move,
            timestamp: Date.now(),
            success: true
          });
          resolve(response);
        } else {
          console.error(`❌ Move rejected: ${response.error}`);
          this.moveHistory.push({
            move: move,
            timestamp: Date.now(),
            success: false,
            error: response.error
          });
          reject(new Error(response.error));
        }
      });
    });
  }

  async getFallbackMove() {
    // Simple fallback: try to find any legal move
    if (!this.gameState || !this.gameState.pieces) {
      return null;
    }

    // Find our pieces
    const ourPieces = [];
    for (const position in this.gameState.pieces) {
      const piece = this.gameState.pieces[position];
      if (piece.color === this.myColor) {
        ourPieces.push({ position, piece });
      }
    }

    if (ourPieces.length === 0) {
      return null;
    }

    // Try to make a simple move
    for (const { position, piece } of ourPieces) {
      const [x, y, z] = position.split(',').map(Number);
      
      // Try simple moves in each direction
      const directions = [
        [1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]
      ];
      
      for (const [dx, dy, dz] of directions) {
        const newX = x + dx;
        const newY = y + dy;
        const newZ = z + dz;
        
        if (newX >= 0 && newX < this.boardSize && 
            newY >= 0 && newY < this.boardSize && 
            newZ >= 0 && newZ < this.boardSize) {
          
          const newPosition = `${newX},${newY},${newZ}`;
          const targetPiece = this.gameState.pieces[newPosition];
          
          // Check if move is legal (empty square or opponent piece)
          if (!targetPiece || targetPiece.color !== this.myColor) {
            return {
              from: position,
              to: newPosition,
              piece: piece.type,
              player: this.myColor
            };
          }
        }
      }
    }

    return null;
  }

  updateGameState(moveData) {
    if (!this.gameState) {
      this.gameState = { pieces: {}, currentPlayer: null };
    }

    // Apply the move to our local game state
    if (moveData.success && moveData.from && moveData.to) {
      // Move the piece
      this.gameState.pieces[moveData.to] = this.gameState.pieces[moveData.from];
      delete this.gameState.pieces[moveData.from];
      
      // Update current player
      this.gameState.currentPlayer = moveData.nextPlayer;
    }
  }

  calculateThinkingDelay() {
    // Add some human-like variation to thinking time
    const baseDelay = this.personality === 'aggressive' ? 500 : 
                     this.personality === 'defensive' ? 2000 : 1000;
    
    const variation = Math.random() * 1000;
    return Math.floor(baseDelay + variation);
  }

  printFinalStatistics() {
    console.log('\n📊 Final AI Bot Statistics:');
    console.log(`   Moves made: ${this.moveCount}`);
    console.log(`   Total thinking time: ${this.totalThinkingTime.toFixed(2)}s`);
    console.log(`   Average thinking time: ${(this.totalThinkingTime / Math.max(1, this.moveCount)).toFixed(2)}s`);
    
    const brainStats = this.brain.getStatistics();
    console.log(`   Nodes evaluated: ${brainStats.nodesEvaluated}`);
    console.log(`   Cache hits: ${brainStats.cacheHits}`);
    console.log(`   Cache hit rate: ${(brainStats.cacheHits / Math.max(1, brainStats.nodesEvaluated) * 100).toFixed(1)}%`);
    
    const successfulMoves = this.moveHistory.filter(m => m.success).length;
    const failedMoves = this.moveHistory.filter(m => !m.success).length;
    console.log(`   Successful moves: ${successfulMoves}`);
    console.log(`   Failed moves: ${failedMoves}`);
    console.log(`   Success rate: ${(successfulMoves / Math.max(1, this.moveHistory.length) * 100).toFixed(1)}%`);
  }

  // Override disconnect to clean up
  disconnect() {
    console.log(`🔌 Enhanced AI Bot disconnecting...`);
    this.isGameActive = false;
    this.isMyTurn = false;
    
    // Clear any pending timeouts
    this.moveTimeouts.forEach(timeout => clearTimeout(timeout));
    this.moveTimeouts = [];
    
    super.disconnect();
    console.log(`👋 Enhanced AI Bot disconnected`);
  }
}

module.exports = EnhancedAIBot;

// Command-line interface for running the enhanced bot
if (require.main === module) {
  const args = process.argv.slice(2);
  
  if (args.length < 1) {
    console.log('Usage: node ai-enhanced-bot.js <room-code> [name] [difficulty] [personality] [server-url]');
    console.log('');
    console.log('Difficulties: easy, medium, hard');
    console.log('Personalities: aggressive, defensive, balanced, tactical');
    console.log('');
    console.log('Examples:');
    console.log('  node ai-enhanced-bot.js ABC123');
    console.log('  node ai-enhanced-bot.js ABC123 "Smart AI" hard tactical');
    console.log('  node ai-enhanced-bot.js ABC123 "Aggressive AI" medium aggressive http://localhost:3000');
    process.exit(1);
  }
  
  const roomCode = args[0];
  const name = args[1] || 'Enhanced_AI';
  const difficulty = args[2] || 'medium';
  const personality = args[3] || 'balanced';
  const serverUrl = args[4] || 'http://localhost:3000';
  
  console.log(`🚀 Starting Enhanced AI Bot:`);
  console.log(`   Room: ${roomCode}`);
  console.log(`   Name: ${name}`);
  console.log(`   Difficulty: ${difficulty}`);
  console.log(`   Personality: ${personality}`);
  console.log(`   Server: ${serverUrl}`);
  console.log('');
  
  const bot = new EnhancedAIBot(roomCode, name, difficulty, personality, serverUrl);
  bot.start().catch(error => {
    console.error('Failed to start Enhanced AI Bot:', error);
    process.exit(1);
  });
  
  // Graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down Enhanced AI Bot...');
    bot.disconnect();
    process.exit(0);
  });
}