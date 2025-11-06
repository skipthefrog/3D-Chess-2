/**
 * Test client for 3D Chess Server
 * Simulates Unity client connections and game interactions
 */

const { io } = require('socket.io-client');

class TestClient {
  constructor(name, serverUrl = 'http://localhost:3000') {
    this.name = name;
    this.serverUrl = serverUrl;
    this.socket = null;
    this.playerId = `test_${name}_${Date.now()}`;
    this.currentRoom = null;
  }

  connect() {
    return new Promise((resolve, reject) => {
      console.log(`🔌 ${this.name}: Connecting to ${this.serverUrl}...`);
      
      this.socket = io(this.serverUrl, {
        query: {
          token: 'UNITY',
          playerId: this.playerId,
          playerName: this.name
        },
        // Enhanced connection settings to match server timeouts
        timeout: 45000,        // Match server connectTimeout
        forceNew: false,       // Allow connection reuse
        reconnection: true,    // Enable automatic reconnection
        reconnectionDelay: 1000,
        reconnectionDelayMax: 5000,
        maxReconnectionAttempts: 5,
        transports: ['websocket', 'polling'] // Match server transports
      });

      this.socket.on('connect', () => {
        console.log(`✅ ${this.name}: Connected successfully`);
        resolve();
      });

      this.socket.on('connect_error', (error) => {
        console.error(`❌ ${this.name}: Connection failed:`, error.message);
        reject(error);
      });

      this.socket.on('connection', (data) => {
        console.log(`📨 ${this.name}: Welcome message:`, data);
      });

      this.socket.on('disconnect', (reason) => {
        console.log(`🔌 ${this.name}: Disconnected:`, reason);
        
        // Enhanced disconnect handling
        if (reason === 'transport close' || reason === 'transport error') {
          console.log(`📞 ${this.name}: Network disconnection detected - connection will attempt to restore automatically`);
        } else if (reason === 'ping timeout') {
          console.log(`⏰ ${this.name}: Ping timeout - connection may be unstable`);
        }
      });

      // Enhanced connection monitoring
      this.socket.on('connect_error', (error) => {
        console.error(`❌ ${this.name}: Connection error:`, error.message);
      });

      this.socket.on('reconnect', (attemptNumber) => {
        console.log(`🔄 ${this.name}: Reconnected successfully after ${attemptNumber} attempt(s)`);
      });

      this.socket.on('reconnect_attempt', (attemptNumber) => {
        console.log(`🔄 ${this.name}: Reconnection attempt #${attemptNumber}...`);
      });

      this.socket.on('reconnect_error', (error) => {
        console.error(`❌ ${this.name}: Reconnection failed:`, error.message);
      });

      this.socket.on('reconnect_failed', () => {
        console.error(`❌ ${this.name}: Reconnection failed after maximum attempts`);
      });

      // Game event listeners
      this.setupGameEventListeners();
    });
  }

  setupGameEventListeners() {
    this.socket.on('player_joined', (data) => {
      console.log(`👥 ${this.name}: Player joined room:`, data);
    });

    this.socket.on('player_left', (data) => {
      console.log(`👋 ${this.name}: Player left room:`, data);
    });

    this.socket.on('game_started', (data) => {
      console.log(`🎮 ${this.name}: Game started:`, data);
    });

    this.socket.on('move_received', (data) => {
      console.log(`🎯 ${this.name}: Move received:`, data);
    });

    this.socket.on('game_state_updated', (data) => {
      console.log(`📊 ${this.name}: Game state updated:`, data);
    });
  }

