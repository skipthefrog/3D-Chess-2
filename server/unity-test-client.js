#!/usr/bin/env node

/**
 * Unity-like Test Client
 * Mimics Unity's Socket.IO connection behavior to test server compatibility
 */

const io = require('socket.io-client');

const SERVER_URL = 'http://localhost:3000';
const ROOM_CODE = '2J47R2'; // Join the AI-hosted room

console.log('🎮 === Unity-Like Test Client ===');
console.log('🌐 Server:', SERVER_URL);
console.log('🏠 Room Code:', ROOM_CODE);
console.log('');

// Mimic Unity's connection parameters exactly as they would be sent
const socket = io(SERVER_URL, {
  query: {
    token: 'UNITY',  // Exact token Unity sends
    playerId: `Unity_Player_${Date.now()}`,
    playerName: 'UnityTestPlayer'
  },
  headers: {
    'User-Agent': 'Unity/2022.3.45f1 (Mac OS X 14.6.0; x64) Chrome/91.0.4472.124'
  },
  transport: ['websocket', 'polling'],
  timeout: 10000,
  reconnection: true,
  reconnectionAttempts: 3,
  reconnectionDelay: 1000
});

console.log('🔌 Connecting to server...');

// Connection events
socket.on('connect', () => {
  console.log('✅ Connected to server!');
  console.log('🆔 Socket ID:', socket.id);
  console.log('');
  
  // Try to join the room like Unity would
  console.log('🚪 === JOIN ROOM ATTEMPT ===');
  console.log('🏠 Attempting to join room:', ROOM_CODE);
  
  socket.emit('join_room', {
    roomCode: ROOM_CODE,
    playerName: 'UnityTestPlayer'
  });
});

socket.on('connect_error', (error) => {
  console.log('❌ Connection failed:', error.message);
  console.log('🔍 Error details:', error);
});

socket.on('connection', (data) => {
  console.log('📨 Welcome message received:', data);
});

// Room events
socket.on('room_joined', (data) => {
  console.log('✅ Successfully joined room!');
  console.log('🎯 Room data:', JSON.stringify(data, null, 2));
});

socket.on('room_join_failed', (data) => {
  console.log('❌ Failed to join room:', data.error);
});

socket.on('room_updated', (data) => {
  console.log('🔄 Room updated:', JSON.stringify(data, null, 2));
});

socket.on('player_joined', (data) => {
  console.log('👥 Player joined room:', data.playerName);
});

socket.on('disconnect', (reason) => {
  console.log('🔌 Disconnected:', reason);
});

// Test connection for 30 seconds
setTimeout(() => {
  console.log('');
  console.log('⏰ Test complete - disconnecting...');
  socket.disconnect();
  process.exit(0);
}, 30000);

// Handle cleanup
process.on('SIGINT', () => {
  console.log('\n🛑 Test interrupted - disconnecting...');
  socket.disconnect();
  process.exit(0);
});