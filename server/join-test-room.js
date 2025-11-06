/**
 * Joins a test room created by Unity or another client
 */

const { TestClient } = require('./test-client.js');

async function joinRoom() {
  // Get room code from command line argument
  const roomCode = process.argv[2];
  
  if (!roomCode) {
    console.log('❌ Please provide a room code!');
    console.log('Usage: node join-test-room.js ROOMCODE');
    console.log('Example: node join-test-room.js ABC123');
    process.exit(1);
  }
  
  console.log(`🎮 Joining room ${roomCode} as test player...\n`);
  
  const player = new TestClient('TestPlayer');
  
  try {
    // Connect to server
    console.log('🔌 Connecting to server...');
    await player.connect();
    
    // Join room
    console.log(`🚪 Joining room ${roomCode}...`);
    const joinResponse = await player.joinRoom(roomCode);
    
    console.log('\n' + '='.repeat(50));
    console.log('✅ JOINED ROOM SUCCESSFULLY!');
    console.log('='.repeat(50));
    console.log(`📋 Room Code: ${roomCode}`);
    console.log(`🎨 Assigned Color: ${joinResponse.assignedColor}`);
    console.log('🎮 Ready to interact with Unity player!');
    console.log('='.repeat(50));
    console.log('\n⏹️  Press Ctrl+C to leave room\n');
    
    // Set ready automatically
    console.log('🎯 Setting ready status...');
    await player.setReady(true);
    console.log('✅ Ready to start game!');
    
    // Set up event listeners
    player.socket.on('player_joined', (data) => {
      console.log(`👥 Another player joined: ${data.playerName}, Color: ${data.assignedColor}`);
    });
    
    player.socket.on('player_left', (data) => {
      console.log(`👋 Player left: ${data.playerName}`);
    });
    
    player.socket.on('game_started', (data) => {
      console.log('🚀 Game started!');
      console.log('Game State:', data.gameState);
      
      // If we're not the current player, wait a bit then make a move
      if (data.gameState.currentPlayer !== joinResponse.assignedColor) {
        setTimeout(async () => {
          console.log('🎯 Making a test move...');
          await player.sendMove(
            { x: 0, y: 0, z: 0 }, 
            { x: 1, y: 0, z: 0 }, 
            joinResponse.assignedColor
          );
        }, 2000);
      }
    });
    
    player.socket.on('move_received', (data) => {
      console.log('🎯 Move received:', data);
      
      // Auto-respond with a move after a delay
      setTimeout(async () => {
        console.log('🎯 Making a response move...');
        await player.sendMove(
          { x: Math.floor(Math.random() * 4), y: 0, z: Math.floor(Math.random() * 4) }, 
          { x: Math.floor(Math.random() * 4), y: 0, z: Math.floor(Math.random() * 4) }, 
          joinResponse.assignedColor
        );
      }, 1000 + Math.random() * 2000);
    });
    
    // Keep the connection alive
    process.on('SIGINT', () => {
      console.log('\n🛑 Leaving room...');
      player.disconnect();
      process.exit(0);
    });
    
    // Keep process alive
    setInterval(() => {
      // Just keep alive
    }, 1000);
    
  } catch (error) {
    console.error('❌ Error joining room:', error.message);
    player.disconnect();
    process.exit(1);
  }
}

// Run the test
joinRoom();