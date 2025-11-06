/**
 * AI Bot for 3D Chess Server
 * Automatically joins a room and plays as an AI opponent
 * Based on the TestClient class with added chess AI logic
 */

const { TestClient } = require('./test-client.js');

class AIBot extends TestClient {
  constructor(roomCode, name = 'AI_Player', difficulty = 'medium', serverUrl = 'http://localhost:3000') {
    super(name, serverUrl);
    this.roomCode = roomCode;
    this.difficulty = difficulty;
    this.myColor = null;
    this.gameState = null;
    this.isMyTurn = false;
    this.boardSize = 4; // Default to 4x4x4, will update if needed
    this.moveHistory = [];
    this.isGameActive = false;
    
    console.log(`🤖 AI Bot created for room ${roomCode} with difficulty ${difficulty}`);
  }

  async start() {
    try {
      console.log(`🚀 AI Bot starting up...`);
      console.log(`🎯 Target room: ${this.roomCode}`);
      console.log(`🧠 Difficulty: ${this.difficulty}`);
      console.log(`👤 Name: ${this.name}`);
      console.log('');
      
      // Connect to server with detailed logging
      console.log(`🔌 Connecting to server...`);
      await this.connect();
      console.log(`✅ Connected successfully!`);
      
      // Setup enhanced game event listeners BEFORE joining
      this.setupAIGameListeners();
      console.log(`📡 Event listeners configured`);
      
      // Join the specified room with detailed error handling
      console.log(`🚪 Attempting to join room ${this.roomCode}...`);
      try {
        const joinResponse = await this.joinRoom(this.roomCode);
        this.myColor = joinResponse.assignedColor;
        
        console.log(`✅ Successfully joined room ${this.roomCode}!`);
        console.log(`🎨 Assigned color: ${this.myColor}`);
        console.log(`📊 Game state:`, joinResponse.gameState);
        
      } catch (joinError) {
        console.error(`❌ Failed to join room ${this.roomCode}`);
        console.error(`   Error: ${joinError.message}`);
        
        // Provide helpful suggestions based on the error
        this.handleJoinError(joinError.message);
        this.disconnect();
        return;
      }
      
      // Set ready automatically
      console.log(`🎯 Setting ready status...`);
      await this.setReady(true);
      console.log(`✅ AI Bot is ready to play!`);
      
      console.log('');
      console.log(`🎮 AI Bot setup complete. Waiting for game to start...`);
      console.log(`💡 The bot will automatically play when it's its turn.`);
      console.log(`🛑 Press Ctrl+C to stop the bot.`);
      console.log('');
      
    } catch (error) {
      console.error(`🚨 AI Bot startup failed:`, error.message);
      console.error(`   Stack:`, error.stack);
      this.disconnect();
      throw error;
    }
  }

