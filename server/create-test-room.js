/**
 * Creates a test room and keeps it open for Unity testing
 */

const { TestClient } = require('./test-client.js');

async function createAndHoldRoom() {
  console.log('🎮 Creating test room for Unity testing...\n');
  
  const host = new TestClient('TestHost');
  
  try {
    // Connect to server
    console.log('🔌 Connecting to server...');
    await host.connect();
    
    // Create room
    console.log('🏠 Creating room...');
    const roomResponse = await host.createRoom();
    const roomCode = roomResponse.roomCode;
    
    console.log('\n' + '='.repeat(50));
    console.log('🎯 ROOM CREATED SUCCESSFULLY!');
    console.log('='.repeat(50));
    console.log(`📋 Room Code: ${roomCode}`);
    console.log('🎮 Host Color: White');
    console.log('👥 Waiting for Unity player to join...');
    console.log('='.repeat(50));
    console.log('\n💡 Instructions:');
    console.log('1. Start Unity 3D Chess game');
    console.log('2. Go to Join Game');
    console.log('3. Turn OFF "Match me with a game"');
    console.log(`4. Enter room code: ${roomCode}`);
    console.log('5. Click "Join Room"');
    console.log('\n⏹️  Press Ctrl+C to stop this test room\n');
    
    // Set up event listeners for room activity
    host.socket.on('player_joined', (data) => {
      console.log(`✅ Unity player joined! Player: ${data.playerName}, Color: ${data.assignedColor}`);
      console.log('🎮 Ready to start game from Unity!');
    });
    
    host.socket.on('player_left', (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
    });
    
    host.socket.on('game_started', (data) => {
      console.log('🚀 Game started from Unity!');
      console.log('Game State:', data.gameState);
    });
    
    host.socket.on('move_received', (data) => {
      console.log('🎯 Move received from Unity:', data);
    });
    
    // Keep the room alive
    process.on('SIGINT', () => {
      console.log('\n🛑 Shutting down test room...');
      host.disconnect();
      process.exit(0);
    });
    
    // Keep process alive
    setInterval(() => {
      // Just keep alive, room code displayed above
    }, 1000);
    
  } catch (error) {
    console.error('❌ Error creating test room:', error.message);
    host.disconnect();
    process.exit(1);
  }
}

// Run the test
createAndHoldRoom();