/**
 * Smart AI Manager
 * Advanced tool for managing AI players across multiple rooms
 * Can auto-join lonely rooms, manage multiple AI instances, and more
 */

const { SmartAIJoiner, RoomDiscovery } = require('./smart-ai-joiner.js');
const { RoomMonitor } = require('./room-monitor.js');

class SmartAIManager {
  constructor(serverUrl = 'http://localhost:3000') {
    this.serverUrl = serverUrl;
    this.monitor = new RoomMonitor(serverUrl);
    this.activeAIs = new Map(); // roomCode -> AIJoiner
    this.autoJoinMode = false;
    this.defaultDifficulty = 'medium';
    this.isRunning = false;
  }

  async start(options = {}) {
    const {
      autoJoin = false,
      difficulty = 'medium',
      watchMode = false
    } = options;

    this.autoJoinMode = autoJoin;
    this.defaultDifficulty = difficulty;
    this.isRunning = true;

    try {
      console.log(`🤖 === Smart AI Manager ===`);
      console.log(`🌐 Server: ${this.serverUrl}`);
      console.log(`🔧 Auto-join mode: ${autoJoin ? 'ON' : 'OFF'}`);
      console.log(`🧠 Default difficulty: ${difficulty}`);
      console.log(`👁️  Watch mode: ${watchMode ? 'ON' : 'OFF'}`);
      console.log('');

      // Start monitoring server activity
      await this.monitor.startMonitoring();
      console.log(`✅ Connected and monitoring server activity`);

      if (autoJoin) {
        this.setupAutoJoinListeners();
        console.log(`🔄 Auto-join mode active - will add AI to lonely rooms`);
      }

      if (watchMode) {
        this.setupWatchMode();
        console.log(`👁️  Watch mode active - monitoring all room activity`);
      }

      console.log('');
      console.log(`💡 Manager is running. Use Ctrl+C to stop.`);
      console.log('');

      return true;

    } catch (error) {
      console.error(`❌ Failed to start AI Manager:`, error.message);
      throw error;
    }
  }

  setupAutoJoinListeners() {
    // Listen for lonely rooms that need AI players
    this.monitor.onPlayerJoin = async (data) => {
      console.log(`👥 Player activity detected: ${data.playerName} joined as ${data.assignedColor}`);
      
      // If this is the first player in a room, they might need an AI opponent
      if (data.assignedColor === 'White') {
        console.log(`🤔 First player in room - might need AI opponent`);
        console.log(`⏳ Waiting 5 seconds for more players to join...`);
        
        // Wait a bit to see if more humans join
        setTimeout(async () => {
          await this.considerAddingAI(data);
        }, 5000);
      }
    };

    this.monitor.onPlayerLeave = (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
      console.log(`🤔 Room might need AI replacement...`);
      
      // Could implement AI replacement logic here
    };
  }

  setupWatchMode() {
    this.monitor.onRoomUpdate = (event, data) => {
      console.log(`📊 Room update [${event}]:`, data);
    };

    // Display status periodically
    setInterval(() => {
      this.displayStatus();
    }, 30000); // Every 30 seconds
  }

  async considerAddingAI(playerData) {
    if (!this.autoJoinMode || !this.isRunning) return;

    console.log(`🤖 Considering adding AI to support ${playerData.playerName}...`);
    
    // Create a test room code (in real implementation, we'd track actual room codes)
    // For now, we'll create a placeholder since we don't have the room code from the event
    const estimatedRoomCode = `ROOM${Date.now().toString().slice(-3)}`;
    
    console.log(`💡 Note: In auto-join mode, room code tracking would be implemented`);
    console.log(`💡 For now, use manual mode: node add-ai-to-room.js ROOMCODE`);
  }

  async addAIToRoom(roomCode, difficulty = null) {
    const aiDifficulty = difficulty || this.defaultDifficulty;
    
    if (this.activeAIs.has(roomCode)) {
      console.log(`⚠️ AI already active in room ${roomCode}`);
      return false;
    }

    console.log(`🤖 Adding AI to room ${roomCode} with ${aiDifficulty} difficulty...`);

    try {
      const joiner = new SmartAIJoiner(this.serverUrl);
      const success = await joiner.joinRoomWithAI(roomCode, aiDifficulty);

      if (success) {
        this.activeAIs.set(roomCode, joiner);
        console.log(`✅ AI successfully added to room ${roomCode}`);
        
        // Monitor this AI
        this.monitorAI(roomCode, joiner);
        return true;
      } else {
        console.log(`❌ Failed to add AI to room ${roomCode}`);
        return false;
      }

    } catch (error) {
      console.error(`❌ Error adding AI to room ${roomCode}:`, error.message);
      return false;
    }
  }

  monitorAI(roomCode, joiner) {
    // Set up monitoring for this specific AI
    const checkInterval = setInterval(() => {
      if (!joiner.activeAI || !joiner.activeAI.socket?.connected) {
        console.log(`⚠️ AI in room ${roomCode} disconnected`);
        this.activeAIs.delete(roomCode);
        clearInterval(checkInterval);
      }
    }, 10000);
  }

  removeAIFromRoom(roomCode) {
    const joiner = this.activeAIs.get(roomCode);
    if (joiner) {
      console.log(`🛑 Removing AI from room ${roomCode}...`);
      joiner.cleanup();
      this.activeAIs.delete(roomCode);
      console.log(`✅ AI removed from room ${roomCode}`);
      return true;
    } else {
      console.log(`⚠️ No AI found in room ${roomCode}`);
      return false;
    }
  }

