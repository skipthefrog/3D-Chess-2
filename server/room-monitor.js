/**
 * Room Monitor System
 * Watches server for room activity and provides real-time room status
 * Can be used standalone or as a base class for other tools
 */

const { TestClient } = require('./test-client.js');

class RoomMonitor extends TestClient {
  constructor(serverUrl = 'http://localhost:3000') {
    super(`RoomMonitor_${Date.now()}`, serverUrl);
    this.rooms = new Map();
    this.isMonitoring = false;
    this.onRoomUpdate = null; // Callback for room updates
    this.onPlayerJoin = null; // Callback for player joins
    this.onPlayerLeave = null; // Callback for player leaves
  }

  async startMonitoring() {
    try {
      console.log(`🔍 Starting room monitoring...`);
      console.log(`🌐 Server: ${this.serverUrl}`);
      
      // Connect to server
      await this.connect();
      console.log(`✅ Connected to server`);
      
      // Setup monitoring event listeners
      this.setupMonitoringListeners();
      this.isMonitoring = true;
      
      console.log(`👁️  Monitoring active. Watching for room activity...`);
      console.log(`📊 Use Ctrl+C to stop monitoring`);
      console.log('');
      
      return true;
      
    } catch (error) {
      console.error(`❌ Failed to start monitoring:`, error.message);
      throw error;
    }
  }

  setupMonitoringListeners() {
    // Listen for room events that we can observe
    this.socket.on('player_joined', (data) => {
      this.handlePlayerJoined(data);
    });

    this.socket.on('player_left', (data) => {
      this.handlePlayerLeft(data);
    });

    this.socket.on('game_started', (data) => {
      this.handleGameStarted(data);
    });

    this.socket.on('game_state_updated', (data) => {
      this.handleGameStateUpdated(data);
    });

    // Connection events
    this.socket.on('connection', (data) => {
      console.log(`📡 Server connection confirmed:`, data.message);
    });

    this.socket.on('disconnect', (reason) => {
      console.log(`🔌 Disconnected from server: ${reason}`);
      this.isMonitoring = false;
    });
  }

  handlePlayerJoined(data) {
    console.log(`👥 Player activity: ${data.playerName} joined as ${data.assignedColor}`);
    
    if (this.onPlayerJoin) {
      this.onPlayerJoin(data);
    }
  }

  handlePlayerLeft(data) {
    console.log(`👋 Player activity: ${data.playerName} left the room`);
    
    if (this.onPlayerLeave) {
      this.onPlayerLeave(data);
    }
  }

  handleGameStarted(data) {
    console.log(`🎮 Game started! Phase: ${data.gameState?.phase || 'unknown'}`);
    
    if (this.onRoomUpdate) {
      this.onRoomUpdate('game_started', data);
    }
  }

  handleGameStateUpdated(data) {
    console.log(`📊 Game state updated: ${JSON.stringify(data)}`);
    
    if (this.onRoomUpdate) {
      this.onRoomUpdate('game_updated', data);
    }
  }

  // Method to check if a specific room exists and get its status
  async checkRoom(roomCode) {
    return new Promise((resolve, reject) => {
      console.log(`🔍 Checking room ${roomCode}...`);
      
      // Create a temporary connection to test room status
      const tempSocket = this.socket;
      
      tempSocket.emit('join_room', { roomCode }, (response) => {
        if (response.success) {
          // Successfully joined, immediately leave
          tempSocket.emit('leave_room', {}, () => {
            resolve({
              exists: true,
              canJoin: true,
              assignedColor: response.assignedColor,
              gameState: response.gameState
            });
          });
        } else {
          // Failed to join, analyze the error
          const roomStatus = this.analyzeRoomError(response.error);
          resolve(roomStatus);
        }
      });
      
      // Timeout after 5 seconds
      setTimeout(() => {
        reject(new Error('Room check timeout'));
      }, 5000);
    });
  }

  analyzeRoomError(errorMessage) {
    if (errorMessage.includes('Room not found')) {
      return {
        exists: false,
        canJoin: false,
        reason: 'Room does not exist'
      };
    } else if (errorMessage.includes('Room is full')) {
      return {
        exists: true,
        canJoin: false,
        reason: 'Room is full (2/2 players)'
      };
    } else if (errorMessage.includes('Game already in progress')) {
      return {
        exists: true,
        canJoin: false,
        reason: 'Game in progress'
      };
    } else {
      return {
        exists: true,
        canJoin: false,
        reason: errorMessage
      };
    }
  }

  // Get a summary of server activity
  displayServerStatus() {
    console.log('\n📊 === Server Status ===');
    console.log(`🔗 Connected: ${this.socket?.connected || false}`);
    console.log(`👁️  Monitoring: ${this.isMonitoring}`);
    console.log(`🕐 Uptime: ${process.uptime().toFixed(1)}s`);
    console.log('');
  }

  stopMonitoring() {
    console.log(`🛑 Stopping room monitoring...`);
    this.isMonitoring = false;
    this.disconnect();
  }

  // Override setupGameEventListeners to avoid conflicts
  setupGameEventListeners() {
    // Use monitoring listeners instead
  }
}

// Standalone monitoring mode
async function runStandaloneMonitor() {
  console.log(`👁️  === 3D Chess Room Monitor ===`);
  console.log(`🎯 Watching for room activity on the server`);
  console.log('');
  
  const monitor = new RoomMonitor();
  
  // Setup graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down monitor...');
    monitor.stopMonitoring();
    setTimeout(() => process.exit(0), 1000);
  });
  
  try {
    await monitor.startMonitoring();
    
    // Display status every 30 seconds
    setInterval(() => {
      monitor.displayServerStatus();
    }, 30000);
    
    // Keep alive
    setInterval(() => {
      if (!monitor.isMonitoring) {
        console.log('❌ Lost connection to server. Exiting...');
        process.exit(1);
      }
    }, 5000);
    
  } catch (error) {
    console.error('❌ Monitor failed:', error.message);
    console.log('\n💡 Make sure the server is running:');
    console.log('   node server-enhanced.js');
    process.exit(1);
  }
}

// Export for use as module or run standalone
module.exports = { RoomMonitor };

if (require.main === module) {
  // Check command line arguments
  if (process.argv.includes('--help') || process.argv.includes('-h')) {
    console.log(`
👁️  3D Chess Room Monitor

USAGE:
  node room-monitor.js [options]

OPTIONS:
  --help, -h    Show this help message

FEATURES:
  ✅ Real-time room activity monitoring
  ✅ Player join/leave notifications  
  ✅ Game state change tracking
  ✅ Server status monitoring

EXAMPLE OUTPUT:
  👥 Player activity: Alice joined as White
  👋 Player activity: Bob left the room
  🎮 Game started! Phase: playing
  📊 Game state updated: {...}

Use this to watch server activity while testing your 3D chess game.
`);
    process.exit(0);
  }
  
  runStandaloneMonitor();
}