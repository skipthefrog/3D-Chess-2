/**
 * Room Status Checker
 * Connects to the server and checks the status of a specific room
 * Usage: node check-room-status.js [roomCode]
 */

const { TestClient } = require('./test-client.js');

class RoomStatusChecker extends TestClient {
  constructor(roomCode, serverUrl = 'http://localhost:3000') {
    super(`StatusChecker_${Date.now()}`, serverUrl);
    this.roomCode = roomCode;
    this.roomInfo = null;
  }

  async checkRoomStatus() {
    try {
      console.log(`🔍 Checking status of room: ${this.roomCode}`);
      console.log(`🌐 Server: ${this.serverUrl}`);
      console.log('');

      // Connect to server
      await this.connect();
      console.log('✅ Connected to server successfully');

      // Try to join the room to see what happens
      console.log(`🚪 Attempting to join room ${this.roomCode}...`);
      
      try {
        const joinResponse = await this.joinRoom(this.roomCode);
        console.log('✅ Successfully joined room!');
        console.log('📋 Room Details:');
        console.log(`   - Room Code: ${joinResponse.roomCode || this.roomCode}`);
        console.log(`   - Assigned Color: ${joinResponse.assignedColor}`);
        console.log(`   - Game State:`, joinResponse.gameState);
        
        // Check current room status
        this.socket.emit('get_room_info', { roomCode: this.roomCode }, (response) => {
          if (response && response.success) {
            console.log('🏠 Extended Room Info:');
            console.log(`   - Player Count: ${response.playerCount}/${response.maxPlayers}`);
            console.log(`   - Game Phase: ${response.gamePhase}`);
            console.log(`   - Is Joinable: ${response.isJoinable}`);
            console.log(`   - Players:`, response.players);
          }
        });

        // Leave the room
        setTimeout(async () => {
          console.log('👋 Leaving room for testing...');
          this.disconnect();
        }, 2000);

      } catch (joinError) {
        console.log('❌ Failed to join room');
        console.log(`   Error: ${joinError.message}`);
        
        // Analyze the error
        this.analyzeJoinError(joinError.message);
      }

    } catch (error) {
      console.error('🚨 Connection error:', error.message);
    }
  }

  analyzeJoinError(errorMessage) {
    console.log('\n🔬 Error Analysis:');
    
    if (errorMessage.includes('Room not found')) {
      console.log('❌ Room LMF8S2 does not exist on the server');
      console.log('💡 Solutions:');
      console.log('   1. Verify the room code is correct');
      console.log('   2. Make sure the room was created in Unity');
      console.log('   3. Check if the server was restarted (rooms are in-memory)');
      
    } else if (errorMessage.includes('Room is full')) {
      console.log('❌ Room LMF8S2 already has maximum players (2)');
      console.log('💡 Solutions:');
      console.log('   1. One player needs to leave the room first');
      console.log('   2. Or create a new room');
      
    } else if (errorMessage.includes('Game already in progress')) {
      console.log('❌ Room LMF8S2 has a game in progress');
      console.log('💡 Solutions:');
      console.log('   1. Wait for the current game to end');
      console.log('   2. Or create a new room');
      
    } else {
      console.log(`❌ Unknown error: ${errorMessage}`);
      console.log('💡 This might be a connection or server issue');
    }
  }

  setupGameEventListeners() {
    // Override with status-checking specific listeners
    this.socket.on('player_joined', (data) => {
      console.log(`👥 Player joined: ${data.playerName} (${data.assignedColor})`);
    });

    this.socket.on('player_left', (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
    });

    this.socket.on('game_started', (data) => {
      console.log(`🎮 Game started! Current state:`, data.gameState);
    });

    this.socket.on('game_state_updated', (data) => {
      console.log(`📊 Game state updated:`, data);
    });
  }
}

// Enhanced server info checker
async function checkServerStatus() {
  console.log('🌐 Checking server status...');
  
  const checker = new RoomStatusChecker('TEST');
  
  try {
    await checker.connect();
    console.log('✅ Server is running and accepting connections');
    
    // Try to get server stats if available
    checker.socket.emit('get_server_stats', {}, (response) => {
      if (response) {
        console.log('📊 Server Statistics:');
        console.log(`   - Total Rooms: ${response.totalRooms || 'Unknown'}`);
        console.log(`   - Total Players: ${response.totalPlayers || 'Unknown'}`);
        console.log(`   - Active Games: ${response.activeGames || 'Unknown'}`);
        console.log(`   - Uptime: ${response.uptime ? Math.floor(response.uptime) + 's' : 'Unknown'}`);
      }
    });
    
    setTimeout(() => checker.disconnect(), 1000);
    
  } catch (error) {
    console.log('❌ Server is not running or not accessible');
    console.log(`   Error: ${error.message}`);
    console.log('💡 Solutions:');
    console.log('   1. Start the server: node server-enhanced.js');
    console.log('   2. Check if port 3000 is available');
    console.log('   3. Verify server configuration');
  }
}

// Main execution
async function main() {
  const roomCode = process.argv[2];
  
  if (!roomCode) {
    console.log('🔍 3D Chess Room Status Checker');
    console.log('');
    console.log('Usage: node check-room-status.js [roomCode]');
    console.log('Example: node check-room-status.js LMF8S2');
    console.log('');
    console.log('First, let me check if the server is running...');
    console.log('');
    
    await checkServerStatus();
    process.exit(0);
  }

  console.log('🔍 === 3D Chess Room Status Checker ===');
  console.log('');
  
  // First check server status
  await checkServerStatus();
  console.log('');
  
  // Then check specific room
  const checker = new RoomStatusChecker(roomCode);
  await checker.checkRoomStatus();
  
  setTimeout(() => process.exit(0), 3000);
}

// Export for use as module
module.exports = { RoomStatusChecker };

// Run if called directly
if (require.main === module) {
  main().catch(error => {
    console.error('🚨 Checker failed:', error);
    process.exit(1);
  });
}