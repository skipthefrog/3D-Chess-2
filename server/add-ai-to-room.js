/**
 * Add AI to Room - Main Testing Tool
 * Simple command-line tool to add an AI player to any room code
 * Perfect for testing and development
 * 
 * Usage: node add-ai-to-room.js [roomCode] [difficulty] [aiName]
 */

const { SmartAIJoiner } = require('./smart-ai-joiner.js');

async function main() {
  // Show help if requested
  if (process.argv.includes('--help') || process.argv.includes('-h')) {
    showHelp();
    process.exit(0);
  }

  // Parse command line arguments
  const roomCode = process.argv[2];
  const difficulty = process.argv[3] || 'medium';
  const aiName = process.argv[4];
  
  // Show help if no room code provided
  if (!roomCode) {
    showHelp();
    process.exit(0);
  }

  // Validate difficulty
  const validDifficulties = ['easy', 'medium', 'hard'];
  if (!validDifficulties.includes(difficulty.toLowerCase())) {
    console.log(`❌ Invalid difficulty: ${difficulty}`);
    console.log(`💡 Valid options: ${validDifficulties.join(', ')}`);
    process.exit(1);
  }

  console.log(`🚀 === Add AI to Room Tool ===`);
  console.log(`🎯 Room Code: ${roomCode.toUpperCase()}`);
  console.log(`🧠 Difficulty: ${difficulty}`);
  if (aiName) console.log(`👤 AI Name: ${aiName}`);
  console.log('');

  // Create and run the AI joiner
  const joiner = new SmartAIJoiner();
  
  try {
    const success = await joiner.joinRoomWithAI(roomCode.toUpperCase(), difficulty, aiName);
    
    if (success) {
      console.log(`🎉 AI successfully added to room ${roomCode.toUpperCase()}!`);
      console.log('');
      console.log(`📋 What happens next:`);
      console.log(`   1. AI is waiting in the room`);
      console.log(`   2. Join the room in Unity with code: ${roomCode.toUpperCase()}`);
      console.log(`   3. Set both players ready to start the game`);
      console.log(`   4. AI will automatically make moves when it's its turn`);
      console.log('');
      console.log(`🔧 Debugging:`);
      console.log(`   - Watch server logs for move details`);
      console.log(`   - AI uses ${difficulty} difficulty strategy`);
      console.log(`   - Press Ctrl+C to remove AI from room`);
      console.log('');
      
      // Keep the AI alive and playing
      await joiner.keepAlive();
      
    } else {
      console.log(`❌ Failed to add AI to room ${roomCode.toUpperCase()}`);
      console.log('');
      console.log(`🔧 Troubleshooting:`);
      console.log(`   1. Make sure the server is running:`);
      console.log(`      cd "/Users/skip/3D Chess 2/server"`);
      console.log(`      node server-enhanced.js`);
      console.log('');
      console.log(`   2. Create room ${roomCode.toUpperCase()} in Unity first`);
      console.log('');
      console.log(`   3. Check room status:`);
      console.log(`      node check-room-status.js ${roomCode.toUpperCase()}`);
      
      process.exit(1);
    }
    
  } catch (error) {
    console.error(`🚨 Unexpected error:`, error.message);
    console.log('');
    console.log(`💡 Quick fixes:`);
    console.log(`   - Restart the server: node server-enhanced.js`);
    console.log(`   - Check network connection`);
    console.log(`   - Verify room code is correct: ${roomCode.toUpperCase()}`);
    
    process.exit(1);
  }
}

function showHelp() {
  console.log(`
🤖 Add AI to Room - 3D Chess Testing Tool

USAGE:
  node add-ai-to-room.js <roomCode> [difficulty] [aiName]

ARGUMENTS:
  roomCode     Required. 6-character room code (e.g., ABC123)
  difficulty   Optional. AI difficulty: easy, medium, hard (default: medium)
  aiName       Optional. Custom name for AI player (default: auto-generated)

EXAMPLES:
  node add-ai-to-room.js ABC123
  node add-ai-to-room.js XYZ789 hard
  node add-ai-to-room.js PLAYER easy ChessBot

WORKFLOW:
  1. Create a room in Unity (you'll get a 6-character code)
  2. Run this script with that room code
  3. AI joins the room automatically
  4. Join the same room in Unity
  5. Start playing chess against the AI!

DIFFICULTY LEVELS:
  easy    - Random moves, 0.5-1.5s thinking time
  medium  - Tactical play, center control, 1-3s thinking time
  hard    - Strategic positioning, 2-5s thinking time

FEATURES:
  ✅ Works with any room code
  ✅ Automatic room validation
  ✅ Real-time status updates
  ✅ Intelligent error handling
  ✅ Perfect for testing and development

TROUBLESHOOTING:
  🔧 Server not running: node server-enhanced.js
  🔧 Room doesn't exist: Create it in Unity first
  🔧 Check room status: node check-room-status.js ROOMCODE
  🔧 Monitor activity: node room-monitor.js

This tool makes it easy to add AI opponents to any room for testing!
`);
}

// Quick validation function
function validateRoomCode(roomCode) {
  if (!roomCode) return false;
  if (typeof roomCode !== 'string') return false;
  
  const upperCode = roomCode.toUpperCase();
  if (upperCode.length !== 6) return false;
  if (!/^[A-Z0-9]+$/.test(upperCode)) return false;
  return true;
}

// Enhanced version with room code validation
function validateInput() {
  const roomCode = process.argv[2];
  
  if (!roomCode) {
    console.log('❌ Room code is required!');
    console.log('Usage: node add-ai-to-room.js <roomCode>');
    console.log('Example: node add-ai-to-room.js ABC123');
    return false;
  }
  
  if (!validateRoomCode(roomCode)) {
    console.log(`❌ Invalid room code: ${roomCode}`);
    console.log('💡 Room codes should be 6 characters (letters and numbers)');
    console.log('💡 Example: ABC123, XYZ789, GAME01');
    return false;
  }
  
  return true;
}

// Enhanced main with validation
async function enhancedMain() {
  // Check for help first, before validation
  if (process.argv.includes('--help') || process.argv.includes('-h')) {
    showHelp();
    process.exit(0);
  }
  
  if (!validateInput()) {
    process.exit(1);
  }
  
  await main();
}

// Run the tool
if (require.main === module) {
  enhancedMain();
}