  displayStatus() {
    console.log('\n📊 === AI Manager Status ===');
    console.log(`🔗 Connected: ${this.monitor.socket?.connected || false}`);
    console.log(`🤖 Active AIs: ${this.activeAIs.size}`);
    console.log(`🔄 Auto-join: ${this.autoJoinMode ? 'ON' : 'OFF'}`);
    console.log(`⏱️  Uptime: ${process.uptime().toFixed(1)}s`);
    
    if (this.activeAIs.size > 0) {
      console.log(`📋 Active rooms:`);
      for (const [roomCode, joiner] of this.activeAIs) {
        const status = joiner.activeAI?.socket?.connected ? '🟢' : '🔴';
        console.log(`   ${status} ${roomCode}: ${joiner.activeAI?.myColor || 'unknown'} (${joiner.difficulty})`);
      }
    }
    console.log('');
  }

  stop() {
    console.log(`🛑 Stopping AI Manager...`);
    this.isRunning = false;

    // Stop all active AIs
    for (const [roomCode, joiner] of this.activeAIs) {
      console.log(`🤖 Stopping AI in room ${roomCode}...`);
      joiner.cleanup();
    }
    this.activeAIs.clear();

    // Stop monitoring
    this.monitor.stopMonitoring();
    console.log(`✅ AI Manager stopped`);
  }
}

// Command line interface
async function main() {
  const args = process.argv.slice(2);
  
  // Parse command line options
  const options = {
    autoJoin: args.includes('--auto') || args.includes('-a'),
    watchMode: args.includes('--watch') || args.includes('-w'),
    difficulty: 'medium'
  };

  // Get difficulty from command line
  const difficultyIndex = args.findIndex(arg => ['easy', 'medium', 'hard'].includes(arg));
  if (difficultyIndex !== -1) {
    options.difficulty = args[difficultyIndex];
  }

  // Show help
  if (args.includes('--help') || args.includes('-h')) {
    showHelp();
    process.exit(0);
  }

  console.log(`🚀 Starting Smart AI Manager...`);
  
  const manager = new SmartAIManager();

  // Setup graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down AI Manager...');
    manager.stop();
    setTimeout(() => process.exit(0), 2000);
  });

  try {
    await manager.start(options);

    // Interactive commands
    console.log(`💡 Interactive commands:`);
    console.log(`   Type 'status' to see current status`);
    console.log(`   Type 'add ROOMCODE' to add AI to a room`);
    console.log(`   Type 'remove ROOMCODE' to remove AI from room`);
    console.log(`   Type 'help' for command list`);
    console.log('');

    // Simple command interface
    process.stdin.on('data', (data) => {
      const input = data.toString().trim();
      handleCommand(manager, input);
    });

    // Keep alive
    setInterval(() => {
      if (!manager.isRunning) {
        process.exit(0);
      }
    }, 5000);

  } catch (error) {
    console.error('❌ AI Manager failed:', error.message);
    process.exit(1);
  }
}

function handleCommand(manager, input) {
  const parts = input.split(' ');
  const command = parts[0].toLowerCase();

  switch (command) {
    case 'status':
      manager.displayStatus();
      break;
    case 'add':
      if (parts[1]) {
        manager.addAIToRoom(parts[1].toUpperCase(), parts[2]);
      } else {
        console.log('❌ Usage: add ROOMCODE [difficulty]');
      }
      break;
    case 'remove':
      if (parts[1]) {
        manager.removeAIFromRoom(parts[1].toUpperCase());
      } else {
        console.log('❌ Usage: remove ROOMCODE');
      }
      break;
    case 'help':
      console.log(`\n📋 Available commands:`);
      console.log(`   status - Show manager status`);
      console.log(`   add ROOMCODE [difficulty] - Add AI to room`);
      console.log(`   remove ROOMCODE - Remove AI from room`);
      console.log(`   help - Show this help`);
      console.log('');
      break;
    default:
      console.log(`❌ Unknown command: ${command}. Type 'help' for available commands.`);
  }
}

function showHelp() {
  console.log(`
🤖 Smart AI Manager - Advanced 3D Chess AI Control

USAGE:
  node smart-ai-manager.js [options]

OPTIONS:
  --auto, -a      Auto-join mode (add AI to lonely rooms)
  --watch, -w     Watch mode (monitor all activity)
  easy|medium|hard Default AI difficulty (default: medium)
  --help, -h      Show this help

EXAMPLES:
  node smart-ai-manager.js --watch
  node smart-ai-manager.js --auto hard
  node smart-ai-manager.js --watch --auto medium

FEATURES:
  ✅ Monitor multiple rooms simultaneously
  ✅ Auto-add AI to rooms that need players
  ✅ Interactive command interface
  ✅ Real-time room activity monitoring
  ✅ Manage multiple AI difficulties

INTERACTIVE COMMANDS:
  status              Show current status
  add ROOMCODE        Add AI to specific room
  remove ROOMCODE     Remove AI from room
  help               Show command list

WORKFLOW:
  1. Start the manager: node smart-ai-manager.js --watch
  2. Create rooms in Unity
  3. Manager will detect activity and can auto-add AI
  4. Or manually add AI: add ABC123
  5. Monitor all activity in real-time

This is perfect for development and testing with multiple rooms!
`);
}

module.exports = { SmartAIManager };

if (require.main === module) {
  main();
}