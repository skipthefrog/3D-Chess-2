/**
 * Start AI Bot Script
 * Usage: node start-ai-bot.js [roomCode] [difficulty] [botName]
 * 
 * Examples:
 * node start-ai-bot.js LMF8S2 medium AI_Bot
 * node start-ai-bot.js ABC123 hard ChessBot
 * node start-ai-bot.js (uses defaults: random room, medium difficulty)
 */

const { AIBot } = require('./ai-bot.js');

async function main() {
  const roomCode = process.argv[2] || generateRoomCode();
  const difficulty = process.argv[3] || 'medium';
  const botName = process.argv[4] || 'AI_Bot';
  
  console.log('🤖 === 3D Chess AI Bot ===');
  console.log(`🎮 Room Code: ${roomCode}`);
  console.log(`🧠 Difficulty: ${difficulty}`);
  console.log(`👤 Bot Name: ${botName}`);
  console.log(`🌐 Server: http://localhost:3000`);
  console.log('');
  
  // Create and start the AI bot
  const aiBot = new AIBot(roomCode, botName, difficulty);
  
  // Handle graceful shutdown
  process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down AI Bot...');
    aiBot.stop();
    setTimeout(() => process.exit(0), 1000);
  });
  
  process.on('SIGTERM', () => {
    aiBot.stop();
    setTimeout(() => process.exit(0), 1000);
  });
  
  try {
    await aiBot.start();
    console.log('🎉 AI Bot is running! Press Ctrl+C to stop.');
    console.log('🎮 The bot will automatically play when the game starts.');
    
    // Keep the process alive
    const keepAlive = setInterval(() => {
      // Bot handles everything through events
    }, 10000);
    
  } catch (error) {
    console.error('❌ Failed to start AI Bot:', error.message);
    
    if (error.message.includes('Room not found')) {
      console.log('\n💡 Suggestions:');
      console.log('1. Make sure you have the correct room code');
      console.log('2. Create a room first in Unity, then run this bot');
      console.log('3. Or run without room code to create a new room:');
      console.log(`   node start-ai-bot.js`);
    }
    
    process.exit(1);
  }
}

function generateRoomCode() {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < 6; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

// Show help if requested
if (process.argv.includes('--help') || process.argv.includes('-h')) {
  console.log(`
🤖 3D Chess AI Bot

USAGE:
  node start-ai-bot.js [roomCode] [difficulty] [botName]

ARGUMENTS:
  roomCode    6-character room code (e.g., LMF8S2)
  difficulty  AI difficulty: easy, medium, hard (default: medium)
  botName     Name for the AI bot (default: AI_Bot)

EXAMPLES:
  node start-ai-bot.js LMF8S2 medium
  node start-ai-bot.js ABC123 hard ChessBot
  node start-ai-bot.js (generates random room)

FEATURES:
  ✅ Automatically joins specified room
  ✅ Sets ready status automatically  
  ✅ Plays chess moves with realistic thinking time
  ✅ Three difficulty levels with different strategies
  ✅ Graceful shutdown with Ctrl+C

DIFFICULTY LEVELS:
  easy   - Random moves, 0.5-1.5s thinking time
  medium - Tactical moves, center control, 1-3s thinking time  
  hard   - Strategic positioning, 2-5s thinking time
`);
  process.exit(0);
}

main();