  createRoom() {
    return new Promise((resolve, reject) => {
      console.log(`🏠 ${this.name}: Creating room...`);
      
      this.socket.emit('create_room', {}, (response) => {
        if (response.success) {
          this.currentRoom = response.roomCode;
          console.log(`✅ ${this.name}: Room created: ${response.roomCode}`);
          resolve(response);
        } else {
          console.error(`❌ ${this.name}: Failed to create room:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  joinRoom(roomCode) {
    return new Promise((resolve, reject) => {
      console.log(`🚪 ${this.name}: Joining room ${roomCode}...`);
      
      this.socket.emit('join_room', { roomCode }, (response) => {
        if (response.success) {
          this.currentRoom = roomCode;
          console.log(`✅ ${this.name}: Joined room: ${roomCode} as ${response.assignedColor}`);
          resolve(response);
        } else {
          console.error(`❌ ${this.name}: Failed to join room:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  setReady(isReady = true) {
    return new Promise((resolve, reject) => {
      console.log(`🎯 ${this.name}: Setting ready status to ${isReady}...`);
      
      this.socket.emit('player_ready', { isReady }, (response) => {
        if (response.success) {
          console.log(`✅ ${this.name}: Ready status updated`);
          resolve(response);
        } else {
          console.error(`❌ ${this.name}: Failed to update ready status:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  startGame() {
    return new Promise((resolve, reject) => {
      console.log(`🎮 ${this.name}: Starting game...`);
      
      this.socket.emit('start_game', {}, (response) => {
        if (response.success) {
          console.log(`✅ ${this.name}: Game started successfully`);
          resolve(response);
        } else {
          console.error(`❌ ${this.name}: Failed to start game:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  sendMove(from, to, playerColor) {
    return new Promise((resolve, reject) => {
      const moveData = {
        fromX: from.x, fromY: from.y, fromZ: from.z,
        toX: to.x, toY: to.y, toZ: to.z,
        playerColor,
        timestamp: Date.now()
      };

      console.log(`🎯 ${this.name}: Sending move...`, moveData);
      
      this.socket.emit('send_move', moveData, (response) => {
        if (response.success) {
          console.log(`✅ ${this.name}: Move sent successfully`);
          resolve(response);
        } else {
          console.error(`❌ ${this.name}: Failed to send move:`, response.error);
          reject(new Error(response.error));
        }
      });
    });
  }

  disconnect() {
    if (this.socket) {
      console.log(`🔌 ${this.name}: Disconnecting...`);
      this.socket.disconnect();
    }
  }

  // Test legacy Unity messages
  async testLegacyMessages() {
    console.log(`🧪 ${this.name}: Testing legacy Unity messages...`);
    
    // Test hello message
    this.socket.emit('hello', { message: 'Hello from test client' });
    
    // Test connection test
    this.socket.emit('test_connection', { client: this.name, timestamp: Date.now() });
    
    // Test spin event
    this.socket.emit('spin', { rotation: 360 });
    
    // Test class event
    this.socket.emit('class', { type: 'TestClient', name: this.name });
    
    await this.sleep(1000);
    console.log(`✅ ${this.name}: Legacy message tests completed`);
  }

  sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Test scenarios
async function runBasicConnectionTest() {
  console.log('\n🧪 === BASIC CONNECTION TEST ===');
  
  const client = new TestClient('TestPlayer1');
  try {
    await client.connect();
    await client.testLegacyMessages();
    await client.sleep(2000);
    client.disconnect();
    console.log('✅ Basic connection test passed');
  } catch (error) {
    console.error('❌ Basic connection test failed:', error.message);
  }
}

async function runRoomManagementTest() {
  console.log('\n🧪 === ROOM MANAGEMENT TEST ===');
  
  const host = new TestClient('Host');
  const player = new TestClient('Player');
  
  try {
    // Connect both clients
    await host.connect();
    await player.connect();
    
    // Host creates room
    const roomResponse = await host.createRoom();
    const roomCode = roomResponse.roomCode;
    
    // Player joins room
    await player.joinRoom(roomCode);
    
    // Both players set ready
    await host.setReady(true);
    await player.setReady(true);
    
    // Host starts game
    await host.startGame();
    
    await host.sleep(2000);
    
    // Cleanup
    host.disconnect();
    player.disconnect();
    
    console.log('✅ Room management test passed');
  } catch (error) {
    console.error('❌ Room management test failed:', error.message);
    host.disconnect();
    player.disconnect();
  }
}

async function runMoveTest() {
  console.log('\n🧪 === MOVE HANDLING TEST ===');
  
  const white = new TestClient('WhitePlayer');
  const black = new TestClient('BlackPlayer');
  
  try {
    // Connect and setup game
    await white.connect();
    await black.connect();
    
    const roomResponse = await white.createRoom();
    await black.joinRoom(roomResponse.roomCode);
    
    await white.setReady(true);
    await black.setReady(true);
    await white.startGame();
    
    // Test moves
    await white.sendMove({ x: 0, y: 0, z: 0 }, { x: 1, y: 0, z: 0 }, 'White');
    await white.sleep(500);
    await black.sendMove({ x: 3, y: 0, z: 0 }, { x: 2, y: 0, z: 0 }, 'Black');
    
    await white.sleep(2000);
    
    // Cleanup
    white.disconnect();
    black.disconnect();
    
    console.log('✅ Move handling test passed');
  } catch (error) {
    console.error('❌ Move handling test failed:', error.message);
    white.disconnect();
    black.disconnect();
  }
}

// Run all tests
async function runAllTests() {
  console.log('🚀 Starting 3D Chess Server Tests...\n');
  
  await runBasicConnectionTest();
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  await runRoomManagementTest();
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  await runMoveTest();
  
  console.log('\n🎉 All tests completed!');
}

// Command line interface
if (require.main === module) {
  const args = process.argv.slice(2);
  const command = args[0] || 'all';
  
  switch (command) {
    case 'basic':
      runBasicConnectionTest();
      break;
    case 'room':
      runRoomManagementTest();
      break;
    case 'move':
      runMoveTest();
      break;
    case 'all':
    default:
      runAllTests();
      break;
  }
}

module.exports = { TestClient };