  setupAIGameListeners() {
    // Override the base game listeners with AI-specific logic
    this.socket.on('game_started', (data) => {
      console.log(`🚀 Game started! AI Bot (${this.myColor}) is active`);
      this.gameState = data.gameState;
      this.isGameActive = true;
      
      // Check if it's our turn to start
      if (data.gameState && data.gameState.currentPlayer === this.myColor) {
        this.isMyTurn = true;
        console.log(`🎯 It's AI's turn! Making first move...`);
        setTimeout(() => this.makeAIMove(), 1000); // Brief delay for realism
      } else {
        this.isMyTurn = false;
        console.log(`⏳ Waiting for ${data.gameState?.currentPlayer || 'opponent'} to move...`);
      }
    });

    this.socket.on('move_received', (data) => {
      console.log(`📨 Move received from ${data.playerColor}: (${data.fromX},${data.fromY},${data.fromZ}) → (${data.toX},${data.toY},${data.toZ})`);
      
      // Update our move history
      this.moveHistory.push({
        from: { x: data.fromX, y: data.fromY, z: data.fromZ },
        to: { x: data.toX, y: data.toY, z: data.toZ },
        player: data.playerColor,
        timestamp: Date.now()
      });
      
      // Check if it's now our turn
      if (this.isGameActive && data.playerColor !== this.myColor) {
        this.isMyTurn = true;
        console.log(`🎯 Opponent moved, now it's AI's turn!`);
        
        // Make our move after a realistic thinking delay
        const thinkingTime = this.getThinkingTime();
        console.log(`🧠 AI thinking for ${thinkingTime}ms...`);
        setTimeout(() => this.makeAIMove(), thinkingTime);
      }
    });

    this.socket.on('game_state_updated', (data) => {
      console.log(`📊 Game state updated:`, data);
      this.gameState = data;
      
      // Update board size if provided
      if (data.boardSize) {
        this.boardSize = data.boardSize;
        console.log(`📏 Board size updated to ${this.boardSize}x${this.boardSize}x${this.boardSize}`);
      }
    });

    this.socket.on('player_joined', (data) => {
      console.log(`👥 Player joined: ${data.playerName} (${data.assignedColor})`);
    });

    this.socket.on('player_left', (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
    });

    // Game end events
    this.socket.on('game_ended', (data) => {
      console.log(`🏁 Game ended! Result:`, data);
      this.isGameActive = false;
      this.isMyTurn = false;
    });
  }

  async makeAIMove() {
    if (!this.isMyTurn || !this.isGameActive) {
      console.log(`🚫 Not AI's turn or game not active`);
      return;
    }

    try {
      console.log(`🤖 AI Bot (${this.myColor}) analyzing position...`);
      
      // Use AI strategy to find the best move
      const move = this.findBestMove();
      
      if (move) {
        console.log(`🎯 AI Bot decided: ${move.description}`);
        
        // Send the move to the server
        const moveResponse = await this.sendMove(move.from, move.to, this.myColor);
        
        if (moveResponse.success) {
          console.log(`✅ AI move executed successfully!`);
          this.isMyTurn = false;
          
          // Add our move to history
          this.moveHistory.push({
            from: move.from,
            to: move.to,
            player: this.myColor,
            timestamp: Date.now()
          });
          
        } else {
          console.error(`❌ AI move failed:`, moveResponse.error);
          // Try a fallback move
          setTimeout(() => this.makeAIMove(), 2000);
        }
        
      } else {
        console.error(`💀 AI Bot could not find any valid moves!`);
        // This might indicate checkmate or stalemate
      }
      
    } catch (error) {
      console.error(`🚨 AI move execution error:`, error.message);
      setTimeout(() => this.makeAIMove(), 3000); // Retry after delay
    }
  }

  findBestMove() {
    console.log(`🧠 AI analyzing board with ${this.difficulty} difficulty...`);
    
    // Get all possible moves for our pieces
    const possibleMoves = this.generatePossibleMoves();
    
    if (possibleMoves.length === 0) {
      console.log(`💀 No possible moves found for ${this.myColor}`);
      return null;
    }

    console.log(`🔍 Found ${possibleMoves.length} possible moves`);
    
    // Apply AI strategy based on difficulty
    let bestMove;
    switch (this.difficulty.toLowerCase()) {
      case 'easy':
        bestMove = this.selectRandomMove(possibleMoves);
        break;
      case 'medium':
        bestMove = this.selectTacticalMove(possibleMoves);
        break;
      case 'hard':
        bestMove = this.selectStrategicMove(possibleMoves);
        break;
      default:
        bestMove = this.selectTacticalMove(possibleMoves);
    }

    return bestMove;
  }

