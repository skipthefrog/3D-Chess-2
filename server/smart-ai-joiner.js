/**
 * Smart AI Room Joiner
 * Intelligently joins AI players to any specified room
 * Handles room validation, error recovery, and provides detailed feedback
 */

const { AIBot } = require('./ai-bot.js');
const { RoomMonitor } = require('./room-monitor.js');

class SmartAIJoiner {
  constructor(serverUrl = 'http://localhost:3000') {
    this.serverUrl = serverUrl;
    this.monitor = new RoomMonitor(serverUrl);
    this.activeAI = null;
    this.roomCode = null;
    this.difficulty = 'medium';
  }

  async joinRoomWithAI(roomCode, difficulty = 'medium', aiName = null) {
    this.roomCode = roomCode;
    this.difficulty = difficulty;
    
    try {
      console.log(`🤖 === Smart AI Room Joiner ===`);
      console.log(`🎯 Target Room: ${roomCode}`);
      console.log(`🧠 AI Difficulty: ${difficulty}`);
      console.log(`🌐 Server: ${this.serverUrl}`);
      console.log('');

      // Step 1: Check room status first
      console.log(`🔍 Step 1: Checking room status...`);
      await this.monitor.startMonitoring();
      
      const roomStatus = await this.monitor.checkRoom(roomCode);
      this.displayRoomStatus(roomStatus);
      
      if (!roomStatus.exists) {
        console.log(`❌ Room ${roomCode} does not exist!`);
        console.log(`💡 Create this room in Unity first, then try again.`);
        this.monitor.stopMonitoring();
        return false;
      }
      
      if (!roomStatus.canJoin) {
        console.log(`❌ Cannot join room ${roomCode}: ${roomStatus.reason}`);
        this.monitor.stopMonitoring();
        return false;
      }

      // Step 2: Create and start AI bot
      console.log(`🤖 Step 2: Creating AI player...`);
      const botName = aiName || `AI_${difficulty}_${Date.now()}`;
      this.activeAI = new AIBot(roomCode, botName, difficulty, this.serverUrl);
      
      // Setup monitoring for AI events
      this.setupAIMonitoring();
      
      console.log(`🚀 Step 3: Joining AI to room...`);
      await this.activeAI.start();
      
      console.log(`✅ SUCCESS! AI player joined room ${roomCode}`);
      console.log(`🎨 AI Color: ${this.activeAI.myColor}`);
      console.log(`🎮 AI is ready to play!`);
      console.log('');
      console.log(`💡 The AI will automatically make moves when it's its turn.`);
      console.log(`🛑 Press Ctrl+C to remove AI and stop.`);
      
      return true;
      
    } catch (error) {
      console.error(`❌ Failed to join AI to room:`, error.message);
      
      if (error.message.includes('Room not found')) {
        console.log(`💡 Room ${roomCode} doesn't exist. Create it in Unity first.`);
      } else if (error.message.includes('Room is full')) {
        console.log(`💡 Room ${roomCode} is full. Try again when someone leaves.`);
      } else if (error.message.includes('Game already in progress')) {
        console.log(`💡 Room ${roomCode} has a game in progress. Wait for it to finish.`);
      } else {
        console.log(`💡 Check that the server is running and room code is correct.`);
      }
      
      this.cleanup();
      return false;
    }
  }

  displayRoomStatus(status) {
    console.log(`📊 Room Status Check:`);
    console.log(`   Exists: ${status.exists ? '✅' : '❌'}`);
    console.log(`   Can Join: ${status.canJoin ? '✅' : '❌'}`);
    
    if (status.exists && status.canJoin) {
      console.log(`   Available Color: ${status.assignedColor}`);
      console.log(`   Game Phase: ${status.gameState?.phase || 'unknown'}`);
    } else if (status.exists && !status.canJoin) {
      console.log(`   Reason: ${status.reason}`);
    }
    console.log('');
  }

  setupAIMonitoring() {
    // Monitor room activity and provide helpful feedback
    this.monitor.onPlayerJoin = (data) => {
      if (data.playerName !== this.activeAI.name) {
        console.log(`👥 Human player joined: ${data.playerName} (${data.assignedColor})`);
        console.log(`🎮 Room ready! Game can start when both players are ready.`);
      }
    };

    this.monitor.onPlayerLeave = (data) => {
      if (data.playerName !== this.activeAI.name) {
        console.log(`👋 Human player left: ${data.playerName}`);
        console.log(`⏳ AI waiting for another player to join...`);
      }
    };

    this.monitor.onRoomUpdate = (event, data) => {
      if (event === 'game_started') {
        console.log(`🚀 Game started! AI vs Human chess match begins!`);
      }
    };
  }

  cleanup() {
    console.log(`🧹 Cleaning up...`);
    
    if (this.activeAI) {
      this.activeAI.stop();
      this.activeAI = null;
    }
    
    if (this.monitor) {
      this.monitor.stopMonitoring();
    }
  }

  // Keep the AI running and responsive
  async keepAlive() {
    return new Promise((resolve) => {
      const keepAliveInterval = setInterval(() => {
        if (!this.activeAI || !this.activeAI.socket?.connected) {
          console.log(`❌ AI disconnected. Stopping...`);
          clearInterval(keepAliveInterval);
          this.cleanup();
          resolve(false);
        }
      }, 5000);

      // Handle graceful shutdown
      process.on('SIGINT', () => {
        console.log('\n🛑 Shutting down AI...');
        clearInterval(keepAliveInterval);
        this.cleanup();
        setTimeout(() => process.exit(0), 1500);
      });
    });
  }
}

// Enhanced room discovery - find rooms that need players
class RoomDiscovery {
  constructor(serverUrl = 'http://localhost:3000') {
    this.monitor = new RoomMonitor(serverUrl);
    this.availableRooms = [];
  }

  async findRoomsNeedingPlayers() {
    console.log(`🔍 Scanning for rooms that need players...`);
    
    try {
      await this.monitor.startMonitoring();
      
      // This is a simplified version - in a real implementation,
      // we'd need the server to provide a room list endpoint
      console.log(`⏳ Monitoring for room activity...`);
      console.log(`💡 Create a room in Unity and this will detect it!`);
      
      // Setup listeners to detect when rooms need players
      this.monitor.onPlayerJoin = (data) => {
        console.log(`🔍 Detected room activity: ${data.playerName} joined`);
        console.log(`💡 This room might need another player!`);
      };
      
      return true;
      
    } catch (error) {
      console.error(`❌ Room discovery failed:`, error.message);
      return false;
    }
  }

  stop() {
    this.monitor.stopMonitoring();
  }
}

module.exports = { SmartAIJoiner, RoomDiscovery };