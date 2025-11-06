/**
 * Join or Create Room LMF8S2
 * This script specifically handles joining room LMF8S2 or creates it if it doesn't exist
 */

const { TestClient } = require('./test-client.js');
const { AIBot } = require('./ai-bot.js');

class LMF8S2Bot extends AIBot {
  constructor(difficulty = 'medium', serverUrl = 'http://localhost:3000') {
    super('LMF8S2', 'AI_Player', difficulty, serverUrl);
    this.targetRoomCode = 'LMF8S2';
  }

  async start() {
    try {
      console.log(`🎯 === Joining Room LMF8S2 ===`);
      console.log(`🧠 Difficulty: ${this.difficulty}`);
      console.log(`👤 Name: ${this.name}`);
      console.log('');
      
      // Connect to server
      console.log(`🔌 Connecting to server...`);
      await this.connect();
      console.log(`✅ Connected successfully!`);
      
      // Setup event listeners
      this.setupAIGameListeners();
      
      // Check if LMF8S2 exists, create it if not
      await this.ensureRoomLMF8S2Exists();
      
      // Set ready automatically
      console.log(`🎯 Setting ready status...`);
      await this.setReady(true);
      console.log(`✅ AI Bot is ready to play!`);
      
      console.log('');
      console.log(`🎮 AI Bot is now in room LMF8S2`);
      console.log(`🎨 Playing as: ${this.myColor}`);
      console.log(`💡 Waiting for human player to join or start game...`);
      console.log(`🛑 Press Ctrl+C to stop the bot.`);
      console.log('');
      
    } catch (error) {
      console.error(`🚨 Failed to join room LMF8S2:`, error.message);
      this.disconnect();
      throw error;
    }
  }

  async ensureRoomLMF8S2Exists() {
    console.log(`🔍 Checking if room LMF8S2 exists...`);
    
    try {
      // First, try to join LMF8S2
      const joinResponse = await this.joinRoom('LMF8S2');
      this.myColor = joinResponse.assignedColor;
      
      console.log(`✅ Joined existing room LMF8S2!`);
      console.log(`🎨 Assigned color: ${this.myColor}`);
      console.log(`📊 Game state: ${joinResponse.gameState.phase}`);
      
    } catch (joinError) {
      if (joinError.message.includes('Room not found')) {
        console.log(`⚠️ Room LMF8S2 doesn't exist. Creating it...`);
        
        // Create the room, but we need to create it with the specific code
        // Since the server generates random codes, we'll create and then
        // advise the user about the new code
        const createResponse = await this.createRoom();
        this.myColor = createResponse.assignedColor;
        
        console.log(`✅ Created new room: ${createResponse.roomCode}`);
        console.log(`⚠️  Note: Server generated code ${createResponse.roomCode} instead of LMF8S2`);
        console.log(`🎨 Assigned color: ${this.myColor}`);
        console.log('');
        console.log(`💡 To join this room, use code: ${createResponse.roomCode}`);
        console.log(`💡 Or create room LMF8S2 manually in Unity first.`);
        
        // Update our room code to the actual one
        this.roomCode = createResponse.roomCode;
        
      } else {
        // Other errors (room full, game in progress, etc.)
        throw joinError;
      }
    }
  }

  setupAIGameListeners() {
    super.setupAIGameListeners();
    
    // Additional listener for room-specific events
    this.socket.on('player_joined', (data) => {
      console.log(`👥 Human player joined! ${data.playerName} (${data.assignedColor})`);
      console.log(`🎮 Room now has enough players to start!`);
      
      // Notify about game start
      setTimeout(() => {
        console.log(`🚀 When human player sets ready, game will begin!`);
      }, 1000);
    });

    this.socket.on('player_left', (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
      console.log(`⏳ Waiting for another player to join...`);
    });
  }
}

// Main execution
async function main() {
  const difficulty = process.argv[2] || 'medium';
  
  console.log(`🤖 LMF8S2 AI Bot`);
  console.log(`🎯 Target: Room LMF8S2`);
  console.log(`🧠 Difficulty: ${difficulty}`);
  console.log('');
  
  const bot = new LMF8S2Bot(difficulty);
  
  // Handle graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Stopping AI Bot...');
    bot.stop();
    setTimeout(() => process.exit(0), 1000);
  });
  
  try {
    await bot.start();
    
    // Keep alive
    setInterval(() => {
      // Bot handles everything through events
    }, 10000);
    
  } catch (error) {
    console.error(`❌ Failed:`, error.message);
    console.log('');
    console.log('💡 Make sure:');
    console.log('   1. The server is running (node server-enhanced.js)');
    console.log('   2. You create room LMF8S2 in Unity first');
    console.log('   3. Or this bot will create a new room for you');
    process.exit(1);
  }
}

if (require.main === module) {
  main();
}

module.exports = { LMF8S2Bot };