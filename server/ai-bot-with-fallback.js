/**
 * AI Bot with Room Creation Fallback
 * Enhanced version that can create a room if the target room doesn't exist
 */

const { AIBot } = require('./ai-bot.js');

class AIBotWithFallback extends AIBot {
  constructor(roomCode, name = 'AI_Player', difficulty = 'medium', serverUrl = 'http://localhost:3000') {
    super(roomCode, name, difficulty, serverUrl);
    this.canCreateRoom = true;
    this.createdRoom = false;
  }

  async start() {
    try {
      console.log(`🚀 Enhanced AI Bot starting up...`);
      console.log(`🎯 Target room: ${this.roomCode}`);
      console.log(`🧠 Difficulty: ${this.difficulty}`);
      console.log(`👤 Name: ${this.name}`);
      console.log(`🔧 Fallback: Can create room if needed`);
      console.log('');
      
      // Connect to server
      console.log(`🔌 Connecting to server...`);
      await this.connect();
      console.log(`✅ Connected successfully!`);
      
      // Setup event listeners
      this.setupAIGameListeners();
      console.log(`📡 Event listeners configured`);
      
      // Try to join the room, with fallback to create if needed
      await this.joinOrCreateRoom();
      
      // Set ready automatically
      console.log(`🎯 Setting ready status...`);
      await this.setReady(true);
      console.log(`✅ AI Bot is ready to play!`);
      
      console.log('');
      if (this.createdRoom) {
        console.log(`🎮 AI Bot created and joined room ${this.roomCode}`);
        console.log(`🤝 Share room code "${this.roomCode}" with other players to join!`);
      } else {
        console.log(`🎮 AI Bot joined existing room ${this.roomCode}`);
      }
      console.log(`💡 The bot will automatically play when it's its turn.`);
      console.log(`🛑 Press Ctrl+C to stop the bot.`);
      console.log('');
      
    } catch (error) {
      console.error(`🚨 AI Bot startup failed:`, error.message);
      this.disconnect();
      throw error;
    }
  }

  async joinOrCreateRoom() {
    console.log(`🚪 Attempting to join room ${this.roomCode}...`);
    
    try {
      // First, try to join the existing room
      const joinResponse = await this.joinRoom(this.roomCode);
      this.myColor = joinResponse.assignedColor;
      
      console.log(`✅ Successfully joined existing room ${this.roomCode}!`);
      console.log(`🎨 Assigned color: ${this.myColor}`);
      console.log(`📊 Game state:`, joinResponse.gameState);
      
    } catch (joinError) {
      console.log(`⚠️ Could not join room ${this.roomCode}: ${joinError.message}`);
      
      if (joinError.message.includes('Room not found') && this.canCreateRoom) {
        console.log(`🛠️ Room doesn't exist. Creating new room...`);
        
        try {
          // Create a new room
          const createResponse = await this.createRoom();
          this.roomCode = createResponse.roomCode;
          this.myColor = createResponse.assignedColor;
          this.createdRoom = true;
          
          console.log(`✅ Successfully created room ${this.roomCode}!`);
          console.log(`🎨 Assigned color: ${this.myColor}`);
          console.log(`📊 Game state:`, createResponse.gameState);
          
        } catch (createError) {
          console.error(`❌ Failed to create room: ${createError.message}`);
          throw createError;
        }
        
      } else {
        // Other errors (room full, game in progress, etc.)
        this.handleJoinError(joinError.message);
        throw joinError;
      }
    }
  }

  // Enhanced game listeners to handle being the room creator
  setupAIGameListeners() {
    super.setupAIGameListeners();
    
    // Additional listener for when we're the room host
    this.socket.on('player_joined', (data) => {
      console.log(`👥 Player joined room: ${data.playerName} (${data.assignedColor})`);
      
      if (this.createdRoom) {
        console.log(`🎮 Room now has players. Ready to start game!`);
        
        // If we're the host and room has enough players, we could auto-start
        // For now, just notify that we're ready
        setTimeout(() => {
          console.log(`🚀 All players can set ready status to begin!`);
        }, 1000);
      }
    });
  }
}

// Command line interface
async function main() {
  const roomCode = process.argv[2] || 'LMF8S2';
  const difficulty = process.argv[3] || 'medium';
  const botName = process.argv[4] || 'AI_Bot';
  
  console.log(`🤖 Enhanced AI Bot with Room Creation Fallback`);
  console.log(`🎮 Room Code: ${roomCode}`);
  console.log(`🧠 Difficulty: ${difficulty}`);
  console.log(`👤 Bot Name: ${botName}`);
  console.log('');
  
  const aiBot = new AIBotWithFallback(roomCode, botName, difficulty);
  
  // Handle graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down AI Bot...');
    aiBot.stop();
    setTimeout(() => process.exit(0), 1000);
  });
  
  try {
    await aiBot.start();
    
    // Keep the process alive
    setInterval(() => {
      // Bot handles everything through events
    }, 10000);
    
  } catch (error) {
    console.error('❌ Enhanced AI Bot failed to start:', error.message);
    process.exit(1);
  }
}

// Export for use as module or run directly
if (require.main === module) {
  main();
}

module.exports = { AIBotWithFallback };