  generatePossibleMoves() {
    const moves = [];
    
    // Generate some realistic chess moves based on board size
    // This is a simplified version - in a real implementation you'd have
    // the actual board state from the server
    
    const centerStart = Math.floor(this.boardSize / 3);
    const centerEnd = this.boardSize - centerStart;
    
    // Opening moves - advance pieces toward center
    for (let x = centerStart; x < centerEnd; x++) {
      for (let y = 0; y < this.boardSize; y++) {
        for (let z = centerStart; z < centerEnd; z++) {
          // Generate forward moves (y-axis progression)
          const fromY = this.myColor === 'White' ? 0 : this.boardSize - 1;
          const toY = this.myColor === 'White' ? Math.min(fromY + 1, this.boardSize - 1) : Math.max(fromY - 1, 0);
          
          moves.push({
            from: { x, y: fromY, z },
            to: { x, y: toY, z },
            type: 'advance',
            description: `Advance piece from (${x},${fromY},${z}) to (${x},${toY},${z})`
          });
          
          // Diagonal moves
          if (z + 1 < this.boardSize) {
            moves.push({
              from: { x, y: fromY, z },
              to: { x, y: toY, z: z + 1 },
              type: 'diagonal',
              description: `Diagonal move from (${x},${fromY},${z}) to (${x},${toY},${z + 1})`
            });
          }
          
          if (z - 1 >= 0) {
            moves.push({
              from: { x, y: fromY, z },
              to: { x, y: toY, z: z - 1 },
              type: 'diagonal',
              description: `Diagonal move from (${x},${fromY},${z}) to (${x},${toY},${z - 1})`
            });
          }
        }
      }
    }
    
    // Add some random moves for variety
    for (let i = 0; i < 5; i++) {
      const fromX = Math.floor(Math.random() * this.boardSize);
      const fromY = Math.floor(Math.random() * this.boardSize);
      const fromZ = Math.floor(Math.random() * this.boardSize);
      
      const toX = Math.max(0, Math.min(this.boardSize - 1, fromX + Math.floor(Math.random() * 3) - 1));
      const toY = Math.max(0, Math.min(this.boardSize - 1, fromY + (Math.random() > 0.5 ? 1 : -1)));
      const toZ = Math.max(0, Math.min(this.boardSize - 1, fromZ + Math.floor(Math.random() * 3) - 1));
      
      moves.push({
        from: { x: fromX, y: fromY, z: fromZ },
        to: { x: toX, y: toY, z: toZ },
        type: 'random',
        description: `Random move from (${fromX},${fromY},${fromZ}) to (${toX},${toY},${toZ})`
      });
    }
    
    return moves;
  }

  selectRandomMove(moves) {
    const randomIndex = Math.floor(Math.random() * moves.length);
    const move = moves[randomIndex];
    console.log(`🎲 Easy AI: Selected random move`);
    return move;
  }

  selectTacticalMove(moves) {
    // Medium difficulty: prefer center control and forward advancement
    const scoredMoves = moves.map(move => ({
      ...move,
      score: this.evaluateMove(move)
    }));
    
    scoredMoves.sort((a, b) => b.score - a.score);
    
    // Add some randomness by picking from top 3 moves
    const topMoves = scoredMoves.slice(0, Math.min(3, scoredMoves.length));
    const selectedMove = topMoves[Math.floor(Math.random() * topMoves.length)];
    
    console.log(`🎯 Medium AI: Selected tactical move (score: ${selectedMove.score.toFixed(2)})`);
    return selectedMove;
  }

  selectStrategicMove(moves) {
    // Hard difficulty: best move with deeper evaluation
    const scoredMoves = moves.map(move => ({
      ...move,
      score: this.evaluateMove(move) + this.evaluatePosition(move)
    }));
    
    scoredMoves.sort((a, b) => b.score - a.score);
    
    const bestMove = scoredMoves[0];
    console.log(`🧠 Hard AI: Selected strategic move (score: ${bestMove.score.toFixed(2)})`);
    return bestMove;
  }

  evaluateMove(move) {
    let score = 0;
    
    // Prefer center control
    const centerX = this.boardSize / 2;
    const centerZ = this.boardSize / 2;
    const distanceFromCenter = Math.sqrt(
      Math.pow(move.to.x - centerX, 2) + Math.pow(move.to.z - centerZ, 2)
    );
    score += (this.boardSize - distanceFromCenter) * 0.5;
    
    // Prefer forward advancement
    if (this.myColor === 'White' && move.to.y > move.from.y) score += 1.0;
    if (this.myColor === 'Black' && move.to.y < move.from.y) score += 1.0;
    
    // Prefer diagonal attacks in 3D space
    if (move.type === 'diagonal') score += 0.5;
    
    return score;
  }

  evaluatePosition(move) {
    let score = 0;
    
    // Advanced positional considerations
    // Control of key squares
    const isKeySquare = this.isKeySquare(move.to);
    if (isKeySquare) score += 1.5;
    
    // Piece coordination (simplified)
    score += this.moveHistory.length * 0.1; // Slight bonus for game progression
    
    return score;
  }

  isKeySquare(position) {
    const center = this.boardSize / 2;
    const distance = Math.abs(position.x - center) + Math.abs(position.z - center);
    return distance <= 1; // Central squares are key
  }

  getThinkingTime() {
    // Realistic thinking times based on difficulty
    switch (this.difficulty.toLowerCase()) {
      case 'easy':
        return 500 + Math.random() * 1000; // 0.5-1.5 seconds
      case 'medium':
        return 1000 + Math.random() * 2000; // 1-3 seconds
      case 'hard':
        return 2000 + Math.random() * 3000; // 2-5 seconds
      default:
        return 1000 + Math.random() * 2000;
    }
  }

  // Enhanced error handling for join failures
  handleJoinError(errorMessage) {
    console.log('');
    console.log('🔬 === Error Analysis ===');
    
    if (errorMessage.includes('Room not found')) {
      console.log(`❌ Room ${this.roomCode} does not exist on the server`);
      console.log('💡 Possible solutions:');
      console.log('   1. Create the room first in Unity');
      console.log('   2. Check if the room code is correct');
      console.log('   3. Verify the server wasn\'t restarted (rooms are in-memory)');
      console.log('');
      console.log('🤝 Alternative: Create a new room');
      console.log(`   Run: node start-ai-bot.js (without room code)`);
      
    } else if (errorMessage.includes('Room is full')) {
      console.log(`❌ Room ${this.roomCode} already has the maximum number of players`);
      console.log('💡 Possible solutions:');
      console.log('   1. Wait for a player to leave');
      console.log('   2. Create a new room');
      
    } else if (errorMessage.includes('Game already in progress')) {
      console.log(`❌ Room ${this.roomCode} has a game currently in progress`);
      console.log('💡 Possible solutions:');
      console.log('   1. Wait for the current game to finish');
      console.log('   2. Create a new room');
      
    } else {
      console.log(`❌ Unknown error: ${errorMessage}`);
      console.log('💡 This might be a server or connection issue');
      console.log('   Try running the diagnostic: node check-room-status.js');
    }
    console.log('');
  }

  // Graceful shutdown
  stop() {
    console.log(`🛑 AI Bot shutting down...`);
    this.isGameActive = false;
    this.isMyTurn = false;
    this.disconnect();
  }
}

// Command line interface for easy room joining
async function joinRoomAsAI() {
  const roomCode = process.argv[2] || 'LMF8S2';
  const difficulty = process.argv[3] || 'medium';
  const botName = process.argv[4] || 'AI_Player';
  
  console.log(`🤖 Starting AI Bot for room: ${roomCode}`);
  console.log(`🎮 Difficulty: ${difficulty}`);
  console.log(`👤 Bot name: ${botName}`);
  console.log('');
  
  const aiBot = new AIBot(roomCode, botName, difficulty);
  
  // Handle graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Received shutdown signal...');
    aiBot.stop();
    process.exit(0);
  });
  
  // Start the AI bot
  await aiBot.start();
  
  // Keep the process alive
  setInterval(() => {
    // Keep alive - the bot handles everything through events
  }, 5000);
}

// Export for use as module or run directly
if (require.main === module) {
  joinRoomAsAI().catch(error => {
    console.error('❌ AI Bot failed to start:', error);
    process.exit(1);
  });
}

module.exports = { AIBot };